// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for listing a node's own rules by actor (germio TASK-044).
    ///
    /// Rules for the world (no actor) and rules for each character (an actor
    /// named) sit side by side under one Node. With many characters, which rule
    /// belongs to which character grows hard to see by eye.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class GrapherActorTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Rule buildRule(string id, string actor) {
            return new Rule {
                id = id, trigger = "sig", condition = string.Empty,
                once = false, actor = actor,
                command = new Command { set_flag = new SetFlag { key = id, value = true } }
            };
        }

        static Scenario buildScenario(Node root) {
            var scenario = new Scenario();
            scenario.initial_state = new State();
            scenario.initial_state.flags     = new Map<string, bool>();
            scenario.initial_state.counters  = new Map<string, float>();
            scenario.initial_state.inventory = new Map<string, int>();
            scenario.root = root;
            return scenario;
        }

        static Node buildNode(string id, string scene, List<Rule> rules, List<Node>? children = null) {
            return new Node {
                id = id, name = id, kind = "world", scene = scene,
                children = children ?? new List<Node>(),
                next = new List<Next>(),
                rules = rules
            };
        }

        ///////////////////////////////////////////////////////////////////////
        // Sorting one node's own rules

        [Test, Description("Rules with no actor come back under the world")]
        public void ByActor_WorldRules_ComeBackUnderTheWorld() {
            var scenario = buildScenario(root: buildNode(id: "lv_1", scene: "Level_1",
                rules: new List<Rule> {
                    buildRule(id: "r_gate", actor: string.Empty),
                    buildRule(id: "r_door", actor: string.Empty)
                }));

            var sorted = Grapher.RulesByActor(scenario: scenario);

            Assert.That(sorted.ContainsKey(key: string.Empty), Is.True);
            Assert.That(sorted[string.Empty].Select(r => r.id),
                Is.EquivalentTo(new[] { "r_gate", "r_door" }));
        }

        [Test, Description("Each character's rules come back under that character")]
        public void ByActor_ActorRules_ComeBackApart() {
            var scenario = buildScenario(root: buildNode(id: "lv_1", scene: "Level_1",
                rules: new List<Rule> {
                    buildRule(id: "r_npc1_a", actor: "npc_01"),
                    buildRule(id: "r_npc1_b", actor: "npc_01"),
                    buildRule(id: "r_npc2_a", actor: "npc_02")
                }));

            var sorted = Grapher.RulesByActor(scenario: scenario);

            Assert.That(sorted["npc_01"].Select(r => r.id),
                Is.EquivalentTo(new[] { "r_npc1_a", "r_npc1_b" }));
            Assert.That(sorted["npc_02"].Select(r => r.id),
                Is.EquivalentTo(new[] { "r_npc2_a" }));
        }

        [Test, Description("World rules and character rules come back apart from each other")]
        public void ByActor_WorldAndActors_ComeBackApart() {
            var scenario = buildScenario(root: buildNode(id: "lv_1", scene: "Level_1",
                rules: new List<Rule> {
                    buildRule(id: "r_gate", actor: string.Empty),
                    buildRule(id: "r_npc1", actor: "npc_01")
                }));

            var sorted = Grapher.RulesByActor(scenario: scenario);

            Assert.That(sorted, Has.Count.EqualTo(2));
            Assert.That(sorted[string.Empty].Select(r => r.id), Is.EquivalentTo(new[] { "r_gate" }));
            Assert.That(sorted["npc_01"].Select(r => r.id), Is.EquivalentTo(new[] { "r_npc1" }));
        }

        ///////////////////////////////////////////////////////////////////////
        // Reaching every node in the tree

        [Test, Description("Rules are gathered from every node in the tree, not the root alone")]
        public void ByActor_ReachesEveryNode() {
            var child = buildNode(id: "lv_2", scene: "Level_2",
                rules: new List<Rule> { buildRule(id: "r_npc1_deep", actor: "npc_01") });
            var root = buildNode(id: "world", scene: string.Empty,
                rules: new List<Rule> { buildRule(id: "r_npc1_top", actor: "npc_01") },
                children: new List<Node> { child });

            var sorted = Grapher.RulesByActor(scenario: buildScenario(root: root));

            Assert.That(sorted["npc_01"].Select(r => r.id),
                Is.EquivalentTo(new[] { "r_npc1_top", "r_npc1_deep" }));
        }

        ///////////////////////////////////////////////////////////////////////
        // Nothing to sort

        [Test, Description("A tree holding no rules comes back holding nothing")]
        public void ByActor_NoRules_ComesBackEmpty() {
            var scenario = buildScenario(root: buildNode(id: "lv_1", scene: "Level_1",
                rules: new List<Rule>()));

            var sorted = Grapher.RulesByActor(scenario: scenario);

            Assert.That(sorted, Is.Empty);
        }

        [Test, Description("A scenario with no root at all throws nothing")]
        public void ByActor_NoRoot_DoesNotThrow() {
            var scenario = new Scenario();
            scenario.initial_state = new State();

            Assert.DoesNotThrow(() => Grapher.RulesByActor(scenario: scenario));
        }

        ///////////////////////////////////////////////////////////////////////
        // What is given back must not let a reader change what was read

        [Test, Description("The rules given back are the ones that stand, not copies")]
        public void ByActor_GivesBackTheRulesThemselves() {
            var rule = buildRule(id: "r_npc1", actor: "npc_01");
            var scenario = buildScenario(root: buildNode(id: "lv_1", scene: "Level_1",
                rules: new List<Rule> { rule }));

            var sorted = Grapher.RulesByActor(scenario: scenario);

            Assert.That(sorted["npc_01"][0], Is.SameAs(rule),
                "A reader looking at rules wants the rules, not a picture of them.");
        }
    }
}
