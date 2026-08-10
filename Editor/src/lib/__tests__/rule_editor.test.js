// @vitest-environment jsdom
import { describe, test, expect, vi } from 'vitest';
import { render_rule_editor } from '../rule_editor.js';

function sample_rule() {
    return {
        id: 'rule_a', trigger: 'vol_home', condition: 'flags.is_beat',
        command: { request_notify: 'level_clear' }, once: true,
    };
}

describe('render_rule_editor', () => {
    test('shows the rule\'s current trigger and condition in the fields', () => {
        const container = document.createElement('div');
        render_rule_editor(container, sample_rule());
        expect(container.querySelector('[data-field="trigger"]').value).toBe('vol_home');
        expect(container.querySelector('[data-field="condition"]').value).toBe('flags.is_beat');
    });

    test('checks the box for a command kind the rule already has set', () => {
        const container = document.createElement('div');
        render_rule_editor(container, sample_rule());
        expect(container.querySelector('[data-command-kind="request_notify"]').checked).toBe(true);
        expect(container.querySelector('[data-command-kind="set_flag"]').checked).toBe(false);
    });

    test('calls on_save with an updated rule when Save is clicked', () => {
        const container = document.createElement('div');
        let saved_rule = null;
        render_rule_editor(container, sample_rule(), (rule) => { saved_rule = rule; });

        container.querySelector('[data-field="trigger"]').value = 'signal_btn_start_pressed';
        container.querySelector('[data-action="save"]').click();

        expect(saved_rule.trigger).toBe('signal_btn_start_pressed');
        expect(saved_rule.command).toEqual({ request_notify: 'level_clear' });
    });

    test('turning on a second command kind adds it alongside the first on save', () => {
        const container = document.createElement('div');
        let saved_rule = null;
        render_rule_editor(container, sample_rule(), (rule) => { saved_rule = rule; });

        container.querySelector('[data-command-kind="request_transition"]').checked = true;
        container.querySelector('[data-command-kind="request_transition"]').dispatchEvent(new Event('change'));
        container.querySelector('[data-value-field="request_transition"]').value = 'level_2';
        container.querySelector('[data-action="save"]').click();

        expect(saved_rule.command).toEqual({ request_notify: 'level_clear', request_transition: 'level_2' });
    });

    test('calls on_delete when the delete button is clicked', () => {
        const container = document.createElement('div');
        const on_delete = vi.fn();
        render_rule_editor(container, sample_rule(), null, on_delete);
        container.querySelector('[data-action="delete"]').click();
        expect(on_delete).toHaveBeenCalledOnce();
    });

    test('offers the known flag/counter/inventory keys as autocomplete on the condition field', () => {
        const container = document.createElement('div');
        render_rule_editor(container, sample_rule(), null, null, {
            flags: { is_beat: false }, counters: { score: 0 }, inventory: { key_item: 0 },
        });
        const options = Array.from(container.querySelectorAll('datalist option')).map(o => o.value);
        expect(options).toContain('flags.is_beat');
        expect(options).toContain('counters.score');
        expect(options).toContain('inventory.key_item');
    });

    test('a broken condition shows a message and does not call on_save', () => {
        const container = document.createElement('div');
        let saved = false;
        render_rule_editor(container, sample_rule(), () => { saved = true; });
        container.querySelector('[data-field="condition"]').value = '(flags.is_beat';
        container.querySelector('[data-action="save"]').click();
        expect(saved).toBe(false);
        expect(container.querySelector('[data-condition-error]').textContent).toContain('Unbalanced');
    });

    test('fixing a broken condition and saving again works normally', () => {
        const container = document.createElement('div');
        let saved_rule = null;
        render_rule_editor(container, sample_rule(), (rule) => { saved_rule = rule; });
        container.querySelector('[data-field="condition"]').value = '(flags.is_beat';
        container.querySelector('[data-action="save"]').click();
        container.querySelector('[data-field="condition"]').value = 'flags.is_beat';
        container.querySelector('[data-action="save"]').click();
        expect(saved_rule.condition).toBe('flags.is_beat');
    });
});
