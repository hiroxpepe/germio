// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// Executes a DataAction against the game state via a Store.
    /// Stateless: all state mutation happens through the store.
    /// Calls store.MarkDirty() once per Execute call only when at least one mutation occurred.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Executor {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Executes the given action, mutating state through the provided store.
        /// No-op if all action fields are null.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="store">The Store that owns the mutable DataState.</param>
        public static void Execute(DataAction action, Store store) {
            bool mutated = false;

            if (action.setFlag != null) {
                store.state.flags[action.setFlag.key] = action.setFlag.value;
                mutated = true;
            }

            if (action.updateCounter != null) {
                var uc = action.updateCounter;
                float current = store.state.counters.TryGetValue(uc.key, out float v) ? v : 0f;
                store.state.counters[uc.key] = uc.op switch {
                    CounterOp.Sub => current - uc.delta,
                    CounterOp.Set => uc.delta,
                    _             => current + uc.delta  // CounterOp.Add (default)
                };
                mutated = true;
            }

            if (action.updateInventory != null) {
                var ui = action.updateInventory;
                int current = store.state.inventory.TryGetValue(ui.id, out int v) ? v : 0;
                int next    = current + ui.delta;
                if (next <= 0) {
                    store.state.inventory.Remove(ui.id);
                } else {
                    store.state.inventory[ui.id] = next;
                }
                mutated = true;
            }

            if (action.requestTransition != null) {
                store.RequestTransition(target_id: action.requestTransition);
                mutated = true;
            }

            if (mutated) { store.MarkDirty(); }
        }
    }
}
