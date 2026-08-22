// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

using Germio.Model;
using Germio.Core;
using Germio.Systems;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for passing an actor through the Bus (germio TASK-017).
    /// The Bus carries a signal to the Store, and must carry the actor with it.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class BusActorTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Store buildStore(params Rule[] rules) {
            var node = new Node();
            node.id = "lv_1";
            foreach (var rule in rules) { node.rules.Add(item: rule); }

            var scenario = new Scenario();
            scenario.initial_state = new State();
            scenario.initial_state.current_node = "lv_1";
            scenario.root = node;

            return new Store(scenario: scenario);
        }

        static Rule buildRule(string id, string trigger, string actor, string flag_key) {
            var rule = new Rule();
            rule.id = id;
            rule.trigger = trigger;
            rule.actor = actor;
            rule.once = false;
            rule.command = new Command {
                set_flag = new SetFlag { key = flag_key, value = true }
            };
            return rule;
        }

        static bool flagIsSet(Store store, string key) {
            return store.Scenario.initial_state.flags.ContainsKey(key: key)
                && store.Scenario.initial_state.flags[key];
        }

        ///////////////////////////////////////////////////////////////////////
        // Publish, with no actor named

        [Test, Description("Publish with no actor still fires a world rule")]
        public void Publish_NoActorNamed_FiresWorldRule() {
            var store = buildStore(buildRule(
                id: "r_world", trigger: "sig_despawn", actor: string.Empty, flag_key: "world_fired"));
            var bus = new Bus(store: store);

            bus.Publish(signal_id: "sig_despawn");

            Assert.That(flagIsSet(store: store, key: "world_fired"), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////
        // Publish, with an actor named

        [Test, Description("Publish carries the actor through to the rule")]
        public void Publish_ActorNamed_FiresThatActorsRule() {
            var store = buildStore(buildRule(
                id: "r_npc1", trigger: "sig_behavior_explore", actor: "npc_01", flag_key: "npc1_fired"));
            var bus = new Bus(store: store);

            bus.Publish(signal_id: "sig_behavior_explore", actor: "npc_01");

            Assert.That(flagIsSet(store: store, key: "npc1_fired"), Is.True);
        }

        [Test, Description("Publish does not fire another actor's rule")]
        public void Publish_ActorNamed_DoesNotFireOtherActorsRule() {
            var store = buildStore(buildRule(
                id: "r_npc1", trigger: "sig_behavior_explore", actor: "npc_01", flag_key: "npc1_fired"));
            var bus = new Bus(store: store);

            bus.Publish(signal_id: "sig_behavior_explore", actor: "npc_02");

            Assert.That(flagIsSet(store: store, key: "npc1_fired"), Is.False);
        }
    }
}
