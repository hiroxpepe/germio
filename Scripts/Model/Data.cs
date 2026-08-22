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

        /// <summary>
        /// Whose rule this is. Empty (default) means the world's own rule, which
        /// fires whoever calls. A name here means the rule belongs to that one
        /// character alone. See modio's own docs/modio_spec.md.
        /// </summary>
        public string actor { get; set; } = string.Empty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>Gives back a copy holding nothing in common with this one.</summary>
        public Rule DeepCopy() {
            return new Rule {
                id = this.id,
                trigger = this.trigger,
                condition = this.condition,
                command = this.command != null ? this.command.DeepCopy() : new Command(),
                once = this.once,
                actor = this.actor
            };
        }
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

        /// <summary>
        /// Moves one or more Needs in animo. A list, always, even holding one:
        /// a single arrival may quiet more than one want. Fires an event out of
        /// the Store, once for each entry; nothing in germio hears it.
        /// See modio's own docs/modio_spec.md §7.2.
        /// </summary>
        public List<UpdateNeed>? update_need { get; set; }

        /// <summary>
        /// Starts a deed: work that takes time, and may fail part way. Fires an
        /// event out of the Store; nothing in germio hears it, and modio is what
        /// carries the deed out. See modio own docs/modio_spec.md §7.3.
        /// </summary>
        public RequestDeed? request_deed { get; set; }

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
        /// Gives back a copy holding nothing in common with this one, reaching
        /// every part within — including a request_deed and the Command it holds.
        /// </summary>
        public Command DeepCopy() {
            return new Command {
                set_flag = this.set_flag == null ? null
                    : new SetFlag { key = this.set_flag.key, value = this.set_flag.value },
                update_counter = this.update_counter == null ? null
                    : new UpdateCounter { key = this.update_counter.key, delta = this.update_counter.delta, op = this.update_counter.op },
                update_inventory = this.update_inventory == null ? null
                    : new UpdateInventory { key = this.update_inventory.key, delta = this.update_inventory.delta },
                update_need = this.update_need == null ? null
                    : copyNeeds(from: this.update_need),
                request_transition = this.request_transition,
                request_notify = this.request_notify,
                request_deed = this.request_deed?.DeepCopy(),
                set_persistence = this.set_persistence == null ? null
                    : new SetPersistence { key = this.set_persistence.key, value = this.set_persistence.value },
                record_event = this.record_event == null ? null
                    : new RecordEvent { kind = this.record_event.kind, target_id = this.record_event.target_id },
                reset_flags = this.reset_flags,
                reset_counters = this.reset_counters,
                reset_inventory = this.reset_inventory
            };
        }

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

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        static List<UpdateNeed> copyNeeds(List<UpdateNeed> from) {
            var made = new List<UpdateNeed>(capacity: from.Count);
            foreach (var need in from) {
                made.Add(item: new UpdateNeed { key = need.key, delta = need.delta });
            }
            return made;
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

    /// <summary>
    /// Moves a named Need in animo. The one way anything reaches that engine:
    /// see modio's own docs/modio_spec.md §7.2. Always written as a list, even
    /// holding one, since a single arrival may quiet more than one want.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class UpdateNeed {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>Which Need, by the name animo holds it under.</summary>
        public string key { get; set; } = string.Empty;

        /// <summary>How far it moves. Below zero to quiet a want.</summary>
        public float delta { get; set; }
    }

    /// <summary>
    /// What a deed looks for: a kind of thing, within a reach and a spread.
    /// Left out where a deed seeks nothing at all (standing still to rest, or
    /// calling out). See modio's own docs/modio_spec.md §7.4.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Target {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>One of germio's own type marks: Ground, Block, Human, Item, Home, and the rest.</summary>
        public string kind { get; set; } = string.Empty;

        /// <summary>How far out to look.</summary>
        public float reach { get; set; }

        /// <summary>How far round to look, in degrees.</summary>
        public float spread { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>Gives back a copy holding nothing in common with this one.</summary>
        public Target DeepCopy() {
            return new Target { kind = this.kind, reach = this.reach, spread = this.spread };
        }
    }

    /// <summary>
    /// When a deed is done. Exactly one of these holds a value; the rest stay
    /// empty. See modio's own docs/modio_spec.md §7.6.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class Until {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>Done once within this far of the target.</summary>
        public float? near { get; set; }

        /// <summary>Done once the bodies touch. Holds $target.</summary>
        public string? meets { get; set; }

        /// <summary>Done once this many seconds have gone by.</summary>
        public float? elapsed { get; set; }

        /// <summary>
        /// Never done of itself: held while the named state holds. A deed ending
        /// this way ends Failed, never Done.
        /// </summary>
        public string? @while { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>Gives back a copy holding nothing in common with this one.</summary>
        public Until DeepCopy() {
            return new Until {
                near = this.near, meets = this.meets,
                elapsed = this.elapsed, @while = this.@while
            };
        }
    }

    /// <summary>
    /// Work that takes time, and may fail part way. Sits beside
    /// request_transition and request_notify, all three asking for what does not
    /// finish on the spot. germio starts it and hears no more: modio carries it
    /// out. See modio's own docs/modio_spec.md §7.3.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [UnityEngine.Scripting.Preserve]
    public class RequestDeed {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>What to seek. Left out where a deed seeks nothing.</summary>
        public Target? target { get; set; }

        /// <summary>
        /// Which of the found ones to take. Read by the same Evaluator as any
        /// other condition, once $target is put in place. Empty takes the nearest.
        /// </summary>
        public string condition { get; set; } = string.Empty;

        /// <summary>How the body moves: one of germio's own seven doing-states.</summary>
        public string motion { get; set; } = string.Empty;

        /// <summary>
        /// What is done once there, on its own clock: hand_over, take_up,
        /// put_down, show, tend. Most deeds need none.
        /// </summary>
        public string act { get; set; } = string.Empty;

        /// <summary>When the deed is done.</summary>
        public Until? until { get; set; }

        /// <summary>
        /// What to do once it lands. A whole Command, held inside, so every
        /// command there is works here with nothing new added.
        /// </summary>
        public Command command { get; set; } = new Command();

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Gives back a copy holding nothing in common with this one, all the way
        /// down to the Command held within. Putting a found id in place works on
        /// a copy, and must never reach the rule that copy came from.
        /// </summary>
        public RequestDeed DeepCopy() {
            return new RequestDeed {
                target = this.target?.DeepCopy(),
                condition = this.condition,
                motion = this.motion,
                act = this.act,
                until = this.until?.DeepCopy(),
                command = this.command != null ? this.command.DeepCopy() : new Command()
            };
        }
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