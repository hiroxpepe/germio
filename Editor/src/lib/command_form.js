// Converts between germio's own Command shape (only the fields a rule
// actually uses are present at all) and a flat "form state" shape a
// checklist-style form can bind to directly (every kind always
// present, each with its own enabled flag). This lets more than one
// command kind be turned on for the same Rule at once — the real
// Command shape already allows this; only the earlier single-picker
// form could not express it.

function blank_flag() { return { enabled: false }; }

export function blank_form_state() {
    return {
        set_flag: { enabled: false, key: '', value: false },
        update_counter: { enabled: false, key: '', delta: 0, op: 'Add' },
        update_inventory: { enabled: false, key: '', delta: 0 },
        request_transition: { enabled: false, value: '' },
        request_notify: { enabled: false, value: '' },
        set_persistence: { enabled: false, key: '', value: '' },
        record_event: { enabled: false, kind: '', target_id: '' },
        reset_flags: blank_flag(),
        reset_counters: blank_flag(),
        reset_inventory: blank_flag(),
    };
}

export function command_to_form_state(command) {
    const state = blank_form_state();
    if (command.set_flag) {
        state.set_flag = { enabled: true, key: command.set_flag.key, value: command.set_flag.value };
    }
    if (command.update_counter) {
        state.update_counter = { enabled: true, ...command.update_counter };
    }
    if (command.update_inventory) {
        state.update_inventory = { enabled: true, ...command.update_inventory };
    }
    if (command.request_transition !== undefined && command.request_transition !== null) {
        state.request_transition = { enabled: true, value: command.request_transition };
    }
    if (command.request_notify !== undefined && command.request_notify !== null) {
        state.request_notify = { enabled: true, value: command.request_notify };
    }
    if (command.set_persistence) {
        state.set_persistence = { enabled: true, ...command.set_persistence };
    }
    if (command.record_event) {
        state.record_event = { enabled: true, ...command.record_event };
    }
    if (command.reset_flags) state.reset_flags = { enabled: true };
    if (command.reset_counters) state.reset_counters = { enabled: true };
    if (command.reset_inventory) state.reset_inventory = { enabled: true };
    return state;
}

export function form_state_to_command(state) {
    const command = {};
    if (state.set_flag.enabled) {
        command.set_flag = { key: state.set_flag.key, value: state.set_flag.value };
    }
    if (state.update_counter.enabled) {
        command.update_counter = {
            key: state.update_counter.key, delta: state.update_counter.delta, op: state.update_counter.op,
        };
    }
    if (state.update_inventory.enabled) {
        command.update_inventory = { key: state.update_inventory.key, delta: state.update_inventory.delta };
    }
    if (state.request_transition.enabled) {
        command.request_transition = state.request_transition.value;
    }
    if (state.request_notify.enabled) {
        command.request_notify = state.request_notify.value;
    }
    if (state.set_persistence.enabled) {
        command.set_persistence = { key: state.set_persistence.key, value: state.set_persistence.value };
    }
    if (state.record_event.enabled) {
        command.record_event = { kind: state.record_event.kind, target_id: state.record_event.target_id };
    }
    if (state.reset_flags.enabled) command.reset_flags = true;
    if (state.reset_counters.enabled) command.reset_counters = true;
    if (state.reset_inventory.enabled) command.reset_inventory = true;
    return command;
}
