import { describe, test, expect } from 'vitest';
import {
  update_node_field,
  set_state_entry,
  remove_state_entry,
  add_next_entry,
  remove_next_entry,
} from '../node_editing.js';

function sample_tree() {
  return {
    id: 'level_1',
    scene: 'Level_1',
    name: 'Level 1',
    next: [{ id: 'level_2', condition: 'flags.is_beat' }],
    children: [],
  };
}

function sample_state() {
  return {
    flags: { is_beat: false },
    counters: { score: 0 },
    inventory: {},
    persistence: {},
    current_node: 'title',
  };
}

describe('update_node_field', () => {
  test('changes the scene field on the named node, leaving others untouched', () => {
    const updated = update_node_field(sample_tree(), 'level_1', 'scene', 'Level_1_New');
    expect(updated.scene).toBe('Level_1_New');
    expect(updated.name).toBe('Level 1');
  });

  test('changes the name field on the named node', () => {
    const updated = update_node_field(sample_tree(), 'level_1', 'name', 'Level One');
    expect(updated.name).toBe('Level One');
  });

  test('does not change the original tree object (a new one is returned)', () => {
    const original = sample_tree();
    const updated = update_node_field(original, 'level_1', 'scene', 'Changed');
    expect(original.scene).toBe('Level_1');
    expect(updated).not.toBe(original);
  });
});

describe('set_state_entry', () => {
  test('adds a brand new flag', () => {
    const updated = set_state_entry(sample_state(), 'flags', 'player_at_home', true);
    expect(updated.flags.player_at_home).toBe(true);
  });

  test('changes an existing counter without touching other counters', () => {
    const state = { ...sample_state(), counters: { score: 0, lives: 3 } };
    const updated = set_state_entry(state, 'counters', 'score', 100);
    expect(updated.counters.score).toBe(100);
    expect(updated.counters.lives).toBe(3);
  });
});

describe('remove_state_entry', () => {
  test('removes a flag by key', () => {
    const state = { ...sample_state(), flags: { is_beat: false, player_at_home: true } };
    const updated = remove_state_entry(state, 'flags', 'player_at_home');
    expect(updated.flags).not.toHaveProperty('player_at_home');
    expect(updated.flags).toHaveProperty('is_beat');
  });
});

describe('add_next_entry', () => {
  test('appends a new next entry, keeping the existing one', () => {
    const updated = add_next_entry(sample_tree(), 'ending', 'flags.game_won');
    expect(updated.next).toHaveLength(2);
    expect(updated.next[1]).toEqual({ id: 'ending', condition: 'flags.game_won' });
  });
});

describe('remove_next_entry', () => {
  test('removes the next entry at the given index', () => {
    const node = { ...sample_tree(), next: [{ id: 'a', condition: '' }, { id: 'b', condition: '' }] };
    const updated = remove_next_entry(node, 0);
    expect(updated.next).toEqual([{ id: 'b', condition: '' }]);
  });
});
