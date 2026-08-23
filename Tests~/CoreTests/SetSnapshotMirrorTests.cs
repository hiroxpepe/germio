// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;
using Germio;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for Store.SetSnapshot mirroring behavior (Phase 5.8 v2 fix6 hotfix9).
    /// 
    /// Codifies the "Unity Scene is the single source of truth for current_node" rule:
    ///   - current_node MUST NOT be mirrored from snapshot to scenario.initial_state
    ///   - current_team MUST NOT be mirrored either (Unity-context info)
    ///   - flags / counters / inventory / persistence MUST be mirrored
    ///     (so domain state survives scene reloads — Phase 5.8 v2 intent)
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class SetSnapshotMirrorTests {
#nullable enable

        // -----------------------------------------------------------------------------------------
        // Helpers

        /// <summary>
        /// Builds a minimal scenario with seeded initial_state values that we can
        /// inspect to determine whether SetSnapshot overwrote them or not.
        /// </summary>
        static Scenario BuildScenarioWithDefaults() {
            var scenario = new Scenario();
            scenario.initial_state = new State {
                current_node = "title",       // pretend Unity loaded the Title scene
                current_team = "team_red",    // pretend the application set this
                flags        = new Map<string, bool>  { { "preset_flag", false } },
                counters     = new Map<string, float> { { "preset_counter", 0f } },
                inventory    = new Map<string, int>   { { "preset_item", 0 } },
                persistence  = new Map<string, string>{ { "preset_p", "" } },
            };
            scenario.root = new Node {
                id = "main", name = "Main", kind = "world", scene = "",
                children = new List<Node> {
                    new Node { id = "title",   name = "Title",   kind = "title", scene = "Title" },
                    new Node { id = "level_1", name = "Level 1", kind = "level", scene = "Level 1" },
                }
            };
            return scenario;
        }

        /// <summary>
        /// Builds a snapshot loaded from disk that simulates a prior session's persisted state:
        /// current_node says level_1 (stale), and several flags/counters are set.
        /// </summary>
        static Snapshot BuildPersistedSnapshot() {
            return new Snapshot {
                state = new State {
                    current_node = "level_1",   // stale: prior session ended on Level 1
                    current_team = "team_blue", // prior session's team
                    flags        = new Map<string, bool>  {
                        { "player_at_home", true },
                        { "is_beat",        true },
                    },
                    counters     = new Map<string, float> { { "score", 1234f } },
                    inventory    = new Map<string, int>   { { "coin",  42   } },
                    persistence  = new Map<string, string>{ { "save_slot_label", "Adventure 1" } },
                }
            };
        }

        // -----------------------------------------------------------------------------------------
        // current_node — MUST NOT be mirrored (Unity Scene is truth)

        [Test, Description("SetSnapshot does NOT overwrite scenario.initial_state.current_node from snapshot (Unity Scene is truth)")]
        public void SetSnapshot_DoesNotMirrorCurrentNode() {
            Scenario scenario = BuildScenarioWithDefaults();
            Store store = new Store(scenario);
            Snapshot snap = BuildPersistedSnapshot();

            store.SetSnapshot(snap);

            Assert.That(scenario.initial_state.current_node, Is.EqualTo("title"),
                "current_node must remain at the value Unity placed there; snapshot value 'level_1' must be ignored");
        }

        [Test, Description("SetSnapshot does NOT overwrite scenario.initial_state.current_team")]
        public void SetSnapshot_DoesNotMirrorCurrentTeam() {
            Scenario scenario = BuildScenarioWithDefaults();
            Store store = new Store(scenario);
            Snapshot snap = BuildPersistedSnapshot();

            store.SetSnapshot(snap);

            Assert.That(scenario.initial_state.current_team, Is.EqualTo("team_red"),
                "current_team must remain at the application-set value; snapshot value 'team_blue' must be ignored");
        }

        // -----------------------------------------------------------------------------------------
        // flags / counters / inventory / persistence — MUST be mirrored

        [Test, Description("SetSnapshot mirrors snapshot.state.flags into scenario.initial_state.flags")]
        public void SetSnapshot_MirrorsFlags() {
            Scenario scenario = BuildScenarioWithDefaults();
            Store store = new Store(scenario);
            Snapshot snap = BuildPersistedSnapshot();

            store.SetSnapshot(snap);

            Assert.That(scenario.initial_state.flags, Does.ContainKey("player_at_home"));
            Assert.That(scenario.initial_state.flags["player_at_home"], Is.True);
            Assert.That(scenario.initial_state.flags["is_beat"], Is.True);
        }

        [Test, Description("SetSnapshot replaces (not merges) flags — preset values not in snapshot are dropped")]
        public void SetSnapshot_ReplacesFlagsRatherThanMerging() {
            Scenario scenario = BuildScenarioWithDefaults();
            Store store = new Store(scenario);
            Snapshot snap = BuildPersistedSnapshot();

            store.SetSnapshot(snap);

            Assert.That(scenario.initial_state.flags, Does.Not.ContainKey("preset_flag"),
                "Snapshot flags should overwrite the entire dictionary so stale preset keys disappear");
        }

        [Test, Description("SetSnapshot mirrors counters")]
        public void SetSnapshot_MirrorsCounters() {
            Scenario scenario = BuildScenarioWithDefaults();
            Store store = new Store(scenario);
            Snapshot snap = BuildPersistedSnapshot();

            store.SetSnapshot(snap);

            Assert.That(scenario.initial_state.counters, Does.ContainKey("score"));
            Assert.That(scenario.initial_state.counters["score"], Is.EqualTo(1234f));
        }

        [Test, Description("SetSnapshot mirrors inventory")]
        public void SetSnapshot_MirrorsInventory() {
            Scenario scenario = BuildScenarioWithDefaults();
            Store store = new Store(scenario);
            Snapshot snap = BuildPersistedSnapshot();

            store.SetSnapshot(snap);

            Assert.That(scenario.initial_state.inventory, Does.ContainKey("coin"));
            Assert.That(scenario.initial_state.inventory["coin"], Is.EqualTo(42));
        }

        [Test, Description("SetSnapshot mirrors persistence")]
        public void SetSnapshot_MirrorsPersistence() {
            Scenario scenario = BuildScenarioWithDefaults();
            Store store = new Store(scenario);
            Snapshot snap = BuildPersistedSnapshot();

            store.SetSnapshot(snap);

            Assert.That(scenario.initial_state.persistence, Does.ContainKey("save_slot_label"));
            Assert.That(scenario.initial_state.persistence["save_slot_label"], Is.EqualTo("Adventure 1"));
        }

        // -----------------------------------------------------------------------------------------
        // Edge cases

        [Test, Description("SetSnapshot tolerates a snapshot whose state is null (no mirror happens, no exception)")]
        public void SetSnapshot_NullStateIsSafe() {
            Scenario scenario = BuildScenarioWithDefaults();
            Store store = new Store(scenario);
            Snapshot snap = new Snapshot { state = null! };

            Assert.DoesNotThrow(() => store.SetSnapshot(snap));
            Assert.That(scenario.initial_state.current_node, Is.EqualTo("title"));
        }
    }
}