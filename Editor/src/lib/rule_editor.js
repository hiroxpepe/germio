// The full rule-editing form: a checklist of every Command kind, each
// with its own small set of fields, wired to command_form.js's pure
// read/write functions so the editor never re-implements that logic.
import { command_to_form_state, form_state_to_command } from './command_form.js';
import { check_condition_syntax } from './condition_syntax.js';
import { escape_attr } from './escape.js';

const KIND_FIELDS = {
  set_flag: [{ key: 'key', label: 'key', type: 'text' }, { key: 'value', label: 'value', type: 'checkbox' }],
  update_counter: [
    { key: 'key', label: 'key', type: 'text' }, { key: 'delta', label: 'delta', type: 'number' },
    { key: 'op', label: 'op', type: 'select', options: ['Add', 'Subtract', 'Set'] },
  ],
  update_inventory: [{ key: 'key', label: 'key', type: 'text' }, { key: 'delta', label: 'delta', type: 'number' }],
  request_transition: [{ key: 'value', label: 'value', type: 'text' }],
  request_notify: [{ key: 'value', label: 'value', type: 'text' }],
  set_persistence: [{ key: 'key', label: 'key', type: 'text' }, { key: 'value', label: 'value', type: 'text' }],
  record_event: [{ key: 'kind', label: 'kind', type: 'text' }, { key: 'target_id', label: 'target_id', type: 'text' }],
  reset_flags: [],
  reset_counters: [],
  reset_inventory: [],
};

function field_input(kind, field, current) {
  const value = current !== undefined ? current : '';
  if (field.type === 'checkbox') {
    return `<label>${field.label} <input type="checkbox" data-kind-field="${kind}:${field.key}" ${value ? 'checked' : ''} /></label>`;
  }
  if (field.type === 'select') {
    const options = field.options.map(o => `<option value="${escape_attr(o)}" ${o === value ? 'selected' : ''}>${o}</option>`).join('');
    return `<label>${field.label} <select data-kind-field="${kind}:${field.key}">${options}</select></label>`;
  }
  const value_attr = kind in { request_transition: 1, request_notify: 1 } && field.key === 'value'
    ? ` data-value-field="${kind}"` : '';
  return `<label>${field.label} <input type="${field.type}" data-kind-field="${kind}:${field.key}"${value_attr} value="${escape_attr(value)}" /></label>`;
}

export function render_rule_editor(container, rule, on_save, on_delete, known_state) {
  const state = command_to_form_state(rule.command);
  const known_keys = known_state
    ? [
        ...Object.keys(known_state.flags || {}).map(k => `flags.${k}`),
        ...Object.keys(known_state.counters || {}).map(k => `counters.${k}`),
        ...Object.keys(known_state.inventory || {}).map(k => `inventory.${k}`),
      ]
    : [];
  container.innerHTML = `
    <div><label>trigger <input type="text" data-field="trigger" value="${escape_attr(rule.trigger)}" /></label></div>
    <div>
      <label>condition <input type="text" data-field="condition" value="${escape_attr(rule.condition || '')}" list="known-condition-keys" /></label>
      <datalist id="known-condition-keys">
        ${known_keys.map(k => `<option value="${escape_attr(k)}"></option>`).join('')}
      </datalist>
    </div>
    <div><label>once <input type="checkbox" data-field="once" ${rule.once ? 'checked' : ''} /></label></div>
    <div id="command-kinds"></div>
    <div data-condition-error style="color: #a02020;"></div>
    <button data-action="save">save</button>
    <button data-action="delete">delete rule</button>
  `;

  const kinds_el = container.querySelector('#command-kinds');
  const enabled_kinds = { ...Object.fromEntries(Object.keys(KIND_FIELDS).map(k => [k, state[k].enabled])) };

  function render_kinds() {
    kinds_el.innerHTML = Object.entries(KIND_FIELDS).map(([kind, fields]) => {
      const enabled = enabled_kinds[kind];
      const field_html = enabled
        ? fields.map(f => field_input(kind, f, state[kind][f.key])).join(' ')
        : '';
      return `<div>
        <label><input type="checkbox" data-command-kind="${kind}" ${enabled ? 'checked' : ''} /> ${kind}</label>
        ${field_html}
      </div>`;
    }).join('');

    for (const kind of Object.keys(KIND_FIELDS)) {
      kinds_el.querySelector(`[data-command-kind="${kind}"]`).addEventListener('change', (e) => {
        enabled_kinds[kind] = e.target.checked;
        render_kinds();
      });
    }
  }
  render_kinds();

  function read_form_state() {
    const read_state = command_to_form_state({});
    for (const [kind, fields] of Object.entries(KIND_FIELDS)) {
      read_state[kind].enabled = enabled_kinds[kind];
      for (const field of fields) {
        const el = kinds_el.querySelector(`[data-kind-field="${kind}:${field.key}"]`);
        if (!el) continue;
        read_state[kind][field.key] = field.type === 'checkbox' ? el.checked
          : field.type === 'number' ? Number(el.value) : el.value;
      }
    }
    return read_state;
  }

  container.querySelector('[data-action="save"]').addEventListener('click', () => {
    const condition = container.querySelector('[data-field="condition"]').value;
    const error = check_condition_syntax(condition);
    const error_el = container.querySelector('[data-condition-error]');
    if (error) {
      error_el.textContent = error;
      return;
    }
    error_el.textContent = '';
    if (!on_save) return;
    on_save({
      ...rule,
      trigger: container.querySelector('[data-field="trigger"]').value,
      condition,
      once: container.querySelector('[data-field="once"]').checked,
      command: form_state_to_command(read_form_state()),
    });
  });

  container.querySelector('[data-action="delete"]').addEventListener('click', () => {
    if (on_delete) on_delete();
  });
}
