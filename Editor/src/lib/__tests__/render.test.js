// @vitest-environment jsdom
import { describe, test, expect } from 'vitest';
import { render_tree, render_rule_list } from '../render.js';

function sample_tree() {
    return {
        id: 'title', children: [
            { id: 'levels', children: [{ id: 'level_1', children: [] }] },
        ],
    };
}

describe('render_tree', () => {
    test('renders one row per node, in document order', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), null);
        const rows = container.querySelectorAll('[data-node-id]');
        expect(rows).toHaveLength(3);
    });

    test('marks the selected node\'s row so it can be styled apart from the rest', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), 'level_1');
        const selected = container.querySelector('[data-node-id="level_1"]');
        expect(selected.getAttribute('aria-selected')).toBe('true');
    });

    test('calls on_select with the clicked node\'s id', () => {
        const container = document.createElement('div');
        let clicked_id = null;
        render_tree(container, sample_tree(), null, (id) => { clicked_id = id; });
        container.querySelector('[data-node-id="levels"]').click();
        expect(clicked_id).toBe('levels');
    });

    test('gives every row an up and a down button, for moving without a mouse', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), null);
        const row = container.querySelector('[data-node-id="levels"]');
        expect(row.querySelector('[data-action="move-up"]')).not.toBeNull();
        expect(row.querySelector('[data-action="move-down"]')).not.toBeNull();
    });

    test('clicking a row\'s up button calls on_move_up with that row\'s id', () => {
        const container = document.createElement('div');
        let moved_id = null;
        render_tree(container, sample_tree(), null, null, (id) => { moved_id = id; });
        container.querySelector('[data-node-id="levels"] [data-action="move-up"]').click();
        expect(moved_id).toBe('levels');
    });

    test('clicking a row\'s down button calls on_move_down with that row\'s id', () => {
        const container = document.createElement('div');
        let moved_id = null;
        render_tree(container, sample_tree(), null, null, null, (id) => { moved_id = id; });
        container.querySelector('[data-node-id="levels"] [data-action="move-down"]').click();
        expect(moved_id).toBe('levels');
    });

    test('every row is draggable, for moving with a mouse', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), null);
        const row = container.querySelector('[data-node-id="levels"]');
        expect(row.draggable).toBe(true);
    });

    test('every row gets the node-row class, for the tool\'s own styling', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), null);
        const row = container.querySelector('[data-node-id="levels"]');
        expect(row.classList.contains('node-row')).toBe(true);
    });

    test('the container itself gets role="tree", for a real accessibility tree', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), null);
        expect(container.getAttribute('role')).toBe('tree');
    });

    test('a node with a warning gets a small warning mark on its own row', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), null, null, null, null, null, new Set(['levels']));
        const row = container.querySelector('[data-node-id="levels"]');
        expect(row.querySelector('[data-warning-mark]')).not.toBeNull();
    });

    test('a node with no warning gets no warning mark', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), null, null, null, null, null, new Set(['levels']));
        const row = container.querySelector('[data-node-id="level_1"]');
        expect(row.querySelector('[data-warning-mark]')).toBeNull();
    });

    test('gives every row an indent and an outdent button, for moving hierarchy without a mouse', () => {
        const container = document.createElement('div');
        render_tree(container, sample_tree(), null);
        const row = container.querySelector('[data-node-id="levels"]');
        expect(row.querySelector('[data-action="indent"]')).not.toBeNull();
        expect(row.querySelector('[data-action="outdent"]')).not.toBeNull();
    });

    test('clicking a row\'s indent button calls on_indent with that row\'s id', () => {
        const container = document.createElement('div');
        let indented_id = null;
        render_tree(container, sample_tree(), null, null, null, null, null, null, (id) => { indented_id = id; });
        container.querySelector('[data-node-id="levels"] [data-action="indent"]').click();
        expect(indented_id).toBe('levels');
    });

    test('clicking a row\'s outdent button calls on_outdent with that row\'s id', () => {
        const container = document.createElement('div');
        let outdented_id = null;
        render_tree(container, sample_tree(), null, null, null, null, null, null, null, (id) => { outdented_id = id; });
        container.querySelector('[data-node-id="levels"] [data-action="outdent"]').click();
        expect(outdented_id).toBe('levels');
    });
});

describe('render_rule_list', () => {
    test('renders one card per rule, with its summary text', () => {
        const container = document.createElement('div');
        const node = {
            id: 'level_1',
            rules: [
                { id: 'rule_a', trigger: 'vol_home', command: { request_notify: 'level_clear' }, once: true },
            ],
        };
        render_rule_list(container, node, null);
        const card = container.querySelector('[data-rule-id="rule_a"]');
        expect(card.textContent).toContain('vol_home');
        expect(card.textContent).toContain('level_clear');
    });

    test('gives each rule card a colored badge per command kind it uses', () => {
        const container = document.createElement('div');
        const node = {
            id: 'level_1',
            rules: [
                { id: 'rule_a', trigger: 'vol_home', command: { request_notify: 'level_clear' }, once: true },
            ],
        };
        render_rule_list(container, node, null);
        const card = container.querySelector('[data-rule-id="rule_a"]');
        expect(card.querySelector('.badge-request_notify')).not.toBeNull();
    });

    test('shows a plain message when the node has no rules yet', () => {
        const container = document.createElement('div');
        render_rule_list(container, { id: 'title', rules: [] }, null);
        expect(container.textContent).toContain('no rules');
    });

    test('calls on_select with the clicked rule\'s id', () => {
        const container = document.createElement('div');
        const node = { id: 'level_1', rules: [{ id: 'rule_a', trigger: 't', command: {}, once: true }] };
        let clicked_id = null;
        render_rule_list(container, node, null, (id) => { clicked_id = id; });
        container.querySelector('[data-rule-id="rule_a"]').click();
        expect(clicked_id).toBe('rule_a');
    });
});
