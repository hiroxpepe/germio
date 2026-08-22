// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for checking a deed's own condition (germio TASK-056).
    ///
    /// Two things are called condition, and they are not the same:
    ///   Rule.condition         — should this deed begin at all. No $target.
    ///   request_deed.condition — which found thing to take. Holds $target.
    ///
    /// V009 checks a condition by reading it, and a deed condition cannot be read
    /// until $target gives way to an id. At check time no deed is running, so a
    /// stand-in id is put in first — a well-formed one is enough to check shape.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ValidatorDeedConditionTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Scenario buildScenario(string deed_condition) {
            var rule = new Rule {
                id = "r_deed", trigger = "sig_behavior_explore", condition = string.Empty,
                once = false, actor = "npc_01",
                command = new Command {
                    request_deed = new RequestDeed {
                        target = new Target { kind = "Ground", reach = 15.0f, spread = 90.0f },
                        condition = deed_condition,
                        motion = "walk",
                        until = new Until { near = 2.0f },
                        command = new Command { set_flag = new SetFlag { key = "done", value = true } }
                    }
                }
            };
            var node = new Node {
                id = "node1", name = "Node 1", kind = "world", scene = "Level_1",
                children = new List<Node>(),
                next = new List<Next>(),
                rules = new List<Rule> { rule }
            };
            var scenario = new Scenario();
            scenario.initial_state = new State();
            scenario.initial_state.flags     = new Map<string, bool>();
            scenario.initial_state.counters  = new Map<string, float>();
            scenario.initial_state.inventory = new Map<string, int>();
            scenario.root = node;
            return scenario;
        }

        static IEnumerable<ValidationResult> resultsFor(string rule_id, IList<ValidationResult> all) {
            return all.Where(r => r.RuleID == rule_id);
        }

        ///////////////////////////////////////////////////////////////////////
        // A deed condition holding $target is read, once a stand-in is put in

        [Test, Description("A good deed condition holding $target raises nothing")]
        public void GoodDeedCondition_WithMark_RaisesNothing() {
            var scenario = buildScenario(
                deed_condition: "history.time_since(kind=met, target_id=$target) > 60");

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V009", all: results), Is.Empty,
                "$target must give way to a stand-in id before the line is read.");
        }

        [Test, Description("A good deed condition with no mark at all raises nothing")]
        public void GoodDeedCondition_NoMark_RaisesNothing() {
            var scenario = buildScenario(deed_condition: "flags.ready == true");

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V009", all: results), Is.Empty);
        }

        [Test, Description("An empty deed condition raises nothing, since it takes the nearest")]
        public void EmptyDeedCondition_RaisesNothing() {
            var scenario = buildScenario(deed_condition: string.Empty);

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V009", all: results), Is.Empty);
        }

        ///////////////////////////////////////////////////////////////////////
        // A deed condition that cannot be read is called out

        [Test, Description("V009: a deed condition that will not read is an error")]
        public void BadDeedCondition_ReturnsError() {
            var scenario = buildScenario(deed_condition: "history.count(kind=met, target_id=$target");

            var results = Validator.Validate(scenario: scenario);

            var v009 = resultsFor(rule_id: "V009", all: results).ToList();
            Assert.That(v009, Has.Count.EqualTo(1));
            Assert.That(v009[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        [Test, Description("V009: a deed condition with a hanging operator is an error")]
        public void HangingOperator_ReturnsError() {
            var scenario = buildScenario(deed_condition: "flags.ready ==");

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V009", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V009: the mark left in place would not read, so it must give way first")]
        public void MarkWithoutStandIn_WouldNotRead() {
            // ExprLexer knows no '$' at all, so a line still holding the mark
            // throws at read time. This is why a stand-in must be put in first.
            var tokens_threw = false;
            try {
                var tokens = ExprLexer.Tokenize(
                    source: "history.count(kind=met, target_id=$target) == 0");
                ExprParser.Parse(tokens: tokens);
            } catch {
                tokens_threw = true;
            }

            Assert.That(tokens_threw, Is.True,
                "If this ever stops throwing, the stand-in is no longer needed.");
        }

        ///////////////////////////////////////////////////////////////////////
        // The rule's own condition is read as it always was

        [Test, Description("A rule's own condition is still read, with no stand-in put in")]
        public void RuleOwnCondition_StillChecked() {
            var rule = new Rule {
                id = "r_bad", trigger = "sig", condition = "flags.ready ==",
                once = true, actor = string.Empty,
                command = new Command { set_flag = new SetFlag { key = "gate", value = true } }
            };
            var node = new Node {
                id = "node1", name = "Node 1", kind = "world", scene = "Level_1",
                children = new List<Node>(), next = new List<Next>(),
                rules = new List<Rule> { rule }
            };
            var scenario = new Scenario();
            scenario.initial_state = new State();
            scenario.initial_state.flags     = new Map<string, bool>();
            scenario.initial_state.counters  = new Map<string, float>();
            scenario.initial_state.inventory = new Map<string, int>();
            scenario.root = node;

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V009", all: results).Count(), Is.EqualTo(1));
        }

        ///////////////////////////////////////////////////////////////////////
        // Checking must never change what was checked

        [Test, Description("Checking leaves the deed's own condition holding the mark")]
        public void Checking_LeavesTheMarkWhereItWas() {
            var scenario = buildScenario(
                deed_condition: "history.time_since(kind=met, target_id=$target) > 60");

            Validator.Validate(scenario: scenario);

            string after = scenario.root.rules[0].command.request_deed!.condition;
            Assert.That(after, Does.Contain("$target"),
                "A check must never write on what it checks: the mark stands until a deed truly runs.");
        }
    }
}
