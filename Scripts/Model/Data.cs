// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System.Collections.Generic;

// When compiled outside Unity (e.g. dotnet test), provide a no-op stub so [Preserve]
// attributes remain valid without requiring UnityEngine.dll.
#if !UNITY_5_3_OR_NEWER
namespace UnityEngine.Scripting {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // Classes

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false)]
    internal class PreserveAttribute : System.Attribute {}
}
#endif

namespace Germio.Model {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Enums [noun]

    /// <summary>
    /// Operation type for updating a numeric counter.
    /// Serialized as lowercase string in JSON ("add", "sub", "set").
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum CounterOp { Add, Sub, Set }

    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // Static-side classes (LLM-edited, persisted in germio.json)

    /// <summary>
    /// Root data class for Germio static configuration.
    /// Contains the scenario tree structure and initial state.
    /// Loaded from germio.json (development) or germio.dat (production, AES-encrypted).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Scenario {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

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
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

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
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

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
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

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
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>Sets a boolean flag in State.flags.</summary>
        public SetFlag? set_flag { get; set; }

        /// <summary>Adds, subtracts, or assigns a value to State.counters.</summary>
        public UpdateCounter? update_counter { get; set; }

        /// <summary>Adds or removes items from State.inventory.</summary>
        public UpdateInventory? update_inventory { get; set; }

        /// <summary>Requests an immediate scene transition to the specified node ID.</summary>
        public string? request_transition { get; set; }

        /// <summary>
        /// Requests a notify. The value is a free-form id (e.g.
        /// "level_clear") whose meaning is decided entirely by the game,
        /// the same way <see cref="Rule.trigger"/> and
        /// <see cref="HistoryEntry.kind"/> already are. Changes no saved
        /// state — use <see cref="record_event"/> instead if the happening
        /// itself must also be kept in the History.
        /// </summary>
        public string? request_notify { get; set; }

        /// <summary>Sets an arbitrary key-value entry in State.persistence.</summary>
        public SetPersistence? set_persistence { get; set; }

        /// <summary>Records a custom event into the History.</summary>
        public RecordEvent? record_event { get; set; }

        /// <summary>
        /// Clears all entries from <c>State.flags</c>. (Phase 5.8 v2 fix6 extension)
        /// Typically combined with <c>trigger="_on_enter_node"</c> on a title/menu node
        /// to start a fresh session.
        /// </summary>
        public bool reset_flags { get; set; } = false;

        /// <summary>
        /// Clears all entries from <c>State.counters</c>. (Phase 5.8 v2 fix6 extension)
        /// </summary>
        public bool reset_counters { get; set; } = false;

        /// <summary>
        /// Clears all entries from <c>State.inventory</c>. (Phase 5.8 v2 fix6 extension)
        /// </summary>
        public bool reset_inventory { get; set; } = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Builds a full, human-readable line describing every field this
        /// Command actually has set, for logging. Only set fields are
        /// shown, so a log line stays short for the common case (one
        /// field set) while still showing everything when more than one
        /// field is combined on the same Rule.
        /// </summary>
        public string ToLogString() {
            var parts = new List<string>();
            if (set_flag != null) {
                parts.Add($"set_flag={{key='{set_flag.key}', value={set_flag.value}}}");
            }
            if (update_counter != null) {
                parts.Add($"update_counter={{key='{update_counter.key}', delta={update_counter.delta}, op={update_counter.op}}}");
            }
            if (update_inventory != null) {
                parts.Add($"update_inventory={{key='{update_inventory.key}', delta={update_inventory.delta}}}");
            }
            if (request_transition != null) {
                parts.Add($"request_transition='{request_transition}'");
            }
            if (request_notify != null) {
                parts.Add($"request_notify='{request_notify}'");
            }
            if (set_persistence != null) {
                parts.Add($"set_persistence={{key='{set_persistence.key}', value='{set_persistence.value}'}}");
            }
            if (record_event != null) {
                parts.Add($"record_event={{kind='{record_event.kind}', target_id='{record_event.target_id}'}}");
            }
            if (reset_flags)     { parts.Add("reset_flags=True"); }
            if (reset_counters)  { parts.Add("reset_counters=True"); }
            if (reset_inventory) { parts.Add("reset_inventory=True"); }
            return parts.Count == 0 ? "(none)" : string.Join(", ", parts);
        }
    }

    /// <summary>Sets a named flag to a boolean value.</summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class SetFlag {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        public string key { get; set; } = string.Empty;
        public bool value { get; set; }
    }

    /// <summary>Updates a named counter by delta using the specified operation.</summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class UpdateCounter {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        public string key { get; set; } = string.Empty;
        public float delta { get; set; }
        public CounterOp op { get; set; } = CounterOp.Add;
    }

    /// <summary>Changes the quantity of a named inventory item.</summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class UpdateInventory {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        public string key { get; set; } = string.Empty;
        public int delta { get; set; }
    }

    /// <summary>Sets an arbitrary persistence value under the given key.</summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class SetPersistence {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

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
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>Event kind (e.g., "node_clear", "node_fail", custom).</summary>
        public string kind { get; set; } = string.Empty;

        /// <summary>Target identifier (e.g., node id).</summary>
        public string target_id { get; set; } = string.Empty;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // Dynamic-side classes (runtime-managed, persisted in snapshot_*.json)

    /// <summary>
    /// Root data class for Germio runtime snapshot.
    /// Contains the current dynamic state and event history.
    /// Loaded from snapshot_N.json (development) or snapshot_N.dat (production, AES-encrypted).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Snapshot {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

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
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>Boolean state flags.</summary>
        public Map<string, bool> flags { get; set; } = new Map<string, bool>();

        /// <summary>Generic numeric counters for any quantifiable state.</summary>
        public Map<string, float> counters { get; set; } = new Map<string, float>();

        /// <summary>Item inventory with quantity.</summary>
        public Map<string, int> inventory { get; set; } = new Map<string, int>();

        /// <summary>ID of the currently active node in the Scenario tree.</summary>
        public string current_node { get; set; } = string.Empty;

        /// <summary>Identifies the currently active decision-making agent.</summary>
        public string current_team { get; set; } = string.Empty;

        /// <summary>Arbitrary key-value persistence store. Survives transitions and save/load.</summary>
        public Map<string, string> persistence { get; set; } = new Map<string, string>();
    }

    /// <summary>
    /// Event log for the gameplay session. Used for history-dependent rules and DSL queries.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class History {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

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
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

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