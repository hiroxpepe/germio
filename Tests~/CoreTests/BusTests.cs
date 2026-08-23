// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;
using Germio.Systems;

namespace Germio.Tests.Systems {
    /// <summary>
    /// Unit tests for Bus.
    /// Verifies zone dispatch, G2 Layer-1 area-entry guard, signal dispatch, and edge cases.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class BusTests {
#nullable enable
        Store _store = null!;
        Bus _hub = null!;

        // -----------------------------------------------------------------------------------------
        // Helpers

        /// <summary>Builds a minimal root with one level containing an additive counter event.</summary>
        static Scenario BuildRoot(string trigger = "vol_test", bool once = false) {
            var root = new Scenario();
            root.initial_state.current_node = "level_01";
            root.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> {
                    new Node {
                        id = "level_01",
                        name = "Level 1",
                        kind = "level",
                        scene = "level_01",
                        rules = new List<Rule> {
                            new Rule {
                                id        = "evt_counter",
                                trigger   = trigger,
                                condition = "",
                                once      = once,
                                command    = new Command {
                                    update_counter = new UpdateCounter {
                                        key   = "hit_count",
                                        delta = 1f,
                                        op    = CounterOp.Add
                                    }
                                }
                            }
                        }
                    }
                }
            };
            return root;
        }

        [SetUp]
        public void SetUp() {
            _store = new Store(BuildRoot());
            _store.SetSnapshot(new Snapshot());  // Initialize snapshot for history recording
            _hub   = new Bus(_store);
        }

        // -----------------------------------------------------------------------------------------
        // OnZoneEnter — basic dispatch

        [Test]
        public void OnZoneEnter_MatchingTrigger_ExecutesAction() {
            _hub.OnZoneEnter(zone_id: "vol_test");
            Assert.That(_store.Scenario.initial_state.counters["hit_count"], Is.EqualTo(1f));
        }

        [Test]
        public void OnZoneEnter_NonMatchingTrigger_DoesNotExecute() {
            _hub.OnZoneEnter(zone_id: "vol_other");
            Assert.That(_store.Scenario.initial_state.counters.ContainsKey("hit_count"), Is.False);
        }

        [Test]
        public void OnZoneEnter_MarksDirty() {
            _hub.OnZoneEnter(zone_id: "vol_test");
            Assert.That(_store.IsDirty, Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // G2 Layer-1: area-entry deduplication

        [Test]
        public void OnZoneEnter_SameId_SecondCallIsIgnored() {
            _hub.OnZoneEnter(zone_id: "vol_test");
            _hub.OnZoneEnter(zone_id: "vol_test"); // G2 guard
            Assert.That(_store.Scenario.initial_state.counters["hit_count"], Is.EqualTo(1f));
        }

        [Test]
        public void OnZoneEnter_SameId_ThreeTimes_StillOneDispatch() {
            _hub.OnZoneEnter(zone_id: "vol_test");
            _hub.OnZoneEnter(zone_id: "vol_test");
            _hub.OnZoneEnter(zone_id: "vol_test");
            Assert.That(_store.Scenario.initial_state.counters["hit_count"], Is.EqualTo(1f));
        }

        [Test]
        public void OnZoneExit_ClearsGuard_AllowsReentry() {
            _hub.OnZoneEnter(zone_id: "vol_test");
            _hub.OnZoneExit(zone_id: "vol_test");
            _hub.OnZoneEnter(zone_id: "vol_test"); // should fire again
            Assert.That(_store.Scenario.initial_state.counters["hit_count"], Is.EqualTo(2f));
        }

        [Test]
        public void OnZoneExit_WithoutPriorEnter_DoesNotThrow() {
            Assert.DoesNotThrow(() => _hub.OnZoneExit(zone_id: "vol_test"));
        }

        [Test]
        public void OnZoneEnter_DifferentZones_EachDispatchIndependently() {
            var root = new Scenario();
            root.initial_state.current_node = "level_01";
            root.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> {
                    new Node {
                        id     = "level_01",
                        name = "Level 1",
                        kind = "level",
                        scene = "level_01",
                        rules = new List<Rule> {
                            new Rule {
                                id = "evt_a", trigger = "vol_a", condition = "", once = false,
                                command = new Command {
                                    update_counter = new UpdateCounter {
                                        key = "count_a", delta = 1f, op = CounterOp.Add
                                    }
                                }
                            },
                            new Rule {
                                id = "evt_b", trigger = "vol_b", condition = "", once = false,
                                command = new Command {
                                    update_counter = new UpdateCounter {
                                        key = "count_b", delta = 1f, op = CounterOp.Add
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var store = new Store(root);
            var hub   = new Bus(store);

            hub.OnZoneEnter(zone_id: "vol_a");
            hub.OnZoneEnter(zone_id: "vol_b");

            Assert.That(store.Scenario.initial_state.counters["count_a"], Is.EqualTo(1f));
            Assert.That(store.Scenario.initial_state.counters["count_b"], Is.EqualTo(1f));
        }

        // -----------------------------------------------------------------------------------------
        // Publish — no guard, always dispatches

        [Test]
        public void Publish_DispatchesTrigger() {
            _hub.Publish(signal_id: "vol_test");
            Assert.That(_store.Scenario.initial_state.counters["hit_count"], Is.EqualTo(1f));
        }

        [Test]
        public void Publish_CalledTwice_DispatchesTwice() {
            _hub.Publish(signal_id: "vol_test");
            _hub.Publish(signal_id: "vol_test");
            Assert.That(_store.Scenario.initial_state.counters["hit_count"], Is.EqualTo(2f));
        }

        [Test]
        public void Publish_WhileZoneActive_StillDispatches() {
            _hub.OnZoneEnter(zone_id: "vol_test");      // area guard activated
            _hub.Publish(signal_id: "vol_test"); // signal bypasses area guard
            Assert.That(_store.Scenario.initial_state.counters["hit_count"], Is.EqualTo(2f));
        }

        // -----------------------------------------------------------------------------------------
        // G2 combined: once=true event + area guard

        [Test]
        public void G2_OnceTrueEvent_AreaEnter_FiresOnce_EvenAfterExit() {
            _store = new Store(BuildRoot(once: true));
            _store.SetSnapshot(new Snapshot());  // Initialize snapshot for history recording
            _hub   = new Bus(_store);

            _hub.OnZoneEnter(zone_id: "vol_test");  // fires, adds to fired_rules
            _hub.OnZoneExit(zone_id: "vol_test");   // clears area guard
            _hub.OnZoneEnter(zone_id: "vol_test");  // G2 Layer-2 blocks via fired_rules

            Assert.That(_store.Scenario.initial_state.counters["hit_count"], Is.EqualTo(1f),
                "G2 Layer-2 once-guard must block re-entry even after area exit");
        }
    }
}