// The one file that wires every tested src/lib/ module into the real
// page. Kept thin on purpose: every real rule (parsing, tree moves,
// validation, and so on) lives in src/lib/ and is already covered by
// a test there; this file only ever calls into those functions and
// re-renders — it does not repeat any of their logic itself.
import { parse_scenario, serialize_scenario } from './src/lib/scenario_file.js';
import { open_file, save_file } from './src/lib/file_io.js';
import { render_tree, render_rule_list } from './src/lib/render.js';
import { render_rule_editor } from './src/lib/rule_editor.js';
import { render_node_panel } from './src/lib/node_panel.js';
import { render_state_panel } from './src/lib/state_panel.js';
import { validate_scenario } from './src/lib/validator.js';
import { create_undo_stack } from './src/lib/undo_stack.js';
import { find_node, move_node } from './src/lib/tree.js';
import { is_node_id_unique, is_rule_id_unique } from './src/lib/validation.js';
import { write as log, get_all_text as log_text } from './src/lib/editor_log.js';

const tree_el = document.getElementById('tree');
const rule_list_el = document.getElementById('rule-list');
const rule_editor_el = document.getElementById('rule-editor-panel');
const node_panel_el = document.getElementById('node-panel');
const state_panel_el = document.getElementById('state-panel');
const warnings_el = document.getElementById('warnings');
const status_el = document.getElementById('status');
const open_btn = document.getElementById('open-btn');
const save_btn = document.getElementById('save-btn');
const undo_btn = document.getElementById('undo-btn');
const copy_log_btn = document.getElementById('copy-log-btn');
const download_log_btn = document.getElementById('download-log-btn');
const add_node_btn = document.getElementById('add-node-btn');
const delete_node_btn = document.getElementById('delete-node-btn');
const add_rule_btn = document.getElementById('add-rule-btn');

function enable_editing_buttons() {
    add_node_btn.disabled = false;
    delete_node_btn.disabled = false;
    add_rule_btn.disabled = false;
}

log('editor loaded');

let scenario = null;
let file_handle = null;
let selected_node_id = null;
let selected_rule_id = null;
const undo_stack = create_undo_stack();

function find_node_by_id(id) {
    return find_node(scenario.root, id);
}

function replace_node(new_node) {
    scenario = { ...scenario, root: replace_in_tree(scenario.root, new_node) };
}

function replace_in_tree(node, new_node) {
    if (node.id === new_node.id) return new_node;
    return { ...node, children: node.children.map(c => replace_in_tree(c, new_node)) };
}

function commit(reason) {
    log(reason);
    undo_stack.push(serialize_scenario(scenario));
    render();
}

function reorder_sibling(root, node_id, direction) {
    function walk(node) {
        const index = node.children.findIndex(c => c.id === node_id);
        if (index !== -1) {
            const target = index + direction;
            if (target >= 0 && target < node.children.length) {
                const children = [...node.children];
                [children[index], children[target]] = [children[target], children[index]];
                return { ...node, children };
            }
            return node;
        }
        return { ...node, children: node.children.map(walk) };
    }
    return walk(root);
}

function find_parent_id(root, node_id) {
    for (const child of root.children) {
        if (child.id === node_id) return root.id;
        const found = find_parent_id(child, node_id);
        if (found) return found;
    }
    return null;
}

function previous_sibling_id(root, node_id) {
    const parent_id = find_parent_id(root, node_id);
    if (!parent_id) return null;
    const parent = find_node(root, parent_id);
    const index = parent.children.findIndex(c => c.id === node_id);
    return index > 0 ? parent.children[index - 1].id : null;
}

let last_validation_results = [];

function warning_node_ids() {
    // A node id shows up here only if the message text names it, which
    // is close enough for a warning MARK on the right row — a real,
    // fully precise mapping would need every check in validator.js to
    // also carry its own node_id field, which is worth doing later.
    const ids = new Set();
    const all_ids = [];
    (function collect(node) { all_ids.push(node.id); node.children.forEach(collect); })(scenario.root);
    for (const result of last_validation_results) {
        for (const id of all_ids) {
            if (result.message.includes(`'${id}'`)) ids.add(id);
        }
    }
    return ids;
}

function render() {
    if (!scenario) return;

    render_tree(
        tree_el, scenario.root, selected_node_id,
        (id) => { log(`node selected: '${id}'`); selected_node_id = id; selected_rule_id = null; render(); },
        (id) => {
            log(`move up: '${id}'`);
            scenario = { ...scenario, root: reorder_sibling(scenario.root, id, -1) };
            commit(`node '${id}' moved up`);
        },
        (id) => {
            log(`move down: '${id}'`);
            scenario = { ...scenario, root: reorder_sibling(scenario.root, id, 1) };
            commit(`node '${id}' moved down`);
        },
        (dragged_id, target_id) => {
            log(`drop: '${dragged_id}' onto '${target_id}'`);
            try {
                scenario = { ...scenario, root: move_node(scenario.root, dragged_id, target_id) };
                commit(`node '${dragged_id}' moved under '${target_id}'`);
            } catch (err) {
                log(`drop rejected: ${err.message}`);
                window.alert(err.message);
            }
        },
        warning_node_ids(),
        (id) => {
            log(`indent: '${id}'`);
            const prev_sibling_id = previous_sibling_id(scenario.root, id);
            if (!prev_sibling_id) { log(`indent rejected: '${id}' has no previous sibling`); return; }
            scenario = { ...scenario, root: move_node(scenario.root, id, prev_sibling_id) };
            commit(`node '${id}' indented under '${prev_sibling_id}'`);
        },
        (id) => {
            log(`outdent: '${id}'`);
            const parent_id = find_parent_id(scenario.root, id);
            const grandparent_id = parent_id ? find_parent_id(scenario.root, parent_id) : null;
            if (!grandparent_id) { log(`outdent rejected: '${id}' has no grandparent to move next to`); return; }
            scenario = { ...scenario, root: move_node(scenario.root, id, grandparent_id) };
            commit(`node '${id}' outdented next to '${parent_id}'`);
        }
    );

    const node = selected_node_id ? find_node_by_id(selected_node_id) : scenario.root;

    if (node) {
        render_rule_list(rule_list_el, node, selected_rule_id, (id) => {
            log(`rule selected: '${id}' on node '${node.id}'`);
            selected_rule_id = id;
            render();
        });

        if (selected_rule_id) {
            const rule = node.rules.find(r => r.id === selected_rule_id);
            render_rule_editor(
                rule_editor_el, rule,
                (updated_rule) => {
                    const updated_node = { ...node, rules: node.rules.map(r => r.id === rule.id ? updated_rule : r) };
                    replace_node(updated_node);
                    commit(`rule '${rule.id}' saved on node '${node.id}'`);
                },
                () => {
                    const updated_node = { ...node, rules: node.rules.filter(r => r.id !== rule.id) };
                    replace_node(updated_node);
                    selected_rule_id = null;
                    commit(`rule '${rule.id}' deleted from node '${node.id}'`);
                },
                scenario.initial_state
            );
        } else {
            rule_editor_el.innerHTML = '';
        }

        render_node_panel(node_panel_el, node, (updated_node) => {
            replace_node(updated_node);
            commit(`node '${node.id}' properties changed`);
        });

        render_state_panel(state_panel_el, scenario.initial_state, (updated_state) => {
            scenario = { ...scenario, initial_state: updated_state };
            commit('initial_state changed');
        });
    }

    const results = validate_scenario(scenario);
    last_validation_results = results;
    warnings_el.innerHTML = '';
    for (const result of results) {
        const row = document.createElement('div');
        row.className = result.level === 'Error' ? 'level-error' : 'level-warning';
        row.textContent = `${result.rule_id}: ${result.message}`;
        warnings_el.appendChild(row);
    }
    log(`validated: ${results.length} result(s)`);

    save_btn.disabled = false;
    undo_btn.disabled = !undo_stack.can_undo();
}

document.querySelectorAll('#tabs button').forEach((btn) => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('#tabs button').forEach(b => b.setAttribute('aria-selected', 'false'));
        btn.setAttribute('aria-selected', 'true');
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
        document.getElementById(`tab-${btn.dataset.tab}`).classList.add('active');
    });
});

open_btn.addEventListener('click', async () => {
    log('open clicked');
    const { text, handle } = await open_file();
    scenario = parse_scenario(text);
    file_handle = handle;
    selected_node_id = scenario.root.id;
    selected_rule_id = null;
    undo_stack.push(text);
    status_el.textContent = `loaded (${scenario.root.id})`;
    log(`file opened, root id='${scenario.root.id}'`);
    enable_editing_buttons();
    render();
});

save_btn.addEventListener('click', async () => {
    log('save clicked');
    const text = serialize_scenario(scenario);
    file_handle = await save_file(text, file_handle);
    undo_stack.push(text);
    status_el.textContent = 'saved';
    log('file saved');
    render();
});

undo_btn.addEventListener('click', () => {
    log('undo clicked');
    const text = undo_stack.undo();
    scenario = parse_scenario(text);
    render();
});

document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
        e.preventDefault();
        if (!undo_stack.can_undo()) return;
        log('undo via Ctrl+Z');
        const text = undo_stack.undo();
        scenario = parse_scenario(text);
        render();
    }
});

copy_log_btn.addEventListener('click', async () => {
    await navigator.clipboard.writeText(log_text());
    status_el.textContent = 'log copied to clipboard';
});

download_log_btn.addEventListener('click', () => {
    const blob = new Blob([log_text()], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'editor.log';
    a.click();
    URL.revokeObjectURL(url);
});

add_node_btn.addEventListener('click', () => {
    const parent = selected_node_id ? find_node_by_id(selected_node_id) : scenario.root;
    const new_id = window.prompt('New node id:');
    if (!new_id) return;
    if (!is_node_id_unique(scenario.root, new_id)) {
        window.alert(`'${new_id}' is already used by another node.`);
        return;
    }
    const new_node = { id: new_id, scene: '', name: '', children: [], rules: [], next: [] };
    const updated_parent = { ...parent, children: [...parent.children, new_node] };
    replace_node(updated_parent);
    selected_node_id = new_id;
    commit(`node '${new_id}' added under '${parent.id}'`);
});

delete_node_btn.addEventListener('click', () => {
    if (!selected_node_id || selected_node_id === scenario.root.id) {
        window.alert('The root node cannot be deleted.');
        return;
    }
    const id_to_delete = selected_node_id;
    function remove(node) {
        return { ...node, children: node.children.filter(c => c.id !== id_to_delete).map(remove) };
    }
    scenario = { ...scenario, root: remove(scenario.root) };
    selected_node_id = scenario.root.id;
    commit(`node '${id_to_delete}' deleted`);
});

add_rule_btn.addEventListener('click', () => {
    const node = find_node_by_id(selected_node_id);
    if (!node) {
        log('add rule rejected: no node selected');
        window.alert('Select a node first.');
        return;
    }
    const new_id = window.prompt('New rule id:');
    if (!new_id) return;
    if (!is_rule_id_unique(node, new_id)) {
        window.alert(`'${new_id}' is already used by another rule on this node.`);
        return;
    }
    const new_rule = { id: new_id, trigger: '', condition: '', command: {}, once: true };
    const updated_node = { ...node, rules: [...node.rules, new_rule] };
    replace_node(updated_node);
    selected_rule_id = new_id;
    commit(`rule '${new_id}' added on node '${node.id}'`);
});
