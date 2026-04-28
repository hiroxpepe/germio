
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

namespace Germio.Model {
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
    public class Scenario {
#nullable enable
        /// <summary>The current runtime state (flags, counters, inventory).</summary>
        public State state { get; set; } = new State();

        /// <summary>List of all abstract worlds in the game.</summary>
        public List<World> worlds { get; set; } = new List<World>();
    }

    /// <summary>
    /// Represents the player's runtime state.
    /// All quantifiable state values are expressed as named counters — no hardcoded numeric fields.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class State {
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
        public string current_scene { get; set; } = string.Empty;

        /// <summary>
        /// Identifies the currently active decision-making agent in a sequential state machine.
        /// (e.g., player_a / player_b in a two-agent sequence; empty if not applicable)
        /// </summary>
        public string current_team { get; set; } = string.Empty;

        /// <summary>
        /// G2: IDs of rules that have already fired with once=true.
        /// Persisted with save data so one-shot rules survive save/load cycles.
        /// </summary>
        public HashSet<string> fired_rules { get; set; } = new HashSet<string>();
    }

    /// <summary>
    /// Represents a world grouping multiple abstract levels.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class World {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string scene { get; set; } = string.Empty;
        public List<Level> levels { get; set; } = new List<Level>();
    }

    /// <summary>
    /// Represents a level (or node) within a world.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class Level {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string scene { get; set; } = string.Empty;
        public List<Next> next { get; set; } = new List<Next>();
        public List<Rule> rules { get; set; } = new List<Rule>();
    }

    /// <summary>
    /// Represents a conditional transition to another level.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class Next {
        public string id { get; set; } = string.Empty;
        public string condition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a rule triggered within a level based on specific conditions.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class Rule {
#nullable enable
        /// <summary>Unique rule identifier.</summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Abstract trigger ID. Matches Zone.zone_id or a signal ID
        /// dispatched via Bus.Publish.
        /// </summary>
        public string trigger { get; set; } = string.Empty;

        /// <summary>
        /// Optional condition evaluated before executing the command.
        /// Uses the same syntax as Next.condition.
        /// Empty string = unconditional (command always executes when trigger fires).
        /// Example: "counters.score >= 100" or "flags.zone_a_cleared"
        /// </summary>
        public string condition { get; set; } = string.Empty;

        /// <summary>Command to execute when this rule fires.</summary>
        public Command command { get; set; } = new Command();

        /// <summary>
        /// If true (default), this rule fires at most once per session.
        /// Store blocks subsequent dispatches for the same rule ID.
        /// Set to false only for repeatable effects (e.g., ambient counters).
        /// </summary>
        public bool once { get; set; } = true;
    }

    /// <summary>
    /// Represents a state mutation to be executed when a rule fires.
    /// Exactly one command field should be non-null per instance.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class Command {
#nullable enable
        /// <summary>Sets a boolean flag in State.flags.</summary>
        public SetFlag? set_flag { get; set; }

        /// <summary>Adds, subtracts, or assigns a value to State.counters.</summary>
        public UpdateCounter? update_counter { get; set; }

        /// <summary>Adds or removes items from State.inventory.</summary>
        public UpdateInventory? update_inventory { get; set; }

        /// <summary>Requests an immediate scene transition to the specified level ID.</summary>
        public string? request_transition { get; set; }
    }

    /// <summary>Sets a named flag to a boolean value.</summary>
    [UnityEngine.Scripting.Preserve]
    public class SetFlag {
        public string key { get; set; } = string.Empty;
        public bool value { get; set; }
    }

    /// <summary>Updates a named counter by delta using the specified operation.</summary>
    [UnityEngine.Scripting.Preserve]
    public class UpdateCounter {
        public string key { get; set; } = string.Empty;
        public float delta { get; set; }
        /// <summary>Operation: Add (default), Sub, or Set.</summary>
        public CounterOp op { get; set; } = CounterOp.Add;
    }

    /// <summary>Changes the quantity of a named inventory item.</summary>
    [UnityEngine.Scripting.Preserve]
    public class UpdateInventory {
        public string id { get; set; } = string.Empty;
        public int delta { get; set; }
    }
}
