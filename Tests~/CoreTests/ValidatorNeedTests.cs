// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for checking update_need (germio TASK-022, TASK-023).
    /// V028: an empty need key names no Need at all — Error.
    /// V029: a delta of zero moves nothing — Warning.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ValidatorNeedTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Scenario buildScenario(Node? root = null) {
            var scenario = new Scenario();
            scenario.initial_state = new State();
            scenario.initial_state.flags     = new Map<string, bool>();
            scenario.initial_state.counters  = new Map<string, float>();
            scenario.initial_state.inventory = new Map<string, int>();
            scenario.root = root ?? new Node { id = "root" };
            return scenario;
        }

        static Node buildNodeWithNeed(params UpdateNeed[] needs) {
            var rule = new Rule {
                id = "r_need",
                trigger = "sig_behavior_explore",
                condition = "flags.ready == true",
                once = false,
                actor = "npc_01",
                command = new Command { update_need = needs.ToList() }
            };
            return new Node {
                id = "node1", name = "Node 1", kind = "world", scene = "",
                children = new List<Node>(),
                next = new List<Next>(),
                rules = new List<Rule> { rule }
            };
        }

        static IEnumerable<ValidationResult> resultsFor(string rule_id, IList<ValidationResult> all) {
            return all.Where(r => r.RuleID == rule_id);
        }

        ///////////////////////////////////////////////////////////////////////
        // V028: an empty key

        [Test, Description("V028: an empty need key is an error")]
        public void V028_EmptyNeedKey_ReturnsError() {
            var scenario = buildScenario(root: buildNodeWithNeed(
                new UpdateNeed { key = string.Empty, delta = -25f }));

            var results = Validator.Validate(scenario: scenario);

            var v028 = resultsFor(rule_id: "V028", all: results).ToList();
            Assert.That(v028, Has.Count.EqualTo(1));
            Assert.That(v028[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        [Test, Description("V028: a key of spaces alone is an error")]
        public void V028_WhitespaceNeedKey_ReturnsError() {
            var scenario = buildScenario(root: buildNodeWithNeed(
                new UpdateNeed { key = "   ", delta = -25f }));

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V028", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V028: a good key raises nothing")]
        public void V028_GoodNeedKey_ReturnsNothing() {
            var scenario = buildScenario(root: buildNodeWithNeed(
                new UpdateNeed { key = "curiosity", delta = -25f }));

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V028", all: results), Is.Empty);
        }

        [Test, Description("V028: two bad keys raise two errors")]
        public void V028_TwoEmptyKeys_ReturnsTwoErrors() {
            var scenario = buildScenario(root: buildNodeWithNeed(
                new UpdateNeed { key = string.Empty, delta = -25f },
                new UpdateNeed { key = string.Empty, delta = -30f }));

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V028", all: results).Count(), Is.EqualTo(2));
        }

        ///////////////////////////////////////////////////////////////////////
        // V029: a delta of zero

        [Test, Description("V029: a delta of zero is a warning")]
        public void V029_ZeroDelta_ReturnsWarning() {
            var scenario = buildScenario(root: buildNodeWithNeed(
                new UpdateNeed { key = "curiosity", delta = 0f }));

            var results = Validator.Validate(scenario: scenario);

            var v029 = resultsFor(rule_id: "V029", all: results).ToList();
            Assert.That(v029, Has.Count.EqualTo(1));
            Assert.That(v029[0].Severity, Is.EqualTo(ValidationLevel.Warning));
        }

        [Test, Description("V029: a delta below zero raises nothing")]
        public void V029_NegativeDelta_ReturnsNothing() {
            var scenario = buildScenario(root: buildNodeWithNeed(
                new UpdateNeed { key = "curiosity", delta = -25f }));

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V029", all: results), Is.Empty);
        }

        [Test, Description("V029: a delta above zero raises nothing")]
        public void V029_PositiveDelta_ReturnsNothing() {
            var scenario = buildScenario(root: buildNodeWithNeed(
                new UpdateNeed { key = "fear", delta = 40f }));

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V029", all: results), Is.Empty);
        }

        ///////////////////////////////////////////////////////////////////////
        // Nothing to check

        [Test, Description("A command with no update_need raises neither")]
        public void NoUpdateNeed_RaisesNeither() {
            var rule = new Rule {
                id = "r_flag", trigger = "sig", condition = "flags.ready == true",
                command = new Command { set_flag = new SetFlag { key = "gate", value = true } }
            };
            var node = new Node {
                id = "node1", name = "Node 1", kind = "world", scene = "",
                children = new List<Node>(), next = new List<Next>(),
                rules = new List<Rule> { rule }
            };
            var scenario = buildScenario(root: node);

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V028", all: results), Is.Empty);
            Assert.That(resultsFor(rule_id: "V029", all: results), Is.Empty);
        }
    }
}
