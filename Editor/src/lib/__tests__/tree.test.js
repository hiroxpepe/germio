import { describe, test, expect } from 'vitest';
import { find_node, is_descendant, move_node } from '../tree.js';

// A small tree shaped the same way germio.json's real Node.children is:
// nested, not a flat parent pointer.
function sample_tree() {
  return {
    id: 'title',
    children: [
      {
        id: 'levels',
        children: [
          { id: 'level_1', children: [] },
          { id: 'level_2', children: [] },
        ],
      },
      { id: 'ending', children: [] },
    ],
  };
}

describe('find_node', () => {
  test('finds a node at the root', () => {
    const root = sample_tree();
    expect(find_node(root, 'title')).toBe(root);
  });

  test('finds a node nested two levels deep', () => {
    const root = sample_tree();
    const found = find_node(root, 'level_1');
    expect(found.id).toBe('level_1');
  });

  test('returns null for an id that is not in the tree', () => {
    const root = sample_tree();
    expect(find_node(root, 'no_such_id')).toBe(null);
  });
});

describe('is_descendant', () => {
  test('a direct child is a descendant', () => {
    const root = sample_tree();
    expect(is_descendant(root, 'levels', 'level_1')).toBe(true);
  });

  test('a node is not its own descendant', () => {
    const root = sample_tree();
    expect(is_descendant(root, 'levels', 'levels')).toBe(false);
  });

  test('a sibling is not a descendant', () => {
    const root = sample_tree();
    expect(is_descendant(root, 'levels', 'ending')).toBe(false);
  });
});

describe('move_node', () => {
  test('moves a node into a new parent\'s children', () => {
    const root = sample_tree();
    const moved = move_node(root, 'ending', 'levels');
    const levels = find_node(moved, 'levels');
    expect(levels.children.map(c => c.id)).toContain('ending');
  });

  test('removes the node from its old parent after the move', () => {
    const root = sample_tree();
    const moved = move_node(root, 'ending', 'levels');
    // root's own children should no longer list "ending" directly.
    expect(moved.children.map(c => c.id)).not.toContain('ending');
  });

  test('refuses a move that would create a loop', () => {
    const root = sample_tree();
    // Moving "levels" to become a child of its own child "level_1"
    // would create a cycle; this must be rejected.
    expect(() => move_node(root, 'levels', 'level_1')).toThrow();
  });
});
