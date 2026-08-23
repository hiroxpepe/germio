// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for Store.
    /// Verifies node lookup, trigger dispatch, conditional transitions, and history recording.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class StoreTests {
#nullable enable

        // -----------------------------------------------------------------------------------------
        // Helpers

        static Scenario BuildRootScenario() {
            var scenario = new Scenario();
            scenario.initial_state.current_node = "node_01";
            scenario.root = new Node {
                id = "root",
                name = "Root",
                kind = "world",
                scene = "",
                children = new List<Node> {
                    new Node {
                        id = "node_01",
                        name = "Node 1",
                        kind = "level",
                        scene = "Scene1",
                        next = new List<Next> {
                            new Next { id = "node_02", condition = "flags.zone_cleared" }
                        },
                        rules = new List<Rule> {
                            // Once-shot: sets zone_cleared flag
                            new Rule {
                                id = "evt_clear",
                                trigger = "vol_goal",
                                condition = "",
                                once = true,
                                command = new Command {
                                    set_flag = new SetFlag { key = "zone_cleared", value = true }
                                }
                            },
                            // Conditional: only fires when score >= 100
                            new Rule {
                                id = "evt_score_bonus",
                                trigger = "vol_bonus",
                                condition = "counters.score >= 100",
                                once = false,
                                command = new Command {
                                    update_counter = new UpdateCounter {
                                        key = "score", delta = 50f, op = CounterOp.Add
                                    }
                                }
                            }
                        }
                    },
                    new Node {
                        id = "node_02",
                        name = "Node 2",
                        kind = "level",
                        scene = "Scene2"
                    }
                }
            };
            return scenario;
        }

        // -----------------------------------------------------------------------------------------
        // FindNode

        [Test, Description("Store_FindNode returns the node by id")]
        public void Store_FindNode_ReturnsNodeById() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var node = store.FindNode(node_id: "node_01");
            Assert.That(node, Is.Not.Null);
            Assert.That(node!.id, Is.EqualTo("node_01"));
        }

        [Test, Description("Store_FindNode returns null for unknown id")]
        public void Store_FindNode_ReturnsNullForUnknownId() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var node = store.FindNode(node_id: "ghost_node");
            Assert.That(node, Is.Null);
        }

        [Test, Description("Store_FindNode searches deeply nested children")]
        public void Store_FindNode_SearchesDeeply() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var node = store.FindNode(node_id: "root");
            Assert.That(node, Is.Not.Null);
            Assert.That(node!.id, Is.EqualTo("root"));
        }

        // -----------------------------------------------------------------------------------------
        // GetAllNodes

        [Test, Description("Store_GetAllNodes returns all nodes recursively")]
        public void Store_GetAllNodes_ReturnsAllNodes() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var nodes = store.GetAllNodes();
            Assert.That(nodes, Has.Count.EqualTo(3));  // root + node_01 + node_02
            Assert.That(nodes, Has.Some.Matches<Node>(n => n.id == "root"));
            Assert.That(nodes, Has.Some.Matches<Node>(n => n.id == "node_01"));
            Assert.That(nodes, Has.Some.Matches<Node>(n => n.id == "node_02"));
        }

        // -----------------------------------------------------------------------------------------
        // GetLeafNodes

        [Test, Description("Store_GetLeafNodes returns only nodes with empty children")]
        public void Store_GetLeafNodes_ReturnsOnlyLeaves() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var leaves = store.GetLeafNodes();
            Assert.That(leaves, Has.Count.EqualTo(2));  // node_01 + node_02
            Assert.That(leaves, Has.Some.Matches<Node>(n => n.id == "node_01"));
            Assert.That(leaves, Has.Some.Matches<Node>(n => n.id == "node_02"));
        }

        // -----------------------------------------------------------------------------------------
        // GetNodeDepth

        [Test, Description("Store_GetNodeDepth returns 0 for root")]
        public void Store_GetNodeDepth_ReturnsZeroForRoot() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var depth = store.GetNodeDepth(node_id: "root");
            Assert.That(depth, Is.EqualTo(0));
        }

        [Test, Description("Store_GetNodeDepth returns -1 for unknown node")]
        public void Store_GetNodeDepth_ReturnsMinusOneForUnknown() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var depth = store.GetNodeDepth(node_id: "ghost");
            Assert.That(depth, Is.EqualTo(-1));
        }

        [Test, Description("Store_GetNodeDepth returns correct depth for child")]
        public void Store_GetNodeDepth_ReturnsCorrectDepthForChild() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var depth = store.GetNodeDepth(node_id: "node_01");
            Assert.That(depth, Is.EqualTo(1));
        }

        // -----------------------------------------------------------------------------------------
        // GetAncestors

        [Test, Description("Store_GetAncestors returns ancestors excluding root")]
        public void Store_GetAncestors_ReturnsAncestors() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var ancestors = store.GetAncestors(node_id: "node_01");
            Assert.That(ancestors, Has.Count.EqualTo(1));
            Assert.That(ancestors[0].id, Is.EqualTo("root"));
        }

        // -----------------------------------------------------------------------------------------
        // GetNextNode

        [Test, Description("Store_GetNextNode returns the first transition target")]
        public void Store_GetNextNode_ReturnsFirstTransition() {
            var scenario = BuildRootScenario();
            scenario.initial_state.flags["zone_cleared"] = true;
            var store = new Store(scenario);

            var next = store.GetNextNode(current_node_id: "node_01");
            Assert.That(next, Is.Not.Null);
            Assert.That(next!.id, Is.EqualTo("node_02"));
        }

        [Test, Description("Store_GetNextNode returns null when no transitions")]
        public void Store_GetNextNode_ReturnsNullWhenNoTransitions() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var next = store.GetNextNode(current_node_id: "node_02");
            Assert.That(next, Is.Null);
        }

        [Test, Description("Store_GetNextNode returns null for unknown node")]
        public void Store_GetNextNode_ReturnsNullForUnknownNode() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);

            var next = store.GetNextNode(current_node_id: "ghost");
            Assert.That(next, Is.Null);
        }

        // -----------------------------------------------------------------------------------------
        // DispatchTrigger — basic fire

        [Test]
        public void DispatchTrigger_MatchingTrigger_FiresAction() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);
            store.DispatchTrigger(trigger_id: "vol_goal");
            Assert.That(store.Scenario.initial_state.flags.ContainsKey("zone_cleared"), Is.True);
            Assert.That(store.Scenario.initial_state.flags["zone_cleared"], Is.True);
        }

        [Test]
        public void DispatchTrigger_NoMatchingTrigger_DoesNotMutate() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);
            store.DispatchTrigger(trigger_id: "vol_other");
            Assert.That(store.Scenario.initial_state.flags.ContainsKey("zone_cleared"), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // RecordHistoryEvent

        [Test]
        public void RecordHistoryEvent_WithSnapshot_RecordsEvent() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);
            
            var snapshot = new Snapshot {
                state = new State(),
                history = new History { entries = new List<HistoryEntry>(), max_entries = 1000 }
            };
            store.SetSnapshot(snapshot: snapshot);
            
            store.RecordHistoryEvent(kind: "node_enter", target_id: "node_01");
            
            Assert.That(snapshot.history.entries, Has.Count.EqualTo(1));
            Assert.That(snapshot.history.entries[0].kind, Is.EqualTo("node_enter"));
            Assert.That(snapshot.history.entries[0].target_id, Is.EqualTo("node_01"));
        }

        [Test]
        public void RecordHistoryEvent_WithoutSnapshot_DoesNotThrow() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);
            
            // No snapshot set, should not throw
            Assert.DoesNotThrow(() => 
                store.RecordHistoryEvent(kind: "node_enter", target_id: "node_01"));
        }

        // -----------------------------------------------------------------------------------------
        // isDirty

        [Test]
        public void DispatchTrigger_RuleFires_MarksDirty() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);
            Assert.That(store.IsDirty, Is.False);
            store.DispatchTrigger(trigger_id: "vol_goal");
            Assert.That(store.IsDirty, Is.True);
        }

        [Test]
        public void DispatchTrigger_NoRuleFires_DoesNotMarkDirty() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);
            store.DispatchTrigger(trigger_id: "vol_other");
            Assert.That(store.IsDirty, Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // RequestTransition

        [Test]
        public void RequestTransition_RaisesEvent() {
            var scenario = BuildRootScenario();
            var store = new Store(scenario);
            string? received = null;
            store.TransitionRequested += id => received = id;

            store.RequestTransition(target_id: "node_03");
            Assert.That(received, Is.EqualTo("node_03"));
        }
    }
}