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

    ///////////////////////////////////////////////////////////////////////
    // Enums

    /// <summary>
    /// Operation type for updating a numeric counter.
    /// Serialized as lowercase string in JSON ("add", "sub", "set").
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum CounterOp { Add, Sub, Set }

    ///////////////////////////////////////////////////////////////////////
    // Static-side classes (LLM-edited, persisted in germio.json)

    /// <summary>
    /// Root data class for Germio static configuration.
    /// Contains the scenario tree structure and initial state.
    /// Loaded from germio.json (development) or germio.dat (production, AES-encrypted).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Scenario {
#nullable enable
        /// <summary>JSON schema version. Stays at 1 (schema not yet published).</summary>
        public int schema_version { get; set; } = 1;

        /// <summary>The initial state at game start.</summary>
        public State initial_state { get; set; } = new State();

        /// <summary>The root node of the scenario tree.</summary>
        public Node root { get; set; } = new Node();
    }

    /// <summary>
    /// A node in the scenario graph. Represents a Unity scene or a logical grouping.
    /// Recursive structure: a node may contain child nodes.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Node {
#nullable enable
        /// <summary>Unique identifier within the entire Scenario.</summary>
        public string id { get; set; } = string.Empty;

        /// <summary>Human-readable name (display purposes).</summary>
        public string name { get; set; } = string.Empty;

        /// <summary>
        /// Free-form kind label. Conventional values: "world", "region", "title",
        /// "select", "setting", "level", "map", "shop", "boss", "bonus", "ending".
        /// Custom values are allowed.
        /// </summary>
        public string kind { get; set; } = string.Empty;

        /// <summary>
        /// Unity Scene name (used by SceneManager.LoadScene).
        /// Empty for internal nodes that don't correspond to a Unity Scene.
        /// </summary>
        public string scene { get; set; } = string.Empty;

        /// <summary>Child nodes. Empty list = leaf node.</summary>
        public List<Node> children { get; set; } = new List<Node>();

        /// <summary>Conditional transitions to other nodes.</summary>
        public List<Next> next { get; set; } = new List<Next>();

        /// <summary>Rules triggered within this node.</summary>
        public List<Rule> rules { get; set; } = new List<Rule>();
    }

    /// <summary>
    /// Represents a conditional transition to another node.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Next {
#nullable enable
        /// <summary>Target node id.</summary>
        public string id { get; set; } = string.Empty;

        /// <summary>DSL expression for the transition condition.</summary>
        public string condition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a rule triggered within a node based on specific conditions.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Rule {
#nullable enable
        /// <summary>Unique rule identifier.</summary>
        public string id { get; set; } = string.Empty;

        /// <summary>Abstract trigger ID. Matches Zone.zone_id or Bus signal.</summary>
        public string trigger { get; set; } = string.Empty;

        /// <summary>DSL expression evaluated before executing the command.</summary>
        public string condition { get; set; } = string.Empty;

        /// <summary>Command to execute when this rule fires.</summary>
        public Command command { get; set; } = new Command();

        /// <summary>If true (default), this rule fires at most once per session.</summary>
        public bool once { get; set; } = true;
    }

    /// <summary>
    /// Represents a state mutation to be executed when a rule fires.
    /// Exactly one command field should be non-null per instance.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Command {
#nullable enable
        /// <summary>Sets a boolean flag in State.flags.</summary>
        public SetFlag? set_flag { get; set; }

        /// <summary>Adds, subtracts, or assigns a value to State.counters.</summary>
        public UpdateCounter? update_counter { get; set; }

        /// <summary>Adds or removes items from State.inventory.</summary>
        public UpdateInventory? update_inventory { get; set; }

        /// <summary>Requests an immediate scene transition to the specified node ID.</summary>
        public string? request_transition { get; set; }

        /// <summary>Sets an arbitrary key-value entry in State.persistence.</summary>
        public SetPersistence? set_persistence { get; set; }

        /// <summary>Records a custom event into the History.</summary>
        public RecordEvent? record_event { get; set; }
    }

    /// <summary>Sets a named flag to a boolean value.</summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class SetFlag {
#nullable enable
        public string key { get; set; } = string.Empty;
        public bool value { get; set; }
    }

    /// <summary>Updates a named counter by delta using the specified operation.</summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class UpdateCounter {
#nullable enable
        public string key { get; set; } = string.Empty;
        public float delta { get; set; }
        public CounterOp op { get; set; } = CounterOp.Add;
    }

    /// <summary>Changes the quantity of a named inventory item.</summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class UpdateInventory {
#nullable enable
        public string key { get; set; } = string.Empty;
        public int delta { get; set; }
    }

    /// <summary>Sets an arbitrary persistence value under the given key.</summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class SetPersistence {
#nullable enable
        public string key { get; set; } = string.Empty;
        public string value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Records a custom event into the History.
    /// Used by Rule.command to log gameplay events for later DSL queries.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class RecordEvent {
#nullable enable
        /// <summary>Event kind (e.g., "node_clear", "node_fail", custom).</summary>
        public string kind { get; set; } = string.Empty;

        /// <summary>Target identifier (e.g., node id).</summary>
        public string target_id { get; set; } = string.Empty;
    }

    ///////////////////////////////////////////////////////////////////////
    // Dynamic-side classes (runtime-managed, persisted in snapshot_*.json)

    /// <summary>
    /// Root data class for Germio runtime snapshot.
    /// Contains the current dynamic state and event history.
    /// Loaded from snapshot_N.json (development) or snapshot_N.dat (production, AES-encrypted).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Snapshot {
#nullable enable
        /// <summary>JSON schema version.</summary>
        public int schema_version { get; set; } = 1;

        /// <summary>The current dynamic state.</summary>
        public State state { get; set; } = new State();

        /// <summary>The event history.</summary>
        public History history { get; set; } = new History();
    }

    /// <summary>
    /// Represents the player's runtime state.
    /// All quantifiable state values are expressed as named counters.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class State {
#nullable enable
        /// <summary>Boolean state flags.</summary>
        public Dictionary<string, bool> flags { get; set; } = new Dictionary<string, bool>();

        /// <summary>Generic numeric counters for any quantifiable state.</summary>
        public Dictionary<string, float> counters { get; set; } = new Dictionary<string, float>();

        /// <summary>Item inventory with quantity.</summary>
        public Dictionary<string, int> inventory { get; set; } = new Dictionary<string, int>();

        /// <summary>ID of the currently active node in the Scenario tree.</summary>
        public string current_node { get; set; } = string.Empty;

        /// <summary>Identifies the currently active decision-making agent.</summary>
        public string current_team { get; set; } = string.Empty;

        /// <summary>Arbitrary key-value persistence store. Survives transitions and save/load.</summary>
        public Dictionary<string, string> persistence { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Event log for the gameplay session. Used for history-dependent rules and DSL queries.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class History {
#nullable enable
        /// <summary>Chronologically ordered event log.</summary>
        public List<HistoryEntry> entries { get; set; } = new List<HistoryEntry>();

        /// <summary>Maximum number of entries retained. Default 1000.</summary>
        public int max_entries { get; set; } = 1000;
    }

    /// <summary>
    /// A single event in the History log.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class HistoryEntry {
#nullable enable
        /// <summary>
        /// Event kind. Standard values:
        ///   "node_enter", "node_exit", "rule_fire"
        /// Custom values via Command.RecordEvent are allowed.
        /// </summary>
        public string kind { get; set; } = string.Empty;

        /// <summary>Target identifier (node id, rule id, custom).</summary>
        public string target_id { get; set; } = string.Empty;

        /// <summary>In-game elapsed time in seconds since session start.</summary>
        public float timestamp { get; set; }
    }
}
