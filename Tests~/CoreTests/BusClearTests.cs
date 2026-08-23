// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;
using Germio.Systems;

namespace Germio.Tests.Systems {
    /// <summary>
    /// Tests Bus.ClearActiveZones() — clears stale zone guard state on scene transitions (P5-T3).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class BusClearTests {
#nullable enable

        [Test]
        public void ClearActiveZones_AllowsZoneReentry_AfterClear() {
            var store = buildRepeatingStore();
            var bus   = new Bus(store: store);

            bus.OnZoneEnter(zone_id: "vol_goal");
            bus.OnZoneEnter(zone_id: "vol_goal"); // suppressed by G2 guard
            Assert.That(store.Scenario.initial_state.flags.ContainsKey("goal_hit"), Is.True);
            store.Scenario.initial_state.flags["goal_hit"] = false;

            bus.ClearActiveZones();

            bus.OnZoneEnter(zone_id: "vol_goal"); // should fire again after clear
            Assert.That(store.Scenario.initial_state.flags["goal_hit"], Is.True,
                "After ClearActiveZones, zone enter must fire again.");
        }

        [Test]
        public void ClearActiveZones_MultipleZones_AllCleared() {
            var scenario = buildTwoZoneScenario();
            var store    = new Store(scenario: scenario);
            var bus      = new Bus(store: store);

            bus.OnZoneEnter(zone_id: "zone_a");
            bus.OnZoneEnter(zone_id: "zone_b");
            store.Scenario.initial_state.flags["hit_a"] = false;
            store.Scenario.initial_state.flags["hit_b"] = false;

            bus.ClearActiveZones();

            bus.OnZoneEnter(zone_id: "zone_a");
            bus.OnZoneEnter(zone_id: "zone_b");

            Assert.That(store.Scenario.initial_state.flags["hit_a"], Is.True, "zone_a must fire after clear.");
            Assert.That(store.Scenario.initial_state.flags["hit_b"], Is.True, "zone_b must fire after clear.");
        }

        [Test]
        public void SceneLoader_OnTransition_ClearsActiveZones() {
            var scenario = buildTransitionScenario();
            var store    = new Store(scenario: scenario);
            var bus      = new Bus(store: store);

            string? loaded_scene = null;
            using var loader = new SceneLoader(
                store:      store,
                load_scene: name => loaded_scene = name,
                bus:        bus);

            // Enter vol_goal in level_1 → becomes active in bus
            bus.OnZoneEnter(zone_id: "vol_goal");

            // Transition to level_2 → SceneLoader should call ClearActiveZones
            store.RequestTransition(target_id: "level_2");
            Assert.That(loaded_scene, Is.EqualTo("Scene2"));

            // Re-enter vol_goal in level_2 context — should fire (active_zones cleared)
            bus.OnZoneEnter(zone_id: "vol_goal");
            Assert.That(store.Scenario.initial_state.flags.ContainsKey("hit_lv2"), Is.True,
                "vol_goal must fire in level_2 after SceneLoader clears active zones on transition.");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        static Store buildRepeatingStore() {
            var scenario = new Scenario();
            scenario.initial_state.current_node = "level_1";
            var level = new Node {
                id = "level_1",
                name = "Level 1",
                kind = "level",
                scene = "level_1"
            };
            level.rules.Add(new Rule {
                id      = "rule_goal",
                trigger = "vol_goal",
                once    = false,
                command = new Command { set_flag = new SetFlag { key = "goal_hit", value = true } }
            });
            scenario.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> { level }
            };
            return new Store(scenario: scenario);
        }

        static Scenario buildTwoZoneScenario() {
            var scenario = new Scenario();
            scenario.initial_state.current_node = "level_1";
            var level = new Node {
                id = "level_1",
                name = "Level 1",
                kind = "level",
                scene = "level_1"
            };
            level.rules.Add(new Rule {
                id      = "r1",
                trigger = "zone_a",
                once    = false,
                command = new Command { set_flag = new SetFlag { key = "hit_a", value = true } }
            });
            level.rules.Add(new Rule {
                id      = "r2",
                trigger = "zone_b",
                once    = false,
                command = new Command { set_flag = new SetFlag { key = "hit_b", value = true } }
            });
            scenario.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> { level }
            };
            return scenario;
        }

        static Scenario buildTransitionScenario() {
            var scenario = new Scenario();
            scenario.initial_state.current_node = "level_1";

            var lv1 = new Node {
                id = "level_1",
                name = "Level 1",
                kind = "level",
                scene = "Scene1"
            };
            lv1.rules.Add(new Rule {
                id      = "r_lv1",
                trigger = "vol_goal",
                once    = false,
                command = new Command { set_flag = new SetFlag { key = "hit_lv1", value = true } }
            });

            var lv2 = new Node {
                id = "level_2",
                name = "Level 2",
                kind = "level",
                scene = "Scene2"
            };
            lv2.rules.Add(new Rule {
                id      = "r_lv2",
                trigger = "vol_goal",
                once    = false,
                command = new Command { set_flag = new SetFlag { key = "hit_lv2", value = true } }
            });

            scenario.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> { lv1, lv2 }
            };
            return scenario;
        }
    }
}