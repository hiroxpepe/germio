// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for firing a rule at one actor alone (germio TASK-017 to TASK-019).
    /// A rule with no actor belongs to the world; a rule with one belongs to that
    /// character alone. See modio's own docs/modio_spec.md §7.1.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class StoreActorTests {
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
        // TASK-017: DispatchTrigger takes an actor, able to be left out

        [Test, Description("DispatchTrigger still fires a world rule when no actor is named")]
        public void DispatchTrigger_NoActorNamed_FiresWorldRule() {
            var store = buildStore(buildRule(
                id: "r_world", trigger: "sig_gate", actor: string.Empty, flag_key: "world_fired"));

            store.DispatchTrigger(trigger_id: "sig_gate");

            Assert.That(flagIsSet(store: store, key: "world_fired"), Is.True);
        }

        [Test, Description("DispatchTrigger takes an actor without throwing")]
        public void DispatchTrigger_ActorNamed_DoesNotThrow() {
            var store = buildStore(buildRule(
                id: "r_world", trigger: "sig_gate", actor: string.Empty, flag_key: "world_fired"));

            Assert.DoesNotThrow(() => store.DispatchTrigger(trigger_id: "sig_gate", actor: "npc_01"));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-018: a rule with no actor fires whoever calls

        [Test, Description("A rule with no actor fires even when an actor is named")]
        public void DispatchTrigger_WorldRule_FiresForAnyActor() {
            var store = buildStore(buildRule(
                id: "r_world", trigger: "sig_gate", actor: string.Empty, flag_key: "world_fired"));

            store.DispatchTrigger(trigger_id: "sig_gate", actor: "npc_01");

            Assert.That(flagIsSet(store: store, key: "world_fired"), Is.True);
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-019: a rule with an actor fires for that one alone

        [Test, Description("A rule with an actor fires when that same actor calls")]
        public void DispatchTrigger_ActorRule_FiresForSameActor() {
            var store = buildStore(buildRule(
                id: "r_npc1", trigger: "sig_move", actor: "npc_01", flag_key: "npc1_fired"));

            store.DispatchTrigger(trigger_id: "sig_move", actor: "npc_01");

            Assert.That(flagIsSet(store: store, key: "npc1_fired"), Is.True);
        }

        [Test, Description("A rule with an actor does not fire when another actor calls")]
        public void DispatchTrigger_ActorRule_DoesNotFireForOtherActor() {
            var store = buildStore(buildRule(
                id: "r_npc1", trigger: "sig_move", actor: "npc_01", flag_key: "npc1_fired"));

            store.DispatchTrigger(trigger_id: "sig_move", actor: "npc_02");

            Assert.That(flagIsSet(store: store, key: "npc1_fired"), Is.False);
        }

        [Test, Description("A rule with an actor does not fire when no actor is named")]
        public void DispatchTrigger_ActorRule_DoesNotFireForWorld() {
            var store = buildStore(buildRule(
                id: "r_npc1", trigger: "sig_move", actor: "npc_01", flag_key: "npc1_fired"));

            store.DispatchTrigger(trigger_id: "sig_move");

            Assert.That(flagIsSet(store: store, key: "npc1_fired"), Is.False);
        }

        ///////////////////////////////////////////////////////////////////////
        // Two characters together

        [Test, Description("Two actor rules on one trigger fire apart from each other")]
        public void DispatchTrigger_TwoActorRules_FireApart() {
            var store = buildStore(
                buildRule(id: "r_npc1", trigger: "sig_move", actor: "npc_01", flag_key: "npc1_fired"),
                buildRule(id: "r_npc2", trigger: "sig_move", actor: "npc_02", flag_key: "npc2_fired"));

            store.DispatchTrigger(trigger_id: "sig_move", actor: "npc_01");

            Assert.That(flagIsSet(store: store, key: "npc1_fired"), Is.True);
            Assert.That(flagIsSet(store: store, key: "npc2_fired"), Is.False);
        }
    }
}
