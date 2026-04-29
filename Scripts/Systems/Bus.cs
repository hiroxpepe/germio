// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Collections.Generic;

using Germio.Core;

namespace Germio.Systems {
    /// <summary>
    /// Central dispatch bus for all game triggers.
    /// Receives zone enter/exit events and arbitrary signals,
    /// then forwards them to the Store for rule evaluation.
    ///
    /// G2 Layer-1: maintains <see cref="_active_zones"/> (HashSet) to
    /// suppress duplicate OnZoneEnter calls for the same zone ID until OnZoneExit clears it.
    /// Signals (<see cref="Publish"/>) are never de-duplicated — they always dispatch.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Bus {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        readonly Store _store;

        /// <summary>
        /// G2 Layer-1 guard: tracks zone IDs currently inside an active volume area.
        /// Prevents the same area from firing its events every frame.
        /// </summary>
        readonly HashSet<string> _active_zones = new HashSet<string>();

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        /// <summary>
        /// Constructs a Bus connected to the given Store.
        /// </summary>
        /// <param name="store">The Store to dispatch all triggers to.</param>
        public Bus(Store store) {
            _store = store;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Called when a game entity enters an area volume with the specified zone ID.
        /// G2 Layer-1: if the same zone ID is already active, the call is silently ignored.
        /// </summary>
        /// <param name="zone_id">The zone identifier assigned to the volume, e.g. "vol_goal".</param>
        public void OnZoneEnter(string zone_id) {
            // G2 Layer-1: HashSet.Add returns false if already present → suppress duplicate
            if (!_active_zones.Add(zone_id)) { return; }
            _store.Dispatch(trigger_id: zone_id);
        }

        /// <summary>
        /// Called when a game entity exits an area volume.
        /// Clears the G2 guard so the area can fire again on re-entry.
        /// </summary>
        /// <param name="zone_id">The zone identifier of the exited volume.</param>
        public void OnZoneExit(string zone_id) {
            _active_zones.Remove(zone_id);
        }

        /// <summary>
        /// Publishes an instantaneous signal (not bound to an area).
        /// Signals are never de-duplicated and always dispatch to the Store.
        /// Use for events like player death ("sig_despawn") that can occur repeatedly.
        /// </summary>
        /// <param name="signal_id">The signal identifier, e.g. "sig_despawn".</param>
        public void Publish(string signal_id) {
            _store.Dispatch(trigger_id: signal_id);
        }

        /// <summary>
        /// Clears the active zones guard set.
        /// Call this on scene transitions to prevent stale zone IDs from suppressing
        /// re-entry events in the new scene (P5-T3).
        /// </summary>
        public void ClearActiveZones() {
            _active_zones.Clear();
        }
    }
}
