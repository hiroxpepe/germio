// The State tab: flags, counters, inventory, persistence, each shown
// as a plain list with an add-row form. Wired to node_editing.js's
// set_state_entry / remove_state_entry so the actual data changes
// stay in the already-tested library, not duplicated here.
import { set_state_entry, remove_state_entry } from './node_editing.js';
import { escape_html, escape_attr } from './escape.js';

const CATEGORIES = ['flags', 'counters', 'inventory', 'persistence'];

function parse_value(category, raw) {
    if (category === 'flags') return raw === 'true';
    if (category === 'counters' || category === 'inventory') return Number(raw);
    return raw;
}

export function render_state_panel(container, state, on_change) {
    container.innerHTML = `
    <div>
      <label>current_node</label>
      <input type="text" data-field="current_node" value="${escape_attr(state.current_node || '')}" />
      <button data-action="save-current-node">Save</button>
    </div>
    ${CATEGORIES.map(category => `
    <div>
      <div class="section-label">${category}</div>
      <div data-list="${category}"></div>
      <input type="text" placeholder="key" data-add-key="${category}" />
      <input type="text" placeholder="value" data-add-value="${category}" />
      <button data-action="add-${category}">+ Add</button>
    </div>
  `).join('')}
  `;

    container.querySelector('[data-action="save-current-node"]').addEventListener('click', () => {
        if (!on_change) return;
        on_change({ ...state, current_node: container.querySelector('[data-field="current_node"]').value });
    });

    for (const category of CATEGORIES) {
        const list_el = container.querySelector(`[data-list="${category}"]`);
        list_el.innerHTML = Object.entries(state[category]).map(([key, value]) => `
      <div data-entry-category="${category}" data-entry-key="${escape_attr(key)}">
        ${escape_html(key)}: ${escape_html(String(value))} <button data-action="remove">remove</button>
      </div>
    `).join('');

        list_el.querySelectorAll('[data-entry-category]').forEach((row) => {
            row.querySelector('[data-action="remove"]').addEventListener('click', () => {
                if (on_change) on_change(remove_state_entry(state, category, row.dataset.entryKey));
            });
        });

        container.querySelector(`[data-action="add-${category}"]`).addEventListener('click', () => {
            const key = container.querySelector(`[data-add-key="${category}"]`).value;
            const raw_value = container.querySelector(`[data-add-value="${category}"]`).value;
            if (!key) return;
            if (on_change) on_change(set_state_entry(state, category, key, parse_value(category, raw_value)));
        });
    }
}
