// The Node's own property panel: scene, name, and its next[] list.
// Wired to node_editing.js's pure functions so this file only ever
// builds the DOM and reads what a person typed — the actual data
// changes always happen in the already-tested library functions.
import { update_node_field, add_next_entry, remove_next_entry } from './node_editing.js';
import { escape_attr } from './escape.js';

export function render_node_panel(container, node, on_change) {
    container.innerHTML = `
    <div><label>scene <input type="text" data-field="scene" value="${escape_attr(node.scene || '')}" /></label></div>
    <div><label>name <input type="text" data-field="name" value="${escape_attr(node.name || '')}" /></label></div>
    <button data-action="save-node">save node</button>
    <div id="next-list"></div>
    <button data-action="add-next">+ add next</button>
  `;

    container.querySelector('[data-action="save-node"]').addEventListener('click', () => {
        if (!on_change) return;
        let updated = update_node_field({ ...node, children: [] }, node.id, 'scene', container.querySelector('[data-field="scene"]').value);
        updated = update_node_field(updated, node.id, 'name', container.querySelector('[data-field="name"]').value);
        on_change({ ...node, scene: updated.scene, name: updated.name });
    });

    const next_list_el = container.querySelector('#next-list');
    next_list_el.innerHTML = node.next.map((entry, index) => `
    <div data-next-index="${index}">
      <input type="text" data-field="next-id" value="${escape_attr(entry.id)}" />
      <input type="text" data-field="next-condition" value="${escape_attr(entry.condition || '')}" />
      <button data-action="remove-next">remove</button>
    </div>
  `).join('');

    next_list_el.querySelectorAll('[data-next-index]').forEach((row) => {
        const index = Number(row.dataset.nextIndex);
        row.querySelector('[data-action="remove-next"]').addEventListener('click', () => {
            if (on_change) on_change(remove_next_entry(node, index));
        });
    });

    container.querySelector('[data-action="add-next"]').addEventListener('click', () => {
        if (on_change) on_change(add_next_entry(node, '', ''));
    });
}
