// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// Executes a DataAction against the game state via a StateManager.
    /// Stateless: all state mutation happens through the manager.
    /// Calls manager.MarkDirty() once per Execute call only when at least one mutation occurred.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class ActionExecutor {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Executes the given action, mutating state through the provided manager.
        /// No-op if all action fields are null.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="manager">The StateManager that owns the mutable DataState.</param>
        public static void Execute(DataAction action, StateManager manager) {
            bool mutated = false;

            if (action.setFlag != null) {
                manager.state.flags[action.setFlag.key] = action.setFlag.value;
                mutated = true;
            }

            if (action.updateCounter != null) {
                var uc = action.updateCounter;
                float current = manager.state.counters.TryGetValue(uc.key, out float v) ? v : 0f;
                manager.state.counters[uc.key] = uc.op switch {
                    CounterOp.Sub => current - uc.delta,
                    CounterOp.Set => uc.delta,
                    _             => current + uc.delta  // CounterOp.Add (default)
                };
                mutated = true;
            }

            if (action.updateInventory != null) {
                var ui = action.updateInventory;
                int current = manager.state.inventory.TryGetValue(ui.id, out int v) ? v : 0;
                int next    = current + ui.delta;
                if (next <= 0) {
                    manager.state.inventory.Remove(ui.id);
                } else {
                    manager.state.inventory[ui.id] = next;
                }
                mutated = true;
            }

            if (action.requestTransition != null) {
                manager.RequestTransition(action.requestTransition);
                mutated = true;
            }

            if (mutated) { manager.MarkDirty(); }
        }
    }
}
