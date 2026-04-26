
// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Collections.Generic;

// When compiled outside Unity (e.g. dotnet test), provide a no-op stub so [Preserve]
// attributes remain valid without requiring UnityEngine.dll.
#if !UNITY_5_3_OR_NEWER
namespace UnityEngine.Scripting {
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false)]
    internal class PreserveAttribute : System.Attribute {}
}
#endif

namespace Germio {
    /// <summary>
    /// Operation type for updating a numeric counter.
    /// Serialized as lowercase string in JSON ("add", "sub", "set").
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public enum CounterOp { Add, Sub, Set }

    /// <summary>
    /// Root data class for Germio runtime data.
    /// Contains the overall game state and all abstract worlds.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class DataRoot {
#nullable enable
        /// <summary>The current runtime state (flags, counters, inventory).</summary>
        public DataState state { get; set; } = new DataState();

        /// <summary>List of all abstract worlds in the game.</summary>
        public List<DataWorld> worlds { get; set; } = new List<DataWorld>();
    }

    /// <summary>
    /// Represents the player's runtime state.
    /// All quantifiable state values are expressed as named counters — no hardcoded numeric fields.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class DataState {
#nullable enable
        /// <summary>Boolean state flags. (e.g., flags["zone_a_cleared"] = true)</summary>
        public Dictionary<string, bool> flags { get; set; } = new Dictionary<string, bool>();

        /// <summary>
        /// Generic numeric counters for any quantifiable state.
        /// (e.g., counters["score"] = 100f, counters["elapsed_time"] = 0f, counters["depth"] = 3f)
        /// </summary>
        public Dictionary<string, float> counters { get; set; } = new Dictionary<string, float>();

        /// <summary>Item inventory with quantity. (e.g., inventory["key_01"] = 1)</summary>
        public Dictionary<string, int> inventory { get; set; } = new Dictionary<string, int>();

        /// <summary>ID of the currently active scene/level.</summary>
        public string currentScene { get; set; } = string.Empty;

        /// <summary>
        /// Identifies the currently active decision-making agent in a sequential state machine.
        /// (e.g., player_a / player_b in a two-agent sequence; empty if not applicable)
        /// </summary>
        public string currentTeam { get; set; } = string.Empty;

        /// <summary>
        /// G2: IDs of events that have already fired with once=true.
        /// Persisted with save data so one-shot events survive save/load cycles.
        /// </summary>
        public HashSet<string> firedEvents { get; set; } = new HashSet<string>();
    }

    /// <summary>
    /// Represents a world grouping multiple abstract levels.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class DataWorld {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string scene { get; set; } = string.Empty;
        public List<DataLevel> levels { get; set; } = new List<DataLevel>();
    }

    /// <summary>
    /// Represents a level (or node) within a world.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class DataLevel {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string scene { get; set; } = string.Empty;
        public List<DataNext> next { get; set; } = new List<DataNext>();
        public List<DataEvent> events { get; set; } = new List<DataEvent>();
    }

    /// <summary>
    /// Represents a conditional transition to another level.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class DataNext {
        public string id { get; set; } = string.Empty;
        public string condition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an event triggered within a level based on specific conditions.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class DataEvent {
#nullable enable
        /// <summary>Unique event identifier.</summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Abstract trigger ID. Matches VolumeTrigger.triggerId or a signal ID
        /// dispatched via TriggerHub.OnSignalReceived.
        /// </summary>
        public string trigger { get; set; } = string.Empty;

        /// <summary>
        /// Optional condition evaluated before executing the action.
        /// Uses the same syntax as DataNext.condition.
        /// Empty string = unconditional (action always executes when trigger fires).
        /// Example: "counters.score >= 100" or "flags.zone_a_cleared"
        /// </summary>
        public string condition { get; set; } = string.Empty;

        /// <summary>Action to execute when this event fires.</summary>
        public DataAction action { get; set; } = new DataAction();

        /// <summary>
        /// If true (default), this event fires at most once per session.
        /// Store blocks subsequent dispatches for the same event ID.
        /// Set to false only for repeatable effects (e.g., ambient counters).
        /// </summary>
        public bool once { get; set; } = true;
    }

    /// <summary>
    /// Represents a state mutation to be executed when an event fires.
    /// Exactly one action field should be non-null per instance.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class DataAction {
#nullable enable
        /// <summary>Sets a boolean flag in DataState.flags.</summary>
        public DataSetFlag? setFlag { get; set; }

        /// <summary>Adds, subtracts, or assigns a value to DataState.counters.</summary>
        public DataUpdateCounter? updateCounter { get; set; }

        /// <summary>Adds or removes items from DataState.inventory.</summary>
        public DataUpdateInventory? updateInventory { get; set; }

        /// <summary>Requests an immediate scene transition to the specified level ID.</summary>
        public string? requestTransition { get; set; }
    }

    /// <summary>Sets a named flag to a boolean value.</summary>
    [UnityEngine.Scripting.Preserve]
    public class DataSetFlag {
        public string key { get; set; } = string.Empty;
        public bool value { get; set; }
    }

    /// <summary>Updates a named counter by delta using the specified operation.</summary>
    [UnityEngine.Scripting.Preserve]
    public class DataUpdateCounter {
        public string key { get; set; } = string.Empty;
        public float delta { get; set; }
        /// <summary>Operation: Add (default), Sub, or Set.</summary>
        public CounterOp op { get; set; } = CounterOp.Add;
    }

    /// <summary>Changes the quantity of a named inventory item.</summary>
    [UnityEngine.Scripting.Preserve]
    public class DataUpdateInventory {
        public string id { get; set; } = string.Empty;
        public int delta { get; set; }
    }
}
