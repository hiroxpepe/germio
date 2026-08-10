import { describe, test, expect } from 'vitest';
import { parse_scenario, serialize_scenario } from '../scenario_file.js';

// A small, but complete, sample of germio.json's own top-level shape:
// schema_version, initial_state, and the root Node tree.
function sample_json_text() {
  return JSON.stringify({
    schema_version: 1,
    initial_state: {
      flags: { is_beat: false },
      counters: { score: 0 },
      inventory: {},
      persistence: {},
      current_node: 'title',
    },
    root: {
      id: 'title',
      scene: 'Title',
      name: 'Title',
      children: [
        { id: 'levels', scene: '', name: 'Levels', children: [], rules: [], next: [] },
      ],
      rules: [],
      next: [],
    },
  });
}

describe('parse_scenario', () => {
  test('reads schema_version as a number', () => {
    const scenario = parse_scenario(sample_json_text());
    expect(scenario.schema_version).toBe(1);
  });

  test('reads initial_state.flags', () => {
    const scenario = parse_scenario(sample_json_text());
    expect(scenario.initial_state.flags.is_beat).toBe(false);
  });

  test('reads the root Node, with its nested children intact', () => {
    const scenario = parse_scenario(sample_json_text());
    expect(scenario.root.id).toBe('title');
    expect(scenario.root.children[0].id).toBe('levels');
  });

  test('throws a clear error on text that is not valid JSON at all', () => {
    expect(() => parse_scenario('{ not json')).toThrow();
  });
});

describe('serialize_scenario', () => {
  test('round-trips: parse then serialize gives back an equal object', () => {
    const original = parse_scenario(sample_json_text());
    const round_tripped = parse_scenario(serialize_scenario(original));
    expect(round_tripped).toEqual(original);
  });

  test('writes valid, parseable JSON text', () => {
    const scenario = parse_scenario(sample_json_text());
    const text = serialize_scenario(scenario);
    expect(() => JSON.parse(text)).not.toThrow();
  });

  test('keeps the field order germio.json itself uses at the top level', () => {
    const scenario = parse_scenario(sample_json_text());
    const text = serialize_scenario(scenario);
    const keys = Object.keys(JSON.parse(text));
    expect(keys).toEqual(['schema_version', 'initial_state', 'root']);
  });
});
