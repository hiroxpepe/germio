// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Collections.Generic;

namespace Germio {
    /// <summary>
    /// Central dispatch hub for all game triggers.
    /// Receives area enter/exit events and arbitrary signals,
    /// then forwards them to the StateManager for event evaluation.
    ///
    /// G2 Layer-1: maintains <see cref="_active_volume_triggers"/> (HashSet) to
    /// suppress duplicate OnAreaEnter calls for the same trigger ID until OnAreaExit clears it.
    /// Signals (<see cref="OnSignalReceived"/>) are never de-duplicated — they always dispatch.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class UniversalTriggerSystem {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        readonly StateManager _manager;

        /// <summary>
        /// G2 Layer-1 guard: tracks trigger IDs currently inside an active volume area.
        /// Prevents the same area from firing its events every frame.
        /// </summary>
        readonly HashSet<string> _active_volume_triggers = new HashSet<string>();

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        /// <summary>
        /// Constructs a UniversalTriggerSystem connected to the given StateManager.
        /// </summary>
        /// <param name="manager">The StateManager to dispatch all triggers to.</param>
        public UniversalTriggerSystem(StateManager manager) {
            _manager = manager;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Called when a game entity enters an area volume with the specified trigger ID.
        /// G2 Layer-1: if the same trigger ID is already active, the call is silently ignored.
        /// </summary>
        /// <param name="triggerId">The trigger identifier assigned to the volume, e.g. "vol_goal".</param>
        public void OnAreaEnter(string triggerId) {
            // G2 Layer-1: HashSet.Add returns false if already present → suppress duplicate
            if (!_active_volume_triggers.Add(triggerId)) { return; }
            _manager.DispatchTrigger(triggerId);
        }

        /// <summary>
        /// Called when a game entity exits an area volume.
        /// Clears the G2 guard so the area can fire again on re-entry.
        /// </summary>
        /// <param name="triggerId">The trigger identifier of the exited volume.</param>
        public void OnAreaExit(string triggerId) {
            _active_volume_triggers.Remove(triggerId);
        }

        /// <summary>
        /// Called to dispatch an instantaneous signal (not bound to an area).
        /// Signals are never de-duplicated and always dispatch to the StateManager.
        /// Use for events like player death ("sig_despawn") that can occur repeatedly.
        /// </summary>
        /// <param name="signalId">The signal identifier, e.g. "sig_despawn".</param>
        public void OnSignalReceived(string signalId) {
            _manager.DispatchTrigger(signalId);
        }
    }
}
