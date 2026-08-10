// @vitest-environment jsdom
import { describe, test, expect } from 'vitest';
import { render_node_panel } from '../node_panel.js';

function sample_node() {
  return {
    id: 'level_1', scene: 'Level_1', name: 'Level 1',
    next: [{ id: 'level_2', condition: 'flags.is_beat' }],
    children: [],
  };
}

describe('render_node_panel', () => {
  test('shows the node\'s own scene and name', () => {
    const container = document.createElement('div');
    render_node_panel(container, sample_node());
    expect(container.querySelector('[data-field="scene"]').value).toBe('Level_1');
    expect(container.querySelector('[data-field="name"]').value).toBe('Level 1');
  });

  test('calls on_change with the updated node when a field is saved', () => {
    const container = document.createElement('div');
    let saved = null;
    render_node_panel(container, sample_node(), (node) => { saved = node; });
    container.querySelector('[data-field="scene"]').value = 'Level_1_New';
    container.querySelector('[data-action="save-node"]').click();
    expect(saved.scene).toBe('Level_1_New');
  });

  test('shows each existing next[] entry', () => {
    const container = document.createElement('div');
    render_node_panel(container, sample_node());
    const rows = container.querySelectorAll('[data-next-index]');
    expect(rows).toHaveLength(1);
    expect(rows[0].querySelector('[data-field="next-id"]').value).toBe('level_2');
  });

  test('adding a next entry appends it and calls on_change', () => {
    const container = document.createElement('div');
    let saved = null;
    render_node_panel(container, sample_node(), (node) => { saved = node; });
    container.querySelector('[data-action="add-next"]').click();
    expect(saved.next).toHaveLength(2);
  });

  test('removing a next entry drops it and calls on_change', () => {
    const container = document.createElement('div');
    let saved = null;
    render_node_panel(container, sample_node(), (node) => { saved = node; });
    container.querySelector('[data-next-index="0"] [data-action="remove-next"]').click();
    expect(saved.next).toHaveLength(0);
  });
});
