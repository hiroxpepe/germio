// @vitest-environment jsdom
import { describe, test, expect, vi, beforeEach } from 'vitest';

function sample_json_text() {
  return JSON.stringify({
    schema_version: 1,
    initial_state: { flags: { is_beat: false }, counters: {}, inventory: {}, persistence: {}, current_node: 'title' },
    root: {
      id: 'title', scene: 'Title', name: 'Title', rules: [], next: [],
      children: [
        {
          id: 'levels', scene: '', name: 'Levels', next: [],
          children: [],
          rules: [
            { id: 'rule_a', trigger: 'vol_home', condition: 'flags.is_beat', command: { request_notify: 'level_clear' }, once: false },
          ],
        },
      ],
    },
  });
}

// main.js reads these elements the instant it is imported, so they
// must exist in the document first — the same order a real browser
// loading index.html would use (the body first, then the script).
beforeEach(() => {
  document.body.innerHTML = `
    <button id="open-btn">Open</button>
    <button id="save-btn" disabled>Save</button>
    <button id="undo-btn" disabled>Undo</button>
    <button id="copy-log-btn">Copy log</button>
    <button id="download-log-btn">Download log</button>
    <button id="add-node-btn" disabled>+ add node</button>
    <button id="delete-node-btn" disabled>delete node</button>
    <button id="add-rule-btn" disabled>+ add rule</button>
    <div id="status"></div>
    <div id="tree"></div>
    <div id="tabs">
      <button data-tab="rules" aria-selected="true">Rules</button>
      <button data-tab="node" aria-selected="false">Node</button>
      <button data-tab="state" aria-selected="false">State</button>
    </div>
    <div class="tab-content active" id="tab-rules">
      <div id="rule-list"></div>
      <div id="rule-editor-panel"></div>
    </div>
    <div class="tab-content" id="tab-node">
      <div id="node-panel"></div>
    </div>
    <div class="tab-content" id="tab-state">
      <div id="state-panel"></div>
    </div>
    <div id="warnings"></div>
  `;
});

describe('main.js, wired end to end through a real (jsdom) page', () => {
  test('the editing buttons stay disabled until a file is actually opened', async () => {
    vi.resetModules();
    await import('../../../main.js');
    expect(document.getElementById('add-node-btn').disabled).toBe(true);
    expect(document.getElementById('delete-node-btn').disabled).toBe(true);
    expect(document.getElementById('add-rule-btn').disabled).toBe(true);
  });

  test('the editing buttons become enabled once a file has been opened', async () => {
    const fake_file = { text: () => Promise.resolve(sample_json_text()) };
    const fake_handle = { getFile: () => Promise.resolve(fake_file) };
    window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);

    vi.resetModules();
    await import('../../../main.js');
    document.getElementById('open-btn').click();
    // open_file's promise chain needs a tick to settle.
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));

    const tree_rows = document.querySelectorAll('#tree [data-node-id]');
    expect(tree_rows).toHaveLength(2);
    expect(document.getElementById('status').textContent).toContain('title');
    expect(document.getElementById('add-node-btn').disabled).toBe(false);
    expect(document.getElementById('delete-node-btn').disabled).toBe(false);
    expect(document.getElementById('add-rule-btn').disabled).toBe(false);
  });

  test('selecting the node with a rule shows that rule\'s card', async () => {
    const fake_file = { text: () => Promise.resolve(sample_json_text()) };
    const fake_handle = { getFile: () => Promise.resolve(fake_file) };
    window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);

    vi.resetModules();
    await import('../../../main.js');
    document.getElementById('open-btn').click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));

    document.querySelector('[data-node-id="levels"]').click();
    const rule_card = document.querySelector('[data-rule-id="rule_a"]');
    expect(rule_card).not.toBeNull();
    expect(rule_card.textContent).toContain('level_clear');
  });

  test('a real problem in the file shows up as a warning row', async () => {
    const broken_text = JSON.stringify({
      schema_version: 1,
      initial_state: { flags: {}, counters: {}, inventory: {}, persistence: {}, current_node: 'a' },
      root: { id: 'a', scene: '', name: '', rules: [], next: [], children: [] },
    });
    const fake_file = { text: () => Promise.resolve(broken_text) };
    const fake_handle = { getFile: () => Promise.resolve(fake_file) };
    window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);

    vi.resetModules();
    await import('../../../main.js');
    document.getElementById('open-btn').click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));

    // node "a" has no children, no scene, and no rules — a real V011/V021/V023 case.
    expect(document.getElementById('warnings').textContent).toContain('V011');
  });

  test('editing a rule through the real form updates the rule card', async () => {
    const fake_file = { text: () => Promise.resolve(sample_json_text()) };
    const fake_handle = { getFile: () => Promise.resolve(fake_file) };
    window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);

    vi.resetModules();
    await import('../../../main.js');
    document.getElementById('open-btn').click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));

    document.querySelector('[data-node-id="levels"]').click();
    document.querySelector('[data-rule-id="rule_a"]').click();
    document.querySelector('[data-field="trigger"]').value = 'signal_btn_start_pressed';
    document.querySelector('[data-action="save"]').click();

    expect(document.querySelector('[data-rule-id="rule_a"]').textContent).toContain('signal_btn_start_pressed');
  });

  test('adding a node with an id that already exists is rejected', async () => {
    const fake_file = { text: () => Promise.resolve(sample_json_text()) };
    const fake_handle = { getFile: () => Promise.resolve(fake_file) };
    window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);
    window.prompt = vi.fn().mockReturnValue('levels');
    window.alert = vi.fn();

    vi.resetModules();
    await import('../../../main.js');
    document.getElementById('open-btn').click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));

    document.getElementById('add-node-btn').click();

    expect(window.alert).toHaveBeenCalledOnce();
    expect(document.querySelectorAll('#tree [data-node-id]')).toHaveLength(2);
  });

  test('adding a node with a fresh id actually adds a new row to the tree', async () => {
    const fake_file = { text: () => Promise.resolve(sample_json_text()) };
    const fake_handle = { getFile: () => Promise.resolve(fake_file) };
    window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);
    window.prompt = vi.fn().mockReturnValue('ending');

    vi.resetModules();
    await import('../../../main.js');
    document.getElementById('open-btn').click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));

    document.getElementById('add-node-btn').click();

    expect(document.querySelectorAll('#tree [data-node-id]')).toHaveLength(3);
    expect(document.querySelector('[data-node-id="ending"]')).not.toBeNull();
  });

  test('Ctrl+Z undoes the most recent change, through the real keyboard listener', async () => {
    const fake_file = { text: () => Promise.resolve(sample_json_text()) };
    const fake_handle = { getFile: () => Promise.resolve(fake_file) };
    window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);
    window.prompt = vi.fn().mockReturnValue('ending');

    vi.resetModules();
    await import('../../../main.js');
    document.getElementById('open-btn').click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));

    document.getElementById('add-node-btn').click();
    expect(document.querySelectorAll('#tree [data-node-id]')).toHaveLength(3);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'z', ctrlKey: true }));

    expect(document.querySelectorAll('#tree [data-node-id]')).toHaveLength(2);
  });

  test('a malicious node.name in the loaded file cannot break out of its own input field', async () => {
    const evil_text = JSON.stringify({
      schema_version: 1,
      initial_state: { flags: {}, counters: {}, inventory: {}, persistence: {}, current_node: 'title' },
      root: {
        id: 'title', scene: 'Title', name: '" onfocus="window.__pwned = true" autofocus="',
        rules: [], next: [], children: [],
      },
    });
    const fake_file = { text: () => Promise.resolve(evil_text) };
    const fake_handle = { getFile: () => Promise.resolve(fake_file) };
    window.showOpenFilePicker = vi.fn().mockResolvedValue([fake_handle]);

    vi.resetModules();
    await import('../../../main.js');
    document.getElementById('open-btn').click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));

    // Switch to the Node tab, where node.name is rendered into a real input field.
    document.querySelector('[data-tab="node"]').click();

    const name_field = document.querySelector('#node-panel [data-field="name"]');
    // The whole malicious string must sit inside the field's own value,
    // never break out to add a second, real HTML attribute.
    expect(name_field.value).toBe('" onfocus="window.__pwned = true" autofocus="');
    expect(name_field.attributes.length).toBe(3); // type, data-field, value — nothing extra injected
    expect(window.__pwned).toBeUndefined();
  });
});
