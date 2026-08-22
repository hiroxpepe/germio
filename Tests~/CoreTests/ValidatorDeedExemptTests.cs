// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for holding old checks back where a deed is at work
    /// (germio TASK-051 to TASK-053).
    ///
    /// V007 warns on an empty condition, V008 on once=false with set_flag, and
    /// V010 on a command with no effect. Every deed rule would trip at least one
    /// of them, and with 64 characters running the log would fill with warnings
    /// that mean nothing, hiding the ones that do.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ValidatorDeedExemptTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Scenario buildScenario(Rule rule) {
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

        static RequestDeed buildDeed(Command? held = null) {
            return new RequestDeed {
                target = new Target { kind = "Ground", reach = 15.0f, spread = 90.0f },
                condition = string.Empty,
                motion = "walk",
                act = string.Empty,
                until = new Until { near = 2.0f },
                command = held ?? new Command {
                    update_need = new List<UpdateNeed> {
                        new UpdateNeed { key = "curiosity", delta = -25f }
                    }
                }
            };
        }

        static IEnumerable<ValidationResult> resultsFor(string rule_id, IList<ValidationResult> all) {
            return all.Where(r => r.RuleID == rule_id);
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-051: V007, an empty condition

        [Test, Description("V007 is held back where a rule names an actor")]
        public void V007_ActorRule_HeldBack() {
            var rule = new Rule {
                id = "r_deed", trigger = "sig_behavior_explore", condition = string.Empty,
                once = false, actor = "npc_01",
                command = new Command { request_deed = buildDeed() }
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(resultsFor(rule_id: "V007", all: results), Is.Empty);
        }

        [Test, Description("V007 still warns where a world rule holds an empty condition")]
        public void V007_WorldRule_StillWarns() {
            var rule = new Rule {
                id = "r_world", trigger = "sig_gate", condition = string.Empty,
                once = true, actor = string.Empty,
                command = new Command { set_flag = new SetFlag { key = "gate", value = true } }
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(resultsFor(rule_id: "V007", all: results).Count(), Is.EqualTo(1));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-052: V008, once=false with set_flag

        [Test, Description("V008 is held back where the set_flag sits inside a deed")]
        public void V008_SetFlagInsideDeed_HeldBack() {
            var rule = new Rule {
                id = "r_give", trigger = "sig_behavior_give", condition = "flags.ready == true",
                once = false, actor = "npc_01",
                command = new Command {
                    request_deed = buildDeed(held: new Command {
                        set_flag = new SetFlag { key = "gift_given", value = true }
                    })
                }
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(resultsFor(rule_id: "V008", all: results), Is.Empty);
        }

        [Test, Description("V008 still warns where a set_flag sits outside a deed")]
        public void V008_SetFlagOutsideDeed_StillWarns() {
            var rule = new Rule {
                id = "r_loop", trigger = "sig_gate", condition = "flags.ready == true",
                once = false, actor = string.Empty,
                command = new Command { set_flag = new SetFlag { key = "gate", value = true } }
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(resultsFor(rule_id: "V008", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V008 still warns where a set_flag sits beside a deed, not inside it")]
        public void V008_SetFlagBesideDeed_StillWarns() {
            var rule = new Rule {
                id = "r_both", trigger = "sig_behavior_give", condition = "flags.ready == true",
                once = false, actor = "npc_01",
                command = new Command {
                    set_flag = new SetFlag { key = "gate", value = true },
                    request_deed = buildDeed()
                }
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(resultsFor(rule_id: "V008", all: results).Count(), Is.EqualTo(1),
                "A set_flag beside a deed runs every time the rule fires, so the warning still holds.");
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-053: V010, a command with no effect

        [Test, Description("V010 lets a command holding a request_deed alone through")]
        public void V010_RequestDeedAlone_LetThrough() {
            var rule = new Rule {
                id = "r_deed", trigger = "sig_behavior_explore", condition = "flags.ready == true",
                once = false, actor = "npc_01",
                command = new Command { request_deed = buildDeed() }
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(resultsFor(rule_id: "V010", all: results), Is.Empty);
        }

        [Test, Description("V010 lets a command holding an update_need alone through")]
        public void V010_UpdateNeedAlone_LetThrough() {
            var rule = new Rule {
                id = "r_need", trigger = "sig_landed", condition = "flags.ready == true",
                once = false, actor = "npc_01",
                command = new Command {
                    update_need = new List<UpdateNeed> {
                        new UpdateNeed { key = "curiosity", delta = -25f }
                    }
                }
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(resultsFor(rule_id: "V010", all: results), Is.Empty);
        }

        [Test, Description("V010 still calls out a command that truly holds nothing")]
        public void V010_EmptyCommand_StillErrors() {
            var rule = new Rule {
                id = "r_empty", trigger = "sig_gate", condition = "flags.ready == true",
                once = true, actor = string.Empty,
                command = new Command()
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(resultsFor(rule_id: "V010", all: results).Count(), Is.EqualTo(1));
        }

        ///////////////////////////////////////////////////////////////////////
        // A whole deed rule, as modio would write one, trips nothing

        [Test, Description("A deed rule as modio writes one raises no warning at all")]
        public void WholeDeedRule_RaisesNothing() {
            var rule = new Rule {
                id = "rule_explore", trigger = "sig_behavior_explore", condition = string.Empty,
                once = false, actor = "place_curious_01",
                command = new Command {
                    request_deed = new RequestDeed {
                        target = new Target { kind = "Ground", reach = 15.0f, spread = 90.0f },
                        condition = "history.time_since(kind=met, target_id=$target) > 60",
                        motion = "walk",
                        until = new Until { meets = "$target" },
                        command = new Command {
                            update_need = new List<UpdateNeed> {
                                new UpdateNeed { key = "curiosity", delta = -25f }
                            },
                            record_event = new RecordEvent { kind = "met", target_id = "$target" }
                        }
                    }
                }
            };

            var results = Validator.Validate(scenario: buildScenario(rule: rule));

            Assert.That(results, Is.Empty,
                "A well-written deed rule must raise nothing at all: with 64 characters, "
                + "warnings that mean nothing would hide the ones that do.");
        }
    }
}
