import { describe, test, expect } from 'vitest';
import { command_to_form_state, form_state_to_command, blank_form_state } from '../command_form.js';

describe('blank_form_state', () => {
  test('every command kind starts disabled', () => {
    const state = blank_form_state();
    expect(state.set_flag.enabled).toBe(false);
    expect(state.request_notify.enabled).toBe(false);
    expect(state.reset_flags.enabled).toBe(false);
  });
});

describe('command_to_form_state', () => {
  test('marks only the fields a Command actually has set as enabled', () => {
    const command = { request_transition: 'level_2' };
    const state = command_to_form_state(command);
    expect(state.request_transition.enabled).toBe(true);
    expect(state.request_transition.value).toBe('level_2');
    expect(state.set_flag.enabled).toBe(false);
  });

  test('reads a set_flag command into its key and value', () => {
    const command = { set_flag: { key: 'is_beat', value: true } };
    const state = command_to_form_state(command);
    expect(state.set_flag).toEqual({ enabled: true, key: 'is_beat', value: true });
  });

  test('reads an update_counter command into key, delta, and op', () => {
    const command = { update_counter: { key: 'score', delta: 10, op: 'Add' } };
    const state = command_to_form_state(command);
    expect(state.update_counter).toEqual({ enabled: true, key: 'score', delta: 10, op: 'Add' });
  });

  test('reads more than one field set on the same command at once', () => {
    const command = { request_transition: 'level_2', request_notify: 'level_clear' };
    const state = command_to_form_state(command);
    expect(state.request_transition.enabled).toBe(true);
    expect(state.request_notify.enabled).toBe(true);
  });

  test('reads the three reset_* bools', () => {
    const command = { reset_flags: true, reset_inventory: true };
    const state = command_to_form_state(command);
    expect(state.reset_flags.enabled).toBe(true);
    expect(state.reset_counters.enabled).toBe(false);
    expect(state.reset_inventory.enabled).toBe(true);
  });
});

describe('form_state_to_command', () => {
  test('writes out only the enabled kinds', () => {
    const state = blank_form_state();
    state.request_transition = { enabled: true, value: 'level_2' };
    const command = form_state_to_command(state);
    expect(command).toEqual({ request_transition: 'level_2' });
  });

  test('writes a set_flag command as a nested object, not a flat field', () => {
    const state = blank_form_state();
    state.set_flag = { enabled: true, key: 'is_beat', value: true };
    const command = form_state_to_command(state);
    expect(command).toEqual({ set_flag: { key: 'is_beat', value: true } });
  });

  test('writes more than one enabled kind at once', () => {
    const state = blank_form_state();
    state.request_transition = { enabled: true, value: 'level_2' };
    state.request_notify = { enabled: true, value: 'level_clear' };
    const command = form_state_to_command(state);
    expect(command).toEqual({ request_transition: 'level_2', request_notify: 'level_clear' });
  });

  test('writes reset_flags as a plain true, not an object', () => {
    const state = blank_form_state();
    state.reset_flags = { enabled: true };
    const command = form_state_to_command(state);
    expect(command).toEqual({ reset_flags: true });
  });

  test('round-trips: reading a command then writing it back gives the same command', () => {
    const original = {
      update_inventory: { key: 'key_item', delta: 1 },
      record_event: { kind: 'level_cleared', target_id: 'level_1' },
    };
    const round_tripped = form_state_to_command(command_to_form_state(original));
    expect(round_tripped).toEqual(original);
  });
});
