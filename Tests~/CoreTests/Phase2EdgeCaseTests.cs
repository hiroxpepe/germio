// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;
using Germio.Systems;

namespace Germio.Tests.Systems {
    /// <summary>
    /// Edge-case tests for Phase 2: Bus and SceneLoader.
    /// Covers empty worlds, null-safe calls, empty IDs, and Bus+SceneLoader end-to-end chain.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class Phase2EdgeCaseTests {
#nullable enable

        // -----------------------------------------------------------------------------------------
        // UTS edge cases

        [Test]
        public void UTS_EmptyRoot_OnZoneEnter_DoesNotThrow() {
            var store = new Store(new Scenario());
            var hub   = new Bus(store);
            Assert.DoesNotThrow(() => hub.OnZoneEnter(zone_id: "vol_test"));
        }

        [Test]
        public void UTS_EmptyZoneId_OnZoneEnter_DoesNotThrow() {
            var store = new Store(new Scenario());
            var hub   = new Bus(store);
            Assert.DoesNotThrow(() => hub.OnZoneEnter(zone_id: string.Empty));
        }

        [Test]
        public void UTS_EmptyZoneId_OnZoneEnter_ThenExit_DoesNotThrow() {
            var store = new Store(new Scenario());
            var hub   = new Bus(store);
            hub.OnZoneEnter(zone_id: string.Empty);
            Assert.DoesNotThrow(() => hub.OnZoneExit(zone_id: string.Empty));
        }

        [Test]
        public void UTS_Publish_EmptyRoot_DoesNotThrow() {
            var store = new Store(new Scenario());
            var hub   = new Bus(store);
            Assert.DoesNotThrow(() => hub.Publish(signal_id: "sig_test"));
        }

        // -----------------------------------------------------------------------------------------
        // DSL edge cases

        [Test]
        public void DSL_EmptyWorlds_TransitionRequested_DoesNotThrow() {
            var store = new Store(new Scenario());
            string? loaded = null;
            var loader = new SceneLoader(store: store, load_scene: name => loaded = name);
            Assert.DoesNotThrow(() => store.RequestTransition(target_id: "level_01"));
            Assert.That(loaded, Is.Null);
            loader.Dispose();
        }

        [Test]
        public void DSL_NullSceneInLevel_DoesNotCallLoad() {
            var root = new Scenario();
            root.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> {
                    new Node {
                        id = "level_null",
                        name = "Null Level",
                        kind = "level",
                        scene = string.Empty
                    }
                }
            };
            string? loaded = null;
            var store  = new Store(root);
            var loader = new SceneLoader(store: store, load_scene: name => loaded = name);

            store.RequestTransition(target_id: "level_null");
            // Empty scene name should be skipped (no load with empty string)
            Assert.That(loaded, Is.Null,
                "A level with empty scene name must not trigger a load");
            loader.Dispose();
        }

        // -----------------------------------------------------------------------------------------
        // End-to-end chain: Bus → Store.Dispatch → Executor(request_transition)
        //                  → Store.request_transition → SceneLoader

        [Test]
        public void EndToEnd_Zone_Chain_LoadsCorrectScene() {
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
                        scene  = "Scene_Level1",
                        rules = new List<Rule> {
                            new Rule {
                                id        = "evt_clear",
                                trigger   = "vol_goal",
                                condition = "",
                                once      = true,
                                command    = new Command { request_transition = "level_02" }
                            }
                        }
                    },
                    new Node {
                        id = "level_02",
                        name = "Level 2",
                        kind = "level",
                        scene = "Scene_Level2"
                    }
                }
            };

            var store  = new Store(root);
            string? loaded = null;
            var loader = new SceneLoader(store: store, load_scene: name => loaded = name);
            var hub    = new Bus(store);

            // Simulate: player enters volume trigger "vol_goal"
            hub.OnZoneEnter(zone_id: "vol_goal");

            Assert.That(loaded, Is.EqualTo("Scene_Level2"),
                "Full chain: vol_goal → Store → request_transition → SceneLoader");
            Assert.That(store.Scenario.initial_state.current_node, Is.EqualTo("level_02"));

            loader.Dispose();
        }

        [Test]
        public void EndToEnd_Signal_Despawn_ReloadsCurrentScene() {
            var root = new Scenario();
            root.initial_state.current_node = "level_02";
            root.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> {
                    new Node {
                        id     = "level_02",
                        name = "Level 2",
                        kind = "level",
                        scene  = "Scene_Level2",
                        rules = new List<Rule> {
                            new Rule {
                                id        = "evt_die",
                                trigger   = "sig_despawn",
                                condition = "",
                                once      = false,
                                command    = new Command { request_transition = "level_02" }
                            }
                        }
                    }
                }
            };

            var store  = new Store(root);
            string? loaded = null;
            var loader = new SceneLoader(store: store, load_scene: name => loaded = name);
            var hub    = new Bus(store);

            // Simulate: player falls and despawns (signal, no area guard)
            hub.Publish(signal_id: "sig_despawn");
            Assert.That(loaded, Is.EqualTo("Scene_Level2"));

            // Simulate: player dies again in the same session (once=false, should fire again)
            loaded = null;
            hub.Publish(signal_id: "sig_despawn");
            Assert.That(loaded, Is.EqualTo("Scene_Level2"),
                "sig_despawn once=false must reload every time");

            loader.Dispose();
        }
    }
}