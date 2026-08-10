// The one place real DOM elements get built. Every function here
// takes a plain data object and a container element, and only ever
// writes into that container — no global state, no other side effect
// — so each one can be checked on its own with jsdom.
import { escape_html } from './escape.js';

const COMMAND_KIND_FIELDS = [
    'set_flag', 'update_counter', 'update_inventory',
    'request_transition', 'request_notify', 'set_persistence', 'record_event',
];

function command_kind_badges(command) {
    return COMMAND_KIND_FIELDS
        .filter(kind => command[kind] !== undefined && command[kind] !== null)
        .map(kind => `<span class="badge badge-${kind}">${kind.replace('request_', '').replace('_', ' ')}</span>`)
        .join('');
}

// The first command field a rule actually has set, shown as the
// card's own "value" — a rule combining more than one kind still
// shows every kind as its own badge; this is only for the single
// right-aligned value shown next to them.
function main_command_value(command) {
    for (const kind of COMMAND_KIND_FIELDS) {
        if (command[kind] === undefined || command[kind] === null) continue;
        const value = command[kind];
        return typeof value === 'object' ? JSON.stringify(value) : String(value);
    }
    return '';
}

function walk_for_rows(node, depth, rows) {
    rows.push({ id: node.id, depth });
    for (const child of node.children) walk_for_rows(child, depth + 1, rows);
}

/**
 * Renders one row per node in the tree, indented to match depth.
 * on_select is called with a node's id when its row is clicked.
 * on_move_up / on_move_down reorder a node among its own siblings.
 * on_drop, if given, is called with (dragged_id, target_id) when one
 * row is dropped onto another. warning_ids, if given, is a Set of
 * node ids that should show a small warning mark on their own row
 * (fed from validate_scenario's own results). on_indent / on_outdent
 * move a node's place in the hierarchy without a mouse.
 */
export function render_tree(
    container, root, selected_id, on_select, on_move_up, on_move_down, on_drop,
    warning_ids, on_indent, on_outdent
) {
    container.innerHTML = '';
    container.setAttribute('role', 'tree');
    const rows = [];
    walk_for_rows(root, 0, rows);
    let drag_id = null;
    for (const row of rows) {
        const el = document.createElement('div');
        el.dataset.nodeId = row.id;
        el.className = 'node-row';
        el.draggable = true;
        el.style.paddingLeft = `${row.depth * 14}px`;
        el.setAttribute('role', 'treeitem');
        el.setAttribute('aria-selected', row.id === selected_id ? 'true' : 'false');
        if (on_select) el.addEventListener('click', () => on_select(row.id));

        if (warning_ids && warning_ids.has(row.id)) {
            const mark = document.createElement('span');
            mark.dataset.warningMark = 'true';
            mark.textContent = '\u26a0';
            mark.title = 'this node has a Validator warning or error';
            el.appendChild(mark);
        }

        const label = document.createElement('span');
        label.textContent = row.id;
        el.appendChild(label);

        const up_btn = document.createElement('button');
        up_btn.dataset.action = 'move-up';
        up_btn.textContent = '\u2191';
        if (on_move_up) up_btn.addEventListener('click', (e) => { e.stopPropagation(); on_move_up(row.id); });
        el.appendChild(up_btn);

        const down_btn = document.createElement('button');
        down_btn.dataset.action = 'move-down';
        down_btn.textContent = '\u2193';
        if (on_move_down) down_btn.addEventListener('click', (e) => { e.stopPropagation(); on_move_down(row.id); });
        el.appendChild(down_btn);

        const indent_btn = document.createElement('button');
        indent_btn.dataset.action = 'indent';
        indent_btn.textContent = '\u2192';
        indent_btn.title = 'indent: move under the previous sibling';
        if (on_indent) indent_btn.addEventListener('click', (e) => { e.stopPropagation(); on_indent(row.id); });
        el.appendChild(indent_btn);

        const outdent_btn = document.createElement('button');
        outdent_btn.dataset.action = 'outdent';
        outdent_btn.textContent = '\u2190';
        outdent_btn.title = 'outdent: move up next to the parent';
        if (on_outdent) outdent_btn.addEventListener('click', (e) => { e.stopPropagation(); on_outdent(row.id); });
        el.appendChild(outdent_btn);

        el.addEventListener('dragstart', () => { drag_id = row.id; });
        el.addEventListener('dragover', (e) => e.preventDefault());
        el.addEventListener('drop', (e) => {
            e.preventDefault();
            if (on_drop && drag_id && drag_id !== row.id) on_drop(drag_id, row.id);
            drag_id = null;
        });

        container.appendChild(el);
    }
}

/**
 * Renders one card per rule on the given node: a colored badge per
 * for the card's own text. on_select, if given, is called with a
 * rule's id when its card is clicked.
 */
export function render_rule_list(container, node, selected_id, on_select) {
    container.innerHTML = '';
    if (node.rules.length === 0) {
        const empty = document.createElement('div');
        empty.textContent = 'no rules on this node yet';
        container.appendChild(empty);
        return;
    }
    for (const rule of node.rules) {
        const card = document.createElement('div');
        card.className = 'rule-card';
        card.dataset.ruleId = rule.id;
        const command = rule.command || {};
        const badges = command_kind_badges(command);
        card.innerHTML = `${badges}<span data-trigger>${escape_html(rule.trigger)}</span><span data-command-value>${escape_html(main_command_value(command))}</span>`;
        card.setAttribute('aria-selected', rule.id === selected_id ? 'true' : 'false');
        if (on_select) card.addEventListener('click', () => on_select(rule.id));
        container.appendChild(card);
    }
}
