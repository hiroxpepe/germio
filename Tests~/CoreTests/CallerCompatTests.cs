// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;

using Germio.Model;
using Germio.Core;
using Germio.Systems;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests holding every caller standing today to its old ways
    /// (germio TASK-042, TASK-057).
    ///
    /// Adding an actor to DispatchTrigger and Publish must change nothing for
    /// the ten calls already made across germio. Every one of them names no
    /// actor, and every one must go on firing the world's own rules.
    ///
    /// A Zone belongs to the world — a place a body walks into — not to any
    /// character, so it never names an actor at all (TASK-057).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class CallerCompatTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Store buildStore(params Rule[] rules) {
            var node = new Node {
                id = "lv_1", name = "Level 1", kind = "world", scene = "Level_1",
                children = new List<Node>(),
                next = new List<Next>(),
                rules = new List<Rule>(rules)
            };
            var scenario = new Scenario();
            scenario.initial_state = new State();
            scenario.initial_state.current_node = "lv_1";
            scenario.root = node;
            return new Store(scenario: scenario);
        }

        static Rule buildWorldRule(string trigger, string flag_key) {
            return new Rule {
                id = $"r_{flag_key}", trigger = trigger, condition = string.Empty,
                once = false, actor = string.Empty,
                command = new Command { set_flag = new SetFlag { key = flag_key, value = true } }
            };
        }

        static bool flagIsSet(Store store, string key) {
            return store.Scenario.initial_state.flags.ContainsKey(key: key)
                && store.Scenario.initial_state.flags[key];
        }

        ///////////////////////////////////////////////////////////////////////
        // Every signal germio itself sends today

        [Test, Description("Every signal germio sends today still fires its world rule")]
        public void EverySignalStanding_StillFires() {
            // Taken off a real read of germio's own Scripts, 2026-08-22:
            //   Despawn.cs  → sig_despawn
            //   Home.cs     → vol_home (three places)
            //   Scene.cs    → signal_btn_start_pressed, _select_, _up_, _down_
            var signals = new[] {
                "sig_despawn", "vol_home",
                "signal_btn_start_pressed", "signal_btn_select_pressed",
                "signal_btn_up_pressed", "signal_btn_down_pressed"
            };

            foreach (string signal in signals) {
                var store = buildStore(buildWorldRule(trigger: signal, flag_key: $"fired_{signal}"));
                var bus = new Bus(store: store);

                bus.Publish(signal_id: signal);

                Assert.That(flagIsSet(store: store, key: $"fired_{signal}"), Is.True,
                    $"'{signal}' is sent by germio itself today, and must go on working.");
            }
        }

        [Test, Description("The reserved _on_enter_node trigger still fires its world rule")]
        public void OnEnterNode_StillFires() {
            var store = buildStore(buildWorldRule(trigger: "_on_enter_node", flag_key: "entered"));

            // SceneLoader.cs calls this straight on the Store, with no actor.
            store.DispatchTrigger(trigger_id: "_on_enter_node");

            Assert.That(flagIsSet(store: store, key: "entered"), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-057: a Zone belongs to the world

        [Test, Description("A Zone fires the world's own rule")]
        public void OnZoneEnter_FiresWorldRule() {
            var store = buildStore(buildWorldRule(trigger: "vol_goal", flag_key: "goal_reached"));
            var bus = new Bus(store: store);

            bus.OnZoneEnter(zone_id: "vol_goal");

            Assert.That(flagIsSet(store: store, key: "goal_reached"), Is.True);
        }

        [Test, Description("A Zone never fires a rule that names an actor")]
        public void OnZoneEnter_NeverFiresAnActorRule() {
            var actor_rule = new Rule {
                id = "r_npc", trigger = "vol_goal", condition = string.Empty,
                once = false, actor = "npc_01",
                command = new Command { set_flag = new SetFlag { key = "npc_fired", value = true } }
            };
            var store = buildStore(actor_rule);
            var bus = new Bus(store: store);

            bus.OnZoneEnter(zone_id: "vol_goal");

            Assert.That(flagIsSet(store: store, key: "npc_fired"), Is.False,
                "A Zone is a place a body walks into, and belongs to no character.");
        }

        [Test, Description("A Zone fires the world's rule even where an actor rule shares the trigger")]
        public void OnZoneEnter_FiresOnlyTheWorldRule() {
            var store = buildStore(
                buildWorldRule(trigger: "vol_goal", flag_key: "world_fired"),
                new Rule {
                    id = "r_npc", trigger = "vol_goal", condition = string.Empty,
                    once = false, actor = "npc_01",
                    command = new Command { set_flag = new SetFlag { key = "npc_fired", value = true } }
                });
            var bus = new Bus(store: store);

            bus.OnZoneEnter(zone_id: "vol_goal");

            Assert.That(flagIsSet(store: store, key: "world_fired"), Is.True);
            Assert.That(flagIsSet(store: store, key: "npc_fired"), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////
        // A world call reaches only world rules

        [Test, Description("Publish with no actor reaches world rules alone")]
        public void PublishWithNoActor_ReachesWorldRulesAlone() {
            var store = buildStore(
                buildWorldRule(trigger: "sig_despawn", flag_key: "world_fired"),
                new Rule {
                    id = "r_npc", trigger = "sig_despawn", condition = string.Empty,
                    once = false, actor = "npc_01",
                    command = new Command { set_flag = new SetFlag { key = "npc_fired", value = true } }
                });
            var bus = new Bus(store: store);

            bus.Publish(signal_id: "sig_despawn");

            Assert.That(flagIsSet(store: store, key: "world_fired"), Is.True);
            Assert.That(flagIsSet(store: store, key: "npc_fired"), Is.False);
        }

        [Test, Description("A named actor reaches both its own rule and the world's")]
        public void PublishWithActor_ReachesBoth() {
            var store = buildStore(
                buildWorldRule(trigger: "sig_despawn", flag_key: "world_fired"),
                new Rule {
                    id = "r_npc", trigger = "sig_despawn", condition = string.Empty,
                    once = false, actor = "npc_01",
                    command = new Command { set_flag = new SetFlag { key = "npc_fired", value = true } }
                });
            var bus = new Bus(store: store);

            bus.Publish(signal_id: "sig_despawn", actor: "npc_01");

            Assert.That(flagIsSet(store: store, key: "world_fired"), Is.True,
                "A world rule fires whoever calls (TASK-018).");
            Assert.That(flagIsSet(store: store, key: "npc_fired"), Is.True);
        }
    }
}
