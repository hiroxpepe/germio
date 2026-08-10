// Pure data-layer helpers for the parts of germio.json a Node itself
// holds directly (scene, name, next[]) and the parts State holds
// (flags, counters, inventory, persistence). No DOM code lives here —
// only plain data in, plain data out, so every rule can be checked
// without a browser at all.

/**
 * Returns a new tree with the given field on the node whose id matches
 * node_id set to value. Every other node is left as it was.
 */
export function update_node_field(root, node_id, field, value) {
  if (root.id === node_id) {
    return { ...root, [field]: value };
  }
  return {
    ...root,
    children: root.children.map(child => update_node_field(child, node_id, field, value)),
  };
}

/**
 * Returns a new State with state[category][key] set to value. category
 * is one of "flags", "counters", "inventory", "persistence".
 */
export function set_state_entry(state, category, key, value) {
  return {
    ...state,
    [category]: { ...state[category], [key]: value },
  };
}

/**
 * Returns a new State with the given key removed from the named
 * category, leaving every other key untouched.
 */
export function remove_state_entry(state, category, key) {
  const updated_category = { ...state[category] };
  delete updated_category[key];
  return { ...state, [category]: updated_category };
}

/**
 * Returns a new node with a new { id, condition } entry appended to
 * its own next[] array.
 */
export function add_next_entry(node, id, condition) {
  return { ...node, next: [...node.next, { id, condition }] };
}

/**
 * Returns a new node with the next[] entry at the given index removed.
 */
export function remove_next_entry(node, index) {
  return { ...node, next: node.next.filter((_, i) => i !== index) };
}
