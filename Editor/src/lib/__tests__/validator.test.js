import { describe, test, expect } from 'vitest';
import { validate_scenario } from '../validator.js';

function has(results, rule_id) {
  return results.some(r => r.rule_id === rule_id);
}

function base_scenario(root_overrides = {}) {
  return {
    initial_state: { flags: {}, counters: {}, inventory: {} },
    root: {
      id: 'root', scene: '', name: '', children: [], rules: [], next: [],
      ...root_overrides,
    },
  };
}

describe('V004 — Node.id must be unique across the whole Scenario', () => {
  test('flags a repeated Node.id anywhere in the tree', () => {
    const scenario = base_scenario({
      children: [
        { id: 'dup', scene: 'A', name: '', children: [], rules: [], next: [] },
        { id: 'dup', scene: 'B', name: '', children: [], rules: [], next: [] },
      ],
    });
    expect(has(validate_scenario(scenario), 'V004')).toBe(true);
  });

  test('does not flag ids that are all unique', () => {
    const scenario = base_scenario({
      children: [
        { id: 'a', scene: 'A', name: '', children: [], rules: [], next: [] },
        { id: 'b', scene: 'B', name: '', children: [], rules: [], next: [] },
      ],
    });
    expect(has(validate_scenario(scenario), 'V004')).toBe(false);
  });
});

describe('V005 — duplicate rule.id within one node', () => {
  test('flags two rules on the same node sharing an id', () => {
    const scenario = base_scenario({
      rules: [
        { id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true },
        { id: 'r', trigger: 't', condition: '', command: { request_notify: 'y' }, once: true },
      ],
    });
    expect(has(validate_scenario(scenario), 'V005')).toBe(true);
  });
});

describe('V006 — next.id points at a node that does not exist', () => {
  test('flags a next entry pointing nowhere', () => {
    const scenario = base_scenario({ next: [{ id: 'ghost', condition: '' }] });
    expect(has(validate_scenario(scenario), 'V006')).toBe(true);
  });

  test('does not flag a next entry pointing at a real node', () => {
    const scenario = base_scenario({
      children: [{ id: 'real', scene: 'A', name: '', children: [], rules: [], next: [] }],
      next: [{ id: 'real', condition: '' }],
    });
    expect(has(validate_scenario(scenario), 'V006')).toBe(false);
  });
});

describe('V007 — an empty condition fires unconditionally', () => {
  test('flags a rule with a blank condition', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V007')).toBe(true);
  });
});

describe('V008 — once=false with set_flag risks an infinite loop', () => {
  test('flags once=false combined with set_flag', () => {
    const scenario = base_scenario({
      rules: [{
        id: 'r', trigger: 't', condition: 'flags.a', once: false,
        command: { set_flag: { key: 'a', value: true } },
      }],
    });
    expect(has(validate_scenario(scenario), 'V008')).toBe(true);
  });

  test('does not flag once=true with set_flag', () => {
    const scenario = base_scenario({
      rules: [{
        id: 'r', trigger: 't', condition: 'flags.a', once: true,
        command: { set_flag: { key: 'a', value: true } },
      }],
    });
    expect(has(validate_scenario(scenario), 'V008')).toBe(false);
  });
});

describe('V010 — a command with nothing set has no effect', () => {
  test('flags a rule whose command is entirely empty', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: '', command: {}, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V010')).toBe(true);
  });

  test('does not flag a rule with only request_notify set', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V010')).toBe(false);
  });
});

describe('V011 — a dead-end node with no rules, no next, no children', () => {
  test('flags a leaf with nothing that can happen there', () => {
    const scenario = base_scenario({ scene: 'S' });
    expect(has(validate_scenario(scenario), 'V011')).toBe(true);
  });

  test('does not flag a node that has at least one rule', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V011')).toBe(false);
  });
});

describe('V012 — a circular chain in next[] transitions', () => {
  test('flags a loop where a points to b and b points back to a', () => {
    const scenario = {
      initial_state: { flags: {}, counters: {}, inventory: {} },
      root: {
        id: 'root', scene: '', name: '', rules: [],
        next: [],
        children: [
          { id: 'a', scene: 'A', name: '', rules: [], children: [], next: [{ id: 'b', condition: '' }] },
          { id: 'b', scene: 'B', name: '', rules: [], children: [], next: [{ id: 'a', condition: '' }] },
        ],
      },
    };
    expect(has(validate_scenario(scenario), 'V012')).toBe(true);
  });
});

describe('V020 — Node.scene must be unique (empty strings excluded)', () => {
  test('flags two nodes sharing the same non-empty scene', () => {
    const scenario = base_scenario({
      children: [
        { id: 'a', scene: 'Same', name: '', children: [], rules: [], next: [] },
        { id: 'b', scene: 'Same', name: '', children: [], rules: [], next: [] },
      ],
    });
    expect(has(validate_scenario(scenario), 'V020')).toBe(true);
  });

  test('does not flag two nodes both left with an empty scene', () => {
    const scenario = base_scenario({
      children: [
        { id: 'a', scene: '', name: '', children: [], rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true }], next: [] },
        { id: 'b', scene: '', name: '', children: [], rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true }], next: [] },
      ],
    });
    expect(has(validate_scenario(scenario), 'V020')).toBe(false);
  });
});

describe('V021 — a leaf node (no children) must have a scene', () => {
  test('flags a childless node with no scene', () => {
    const scenario = base_scenario({
      children: [{ id: 'leaf', scene: '', name: '', children: [], rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true }], next: [] }],
    });
    expect(has(validate_scenario(scenario), 'V021')).toBe(true);
  });
});

describe('V023 — a node with neither children nor a scene is forbidden', () => {
  test('flags a totally empty node', () => {
    const scenario = base_scenario({
      children: [{ id: 'empty', scene: '', name: '', children: [], rules: [], next: [] }],
    });
    expect(has(validate_scenario(scenario), 'V023')).toBe(true);
  });
});

describe('V024 / V025 — node tree depth limits', () => {
  function deep_chain(depth) {
    let node = { id: `n${depth}`, scene: 'S', name: '', children: [], rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true }], next: [] };
    for (let i = depth - 1; i >= 0; i--) {
      node = { id: `n${i}`, scene: '', name: '', rules: [], next: [], children: [node] };
    }
    return node;
  }

  test('V025 warns past the soft warning depth', () => {
    const scenario = { initial_state: { flags: {}, counters: {}, inventory: {} }, root: deep_chain(6) };
    expect(has(validate_scenario(scenario), 'V025')).toBe(true);
  });

  test('V024 errors past the hard max depth', () => {
    const scenario = { initial_state: { flags: {}, counters: {}, inventory: {} }, root: deep_chain(11) };
    expect(has(validate_scenario(scenario), 'V024')).toBe(true);
  });
});

describe('V026 — a child pointing back at one of its own ancestors', () => {
  test('flags a node whose children list its own ancestor', () => {
    // A real germio.json (always JSON.parse'd) can never hold a true
    // circular object reference — JSON itself cannot express one. The
    // real V026 violation this rule catches is a node appearing again,
    // by id, somewhere under its own descendant: a second, separate
    // node object that happens to share an ancestor's own id.
    const scenario = {
      initial_state: { flags: {}, counters: {}, inventory: {} },
      root: {
        id: 'ancestor', scene: '', name: '', rules: [], next: [],
        children: [
          {
            id: 'child', scene: '', name: '', rules: [], next: [],
            children: [
              { id: 'ancestor', scene: 'S', name: '', rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'x' }, once: true }], next: [], children: [] },
            ],
          },
        ],
      },
    };
    expect(has(validate_scenario(scenario), 'V026')).toBe(true);
  });
});

describe('V027 — request_notify is empty or whitespace-only', () => {
  test('flags a blank request_notify value', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: '   ' }, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V027')).toBe(true);
  });

  test('does not flag a real request_notify value', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: '', command: { request_notify: 'level_clear' }, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V027')).toBe(false);
  });
});

describe('V001 / V002 / V003 — a condition names an undefined key', () => {
  // Not a full DSL parser (that is ExprLexer/ExprParser's own job in
  // the real Validator.cs, and stays out of scope here). This is a
  // plain accessor scan: it finds every "flags.KEY" / "counters.KEY" /
  // "inventory.KEY" pattern in the condition text and checks it
  // against initial_state, the same three checks the real Validator.cs
  // makes, just without a real parse step first.
  test('V001: flags a flag key missing from initial_state.flags', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: 'flags.no_such_flag', command: { request_notify: 'x' }, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V001')).toBe(true);
  });

  test('does not flag a flag key that is defined', () => {
    const scenario = {
      initial_state: { flags: { is_beat: false }, counters: {}, inventory: {} },
      root: {
        id: 'root', scene: '', name: '', children: [], next: [],
        rules: [{ id: 'r', trigger: 't', condition: 'flags.is_beat', command: { request_notify: 'x' }, once: true }],
      },
    };
    expect(has(validate_scenario(scenario), 'V001')).toBe(false);
  });

  test('V002: flags a counter key missing from initial_state.counters', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: 'counters.no_such_counter >= 1', command: { request_notify: 'x' }, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V002')).toBe(true);
  });

  test('V003: flags an inventory key missing from initial_state.inventory', () => {
    const scenario = base_scenario({
      rules: [{ id: 'r', trigger: 't', condition: 'inventory.no_such_item >= 1', command: { request_notify: 'x' }, once: true }],
    });
    expect(has(validate_scenario(scenario), 'V003')).toBe(true);
  });

  test('checks both an ordinary rule condition and a next[] condition', () => {
    const scenario = base_scenario({ next: [{ id: 'root', condition: 'flags.missing_key' }] });
    expect(has(validate_scenario(scenario), 'V001')).toBe(true);
  });
});
