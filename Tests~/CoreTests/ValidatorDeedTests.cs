// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for checking request_deed (germio TASK-028 to TASK-031, TASK-038).
    /// V030: a motion outside the seven doing-states — Error.
    /// V031: an until with no key, or more than one — Error.
    /// V032: a request_deed inside a request_deed — Error.
    /// V033: a kind outside germio's own type marks — Error.
    /// V034: an act outside the five — Error.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ValidatorDeedTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Scenario buildScenario(RequestDeed deed) {
            var rule = new Rule {
                id = "r_deed",
                trigger = "sig_behavior_explore",
                condition = "flags.ready == true",
                once = false,
                actor = "npc_01",
                command = new Command { request_deed = deed }
            };
            var node = new Node {
                id = "node1", name = "Node 1", kind = "world", scene = "",
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

        /// <summary>A deed that raises nothing at all, to change one part of at a time.</summary>
        static RequestDeed goodDeed() {
            return new RequestDeed {
                target = new Target { kind = "Ground", reach = 15.0f, spread = 90.0f },
                condition = string.Empty,
                motion = "walk",
                act = string.Empty,
                until = new Until { near = 2.0f },
                command = new Command { set_flag = new SetFlag { key = "done", value = true } }
            };
        }

        static IEnumerable<ValidationResult> resultsFor(string rule_id, IList<ValidationResult> all) {
            return all.Where(r => r.RuleID == rule_id);
        }

        ///////////////////////////////////////////////////////////////////////
        // A deed that is well built raises none of the five

        [Test, Description("A well-built deed raises none of V030 to V034")]
        public void GoodDeed_RaisesNone() {
            var results = Validator.Validate(scenario: buildScenario(deed: goodDeed()));

            foreach (string id in new[] { "V030", "V031", "V032", "V033", "V034" }) {
                Assert.That(resultsFor(rule_id: id, all: results), Is.Empty, $"{id} should not fire");
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // V030: motion

        [Test, Description("V030: a motion outside the seven is an error")]
        public void V030_UnknownMotion_ReturnsError() {
            var deed = goodDeed();
            deed.motion = "fly";

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            var v030 = resultsFor(rule_id: "V030", all: results).ToList();
            Assert.That(v030, Has.Count.EqualTo(1));
            Assert.That(v030[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        [Test, Description("V030: an empty motion is an error")]
        public void V030_EmptyMotion_ReturnsError() {
            var deed = goodDeed();
            deed.motion = string.Empty;

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            Assert.That(resultsFor(rule_id: "V030", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V030: every one of the seven doing-states is let through")]
        public void V030_EverySevenMotion_ReturnsNothing() {
            foreach (string motion in new[] { "idle", "walk", "run", "backward", "jump", "abort_jump", "stop" }) {
                var deed = goodDeed();
                deed.motion = motion;

                var results = Validator.Validate(scenario: buildScenario(deed: deed));

                Assert.That(resultsFor(rule_id: "V030", all: results), Is.Empty, $"motion '{motion}' should be let through");
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // V031: until

        [Test, Description("V031: an until with no key at all is an error")]
        public void V031_UntilWithNoKey_ReturnsError() {
            var deed = goodDeed();
            deed.until = new Until();

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            var v031 = resultsFor(rule_id: "V031", all: results).ToList();
            Assert.That(v031, Has.Count.EqualTo(1));
            Assert.That(v031[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        [Test, Description("V031: an until missing altogether is an error")]
        public void V031_UntilMissing_ReturnsError() {
            var deed = goodDeed();
            deed.until = null;

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            Assert.That(resultsFor(rule_id: "V031", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V031: an until with two keys is an error")]
        public void V031_UntilWithTwoKeys_ReturnsError() {
            var deed = goodDeed();
            deed.until = new Until { near = 2.0f, elapsed = 4.0f };

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            Assert.That(resultsFor(rule_id: "V031", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V031: every one of the four ways is let through on its own")]
        public void V031_EveryFourUntil_ReturnsNothing() {
            var untils = new List<Until> {
                new Until { near = 2.0f },
                new Until { meets = "$target" },
                new Until { elapsed = 4.0f },
                new Until { @while = "other_near" }
            };
            foreach (var until in untils) {
                var deed = goodDeed();
                deed.until = until;

                var results = Validator.Validate(scenario: buildScenario(deed: deed));

                Assert.That(resultsFor(rule_id: "V031", all: results), Is.Empty);
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // V032: a deed inside a deed

        [Test, Description("V032: a request_deed inside a request_deed is an error")]
        public void V032_DeedInsideDeed_ReturnsError() {
            var deed = goodDeed();
            deed.command = new Command { request_deed = goodDeed() };

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            var v032 = resultsFor(rule_id: "V032", all: results).ToList();
            Assert.That(v032, Has.Count.EqualTo(1));
            Assert.That(v032[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        ///////////////////////////////////////////////////////////////////////
        // V033: kind

        [Test, Description("V033: a kind outside the type marks is an error")]
        public void V033_UnknownKind_ReturnsError() {
            var deed = goodDeed();
            deed.target = new Target { kind = "Dragon", reach = 15.0f, spread = 90.0f };

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            var v033 = resultsFor(rule_id: "V033", all: results).ToList();
            Assert.That(v033, Has.Count.EqualTo(1));
            Assert.That(v033[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        [Test, Description("V033: an empty kind is an error")]
        public void V033_EmptyKind_ReturnsError() {
            var deed = goodDeed();
            deed.target = new Target { kind = string.Empty, reach = 15.0f, spread = 90.0f };

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            Assert.That(resultsFor(rule_id: "V033", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V033: no target at all raises nothing")]
        public void V033_NoTarget_ReturnsNothing() {
            var deed = goodDeed();
            deed.target = null;

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            Assert.That(resultsFor(rule_id: "V033", all: results), Is.Empty);
        }

        [Test, Description("V033: every type mark germio holds is let through")]
        public void V033_EveryTypeMark_ReturnsNothing() {
            foreach (string kind in new[] { "Block", "Ground", "Wall", "Item", "Coin",
                                            "Balloon", "Human", "Vehicle", "Home" }) {
                var deed = goodDeed();
                deed.target = new Target { kind = kind, reach = 15.0f, spread = 90.0f };

                var results = Validator.Validate(scenario: buildScenario(deed: deed));

                Assert.That(resultsFor(rule_id: "V033", all: results), Is.Empty, $"kind '{kind}' should be let through");
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // V034: act

        [Test, Description("V034: an act outside the five is an error")]
        public void V034_UnknownAct_ReturnsError() {
            var deed = goodDeed();
            deed.act = "sing";

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            var v034 = resultsFor(rule_id: "V034", all: results).ToList();
            Assert.That(v034, Has.Count.EqualTo(1));
            Assert.That(v034[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        [Test, Description("V034: an empty act raises nothing, since most deeds take none")]
        public void V034_EmptyAct_ReturnsNothing() {
            var deed = goodDeed();
            deed.act = string.Empty;

            var results = Validator.Validate(scenario: buildScenario(deed: deed));

            Assert.That(resultsFor(rule_id: "V034", all: results), Is.Empty);
        }

        [Test, Description("V034: every one of the five acts is let through")]
        public void V034_EveryFiveAct_ReturnsNothing() {
            foreach (string act in new[] { "hand_over", "take_up", "put_down", "show", "tend" }) {
                var deed = goodDeed();
                deed.act = act;

                var results = Validator.Validate(scenario: buildScenario(deed: deed));

                Assert.That(resultsFor(rule_id: "V034", all: results), Is.Empty, $"act '{act}' should be let through");
            }
        }
    }
}
