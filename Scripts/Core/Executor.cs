// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Executes a Command against the game state via a Store.
    /// Stateless: all state mutation happens through the store.
    /// Calls store.MarkDirty() once per Execute call only when at least one mutation occurred.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Executor {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Executes the given command, mutating state through the provided store.
        /// No-op if all command fields are null.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="store">The Store that owns the mutable State.</param>
        public static void Execute(Command command, Store store) {
            bool mutated = false;

            if (command.set_flag != null) {
                store.scenario.initial_state.flags[command.set_flag.key] = command.set_flag.value;
                mutated = true;
            }

            if (command.update_counter != null) {
                var uc = command.update_counter;
                float current = store.scenario.initial_state.counters.TryGetValue(uc.key, out float v) ? v : 0f;
                store.scenario.initial_state.counters[uc.key] = uc.op switch {
                    CounterOp.Sub => current - uc.delta,
                    CounterOp.Set => uc.delta,
                    _             => current + uc.delta  // CounterOp.Add (default)
                };
                mutated = true;
            }

            if (command.update_inventory != null) {
                var ui = command.update_inventory;
                int current = store.scenario.initial_state.inventory.TryGetValue(ui.key, out int v) ? v : 0;
                int next    = current + ui.delta;
                if (next <= 0) {
                    store.scenario.initial_state.inventory.Remove(ui.key);
                } else {
                    store.scenario.initial_state.inventory[ui.key] = next;
                }
                mutated = true;
            }

            if (command.request_transition != null) {
                store.RequestTransition(target_id: command.request_transition);
                mutated = true;
            }

            if (command.set_persistence != null) {
                store.scenario.initial_state.persistence[command.set_persistence.key] = command.set_persistence.value;
                mutated = true;
            }

            if (command.record_event != null) {
                store.RecordHistoryEvent(
                    kind: command.record_event.kind,
                    target_id: command.record_event.target_id
                );
                mutated = true;
            }

            if (mutated) { store.MarkDirty(); }
        }
    }
}
