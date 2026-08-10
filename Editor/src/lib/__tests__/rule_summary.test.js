import { describe, test, expect } from 'vitest';
import { format_rule_summary } from '../rule_summary.js';

describe('format_rule_summary', () => {
  test('shows trigger, the first set command, and a once badge when once is true', () => {
    const rule = {
      id: 'rule_a',
      trigger: 'vol_home',
      command: { request_notify: 'level_clear' },
      once: true,
    };
    expect(format_rule_summary(rule)).toBe('vol_home \u2192 request_notify: level_clear \u00b7 once');
  });

  test('shows "repeats" instead of "once" when once is false', () => {
    const rule = {
      id: 'rule_b',
      trigger: 'signal_btn_start_pressed',
      command: { request_transition: 'level_2' },
      once: false,
    };
    expect(format_rule_summary(rule)).toBe('signal_btn_start_pressed \u2192 request_transition: level_2 \u00b7 repeats');
  });

  test('joins more than one set command with a comma', () => {
    const rule = {
      id: 'rule_c',
      trigger: 'vol_home',
      command: { request_transition: 'level_2', request_notify: 'level_clear' },
      once: true,
    };
    expect(format_rule_summary(rule)).toBe(
      'vol_home \u2192 request_transition: level_2, request_notify: level_clear \u00b7 once'
    );
  });

  test('shows "(no command)" when the command has nothing set', () => {
    const rule = { id: 'rule_d', trigger: 'vol_home', command: {}, once: true };
    expect(format_rule_summary(rule)).toBe('vol_home \u2192 (no command) \u00b7 once');
  });
});
