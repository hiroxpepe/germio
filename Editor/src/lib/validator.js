// A JavaScript mirror of germio's real Validator.cs, covering every
// STRUCTURAL rule (one that needs no DSL parser to check): V004-V012,
// V020-V027. V001/V002/V003 (undefined flag/counter/inventory keys)
// and V009 (a condition's own DSL syntax) are NOT ported here — those
// need the real ExprLexer/ExprParser, which is its own, much bigger
// piece of work, left for later. This file is a deliberate mirror,
// not the source of truth; the real rules live in Validator.cs, and
// this file can drift from it over time unless kept in sync by hand.

const MAX_NODE_DEPTH = 10;
const WARNING_NODE_DEPTH = 5;

function push(results, rule_id, level, message) {
  results.push({ rule_id, level, message });
}

export function validate_scenario(scenario) {
  const results = [];
  const all_ids = [];
  collect_ids(scenario.root, all_ids);
  check_duplicate_node_ids(all_ids, results);
  check_duplicate_scenes(scenario.root, results);
  check_circular_transitions(scenario.root, results);
  walk_node(scenario.root, results, 0, [], scenario.initial_state);
  return results;
}

// A plain accessor scan for V001/V002/V003: not a real DSL parse (that
// stays out of scope, see the file comment at the top), just a search
// for every "flags.KEY" / "counters.KEY" / "inventory.KEY" pattern in
// a condition string, checked against initial_state.
const ACCESSOR_PATTERN = /\b(flags|counters|inventory)\.([A-Za-z_][A-Za-z0-9_]*)/g;

function check_condition_keys(condition, state, node_id, results) {
  if (!condition) return;
  for (const match of condition.matchAll(ACCESSOR_PATTERN)) {
    const [, prefix, key] = match;
    if (!Object.prototype.hasOwnProperty.call(state[prefix], key)) {
      const rule_id = prefix === 'flags' ? 'V001' : prefix === 'counters' ? 'V002' : 'V003';
      push(results, rule_id, 'Warning', `Node '${node_id}': ${prefix}.${key} is not defined in initial_state.${prefix}.`);
    }
  }
}

function collect_ids(node, ids) {
  ids.push(node.id);
  for (const child of node.children) collect_ids(child, ids);
}

function check_duplicate_node_ids(all_ids, results) {
  const seen = new Set();
  for (const id of all_ids) {
    if (seen.has(id)) {
      push(results, 'V004', 'Error', `Duplicate Node.id '${id}' in the Scenario.`);
    }
    seen.add(id);
  }
}

function check_duplicate_scenes(node, results, seen = new Set()) {
  if (node.scene && node.scene !== '') {
    if (seen.has(node.scene)) {
      push(results, 'V020', 'Error', `Node.scene '${node.scene}' is used by more than one node.`);
    }
    seen.add(node.scene);
  }
  for (const child of node.children) check_duplicate_scenes(child, results, seen);
}

function walk_node(node, results, depth, ancestors, state) {
  // V011: a dead end (no rules, no next, no children).
  if (node.rules.length === 0 && node.next.length === 0 && node.children.length === 0) {
    push(results, 'V011', 'Warning', `Node '${node.id}' is a dead end (no rules, no next, no children).`);
  }

  // V021: a leaf node (no children) must have a scene.
  // V023: a node with neither children nor a scene is forbidden.
  if (node.children.length === 0) {
    if (!node.scene || node.scene === '') {
      push(results, 'V021', 'Error', `Leaf node '${node.id}' has no scene.`);
      push(results, 'V023', 'Error', `Node '${node.id}' has neither children nor a scene.`);
    }
  }

  // V024 / V025: node depth limits.
  if (depth > MAX_NODE_DEPTH) {
    push(results, 'V024', 'Error', `Node '${node.id}' at depth ${depth} exceeds MAX_NODE_DEPTH (${MAX_NODE_DEPTH}).`);
  } else if (depth > WARNING_NODE_DEPTH) {
    push(results, 'V025', 'Warning', `Node '${node.id}' at depth ${depth} exceeds the warning depth (${WARNING_NODE_DEPTH}).`);
  }

  // V026: a child pointing back at one of its own ancestors.
  for (const child of node.children) {
    if (ancestors.includes(child.id)) {
      push(results, 'V026', 'Error', `Circular reference: node '${child.id}' is its own ancestor.`);
    }
  }

  // V006: a next.id pointing nowhere. Checked against the whole tree's ids.
  const all_ids = [];
  collect_ids_from_top(node, all_ids);

  // V005: duplicate rule.id within this one node.
  const rule_ids_seen = new Set();
  for (const rule of node.rules) {
    if (rule_ids_seen.has(rule.id)) {
      push(results, 'V005', 'Error', `Duplicate rule.id '${rule.id}' in node '${node.id}'.`);
    }
    rule_ids_seen.add(rule.id);

    // V007: an empty condition fires unconditionally.
    if (!rule.condition || rule.condition.trim() === '') {
      push(results, 'V007', 'Warning', `Rule '${rule.id}' in node '${node.id}' has an empty condition.`);
    } else {
      check_condition_keys(rule.condition, state, node.id, results);
    }

    // V008: once=false with set_flag risks an infinite loop.
    if (!rule.once && rule.command && rule.command.set_flag) {
      push(results, 'V008', 'Warning', `Rule '${rule.id}' in node '${node.id}' has once=false with set_flag.`);
    }

    // V027: request_notify is empty or whitespace-only.
    if (rule.command && rule.command.request_notify !== undefined &&
        rule.command.request_notify !== null &&
        rule.command.request_notify.trim() === '') {
      push(results, 'V027', 'Warning', `Rule '${rule.id}' in node '${node.id}' has an empty request_notify.`);
    }

    // V010: a command with nothing set has no effect.
    if (!rule.command || command_is_empty(rule.command)) {
      push(results, 'V010', 'Error', `Rule '${rule.id}' in node '${node.id}' has an empty command.`);
    }
  }

  for (const next_entry of node.next) {
    if (!all_ids.includes(next_entry.id)) {
      push(results, 'V006', 'Error', `Node '${node.id}' -> next.id '${next_entry.id}' does not exist.`);
    }
    check_condition_keys(next_entry.condition, state, node.id, results);
  }

  for (const child of node.children) {
    walk_node(child, results, depth + 1, [...ancestors, node.id], state);
  }
}

function collect_ids_from_top(node, ids) {
  // A next.id may point at any node in the whole Scenario, not only a
  // descendant, so this walks from the true root every time it is
  // needed. validate_scenario's own collect_ids already does this once
  // for V004; this local copy keeps walk_node self-contained and easy
  // to read on its own.
  ids.push(node.id);
  for (const child of node.children) collect_ids_from_top(child, ids);
}

function command_is_empty(command) {
  return !command.set_flag && !command.update_counter && !command.update_inventory &&
    !command.request_transition && !command.request_notify && !command.set_persistence &&
    !command.record_event && !command.reset_flags && !command.reset_counters &&
    !command.reset_inventory;
}

function check_circular_transitions(root, results) {
  const node_map = new Map();
  build_node_map(root, node_map);
  const visited = new Set();
  const rec_stack = new Set();
  for (const id of node_map.keys()) {
    if (!visited.has(id)) {
      has_cycle(id, node_map, visited, rec_stack, [], results);
    }
  }
}

function build_node_map(node, map) {
  map.set(node.id, node);
  for (const child of node.children) build_node_map(child, map);
}

function has_cycle(current_id, node_map, visited, rec_stack, path, results) {
  visited.add(current_id);
  rec_stack.add(current_id);
  path.push(current_id);

  const node = node_map.get(current_id);
  if (node) {
    for (const next_entry of node.next) {
      if (!node_map.has(next_entry.id)) continue;
      if (!visited.has(next_entry.id)) {
        if (has_cycle(next_entry.id, node_map, visited, rec_stack, path, results)) return true;
      } else if (rec_stack.has(next_entry.id)) {
        const cycle_start = path.indexOf(next_entry.id);
        const cycle_path = [...path.slice(cycle_start), next_entry.id].join(' -> ');
        push(results, 'V012', 'Error', `Circular transition chain detected: ${cycle_path}`);
        return true;
      }
    }
  }
  rec_stack.delete(current_id);
  return false;
}
