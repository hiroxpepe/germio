// @vitest-environment jsdom
import { describe, test, expect } from 'vitest';
import { render_state_panel } from '../state_panel.js';

function sample_state() {
    return {
        flags: { is_beat: false }, counters: { score: 0 }, inventory: {}, persistence: {}, current_node: 'title',
    };
}

describe('render_state_panel', () => {
    test('shows current_node as a plain editable field', () => {
        const container = document.createElement('div');
        render_state_panel(container, sample_state());
        expect(container.querySelector('[data-field="current_node"]').value).toBe('title');
    });

    test('changing current_node calls on_change with it updated', () => {
        const container = document.createElement('div');
        let saved = null;
        render_state_panel(container, sample_state(), (state) => { saved = state; });
        container.querySelector('[data-field="current_node"]').value = 'level_1';
        container.querySelector('[data-action="save-current-node"]').click();
        expect(saved.current_node).toBe('level_1');
    });

    test('shows every existing flag with its key and value', () => {
        const container = document.createElement('div');
        render_state_panel(container, sample_state());
        const row = container.querySelector('[data-entry-category="flags"][data-entry-key="is_beat"]');
        expect(row).not.toBeNull();
    });

    test('adding a new counter calls on_change with it set', () => {
        const container = document.createElement('div');
        let saved = null;
        render_state_panel(container, sample_state(), (state) => { saved = state; });

        container.querySelector('[data-add-key="counters"]').value = 'lives';
        container.querySelector('[data-add-value="counters"]').value = '3';
        container.querySelector('[data-action="add-counters"]').click();

        expect(saved.counters.lives).toBe(3);
    });

    test('removing an existing flag calls on_change without it', () => {
        const container = document.createElement('div');
        let saved = null;
        render_state_panel(container, sample_state(), (state) => { saved = state; });
        container.querySelector('[data-entry-category="flags"][data-entry-key="is_beat"] [data-action="remove"]').click();
        expect(saved.flags).not.toHaveProperty('is_beat');
    });
});
