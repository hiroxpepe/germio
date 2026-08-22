// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for checking an actor against the personas that answer to it
    /// (germio TASK-058).
    ///
    /// V036: an actor names a persona. A slip in the name — an O for a 0 — would
    /// leave a rule that fires for nobody, and nothing would say so.
    ///
    /// germio knows nothing of animo, and holds no persona of its own. So the
    /// names are handed in: given none, this check does not run at all, and every
    /// caller standing today goes on as it was.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ValidatorActorTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Scenario buildScenario(params Rule[] rules) {
            var node = new Node {
                id = "node1", name = "Node 1", kind = "world", scene = "Level_1",
                children = new List<Node>(),
                next = new List<Next>(),
                rules = new List<Rule>(rules)
            };
            var scenario = new Scenario();
            scenario.initial_state = new State();
            scenario.initial_state.flags     = new Map<string, bool>();
            scenario.initial_state.counters  = new Map<string, float>();
            scenario.initial_state.inventory = new Map<string, int>();
            scenario.root = node;
            return scenario;
        }

        static Rule buildRule(string id, string actor) {
            return new Rule {
                id = id, trigger = "sig_behavior_explore", condition = string.Empty,
                once = false, actor = actor,
                command = new Command { set_flag = new SetFlag { key = $"{id}_done", value = true } }
            };
        }

        static IEnumerable<ValidationResult> resultsFor(string rule_id, IList<ValidationResult> all) {
            return all.Where(r => r.RuleID == rule_id);
        }

        ///////////////////////////////////////////////////////////////////////
        // With no names handed in, nothing is checked

        [Test, Description("V036 does not run at all where no names are handed in")]
        public void V036_NoNamesGiven_DoesNotRun() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "nobody_at_all"));

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V036", all: results), Is.Empty,
                "germio holds no persona of its own, so with no names handed in it can say nothing.");
        }

        ///////////////////////////////////////////////////////////////////////
        // With names handed in

        [Test, Description("V036: an actor no persona answers to is an error")]
        public void V036_UnknownActor_ReturnsError() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "place_curious_O1"));
            var known = new[] { "place_curious_01", "company_seeking_01" };

            var results = Validator.Validate(scenario: scenario, known_actors: known);

            var v036 = resultsFor(rule_id: "V036", all: results).ToList();
            Assert.That(v036, Has.Count.EqualTo(1));
            Assert.That(v036[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        [Test, Description("V036: an actor a persona answers to raises nothing")]
        public void V036_KnownActor_ReturnsNothing() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "place_curious_01"));
            var known = new[] { "place_curious_01", "company_seeking_01" };

            var results = Validator.Validate(scenario: scenario, known_actors: known);

            Assert.That(resultsFor(rule_id: "V036", all: results), Is.Empty);
        }

        [Test, Description("V036: an empty actor raises nothing, since it names the world")]
        public void V036_EmptyActor_ReturnsNothing() {
            var scenario = buildScenario(buildRule(id: "r_world", actor: string.Empty));
            var known = new[] { "place_curious_01" };

            var results = Validator.Validate(scenario: scenario, known_actors: known);

            Assert.That(resultsFor(rule_id: "V036", all: results), Is.Empty);
        }

        [Test, Description("V036: two bad actors raise two errors")]
        public void V036_TwoUnknownActors_ReturnsTwoErrors() {
            var scenario = buildScenario(
                buildRule(id: "r_a", actor: "place_curious_O1"),
                buildRule(id: "r_b", actor: "company_seeking_l1"));
            var known = new[] { "place_curious_01", "company_seeking_01" };

            var results = Validator.Validate(scenario: scenario, known_actors: known);

            Assert.That(resultsFor(rule_id: "V036", all: results).Count(), Is.EqualTo(2));
        }

        [Test, Description("V036: letters count, so a name off by case is an error")]
        public void V036_ActorOffByCase_ReturnsError() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "Place_Curious_01"));
            var known = new[] { "place_curious_01" };

            var results = Validator.Validate(scenario: scenario, known_actors: known);

            Assert.That(resultsFor(rule_id: "V036", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V036: an empty list of names calls out every actor named")]
        public void V036_EmptyNameList_CallsOutEveryActor() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "place_curious_01"));

            var results = Validator.Validate(scenario: scenario, known_actors: new string[0]);

            Assert.That(resultsFor(rule_id: "V036", all: results).Count(), Is.EqualTo(1),
                "An empty list is not the same as none at all: it says no persona stands.");
        }

        ///////////////////////////////////////////////////////////////////////
        // Every caller standing today

        [Test, Description("The old one-argument call still works, and every old check still runs")]
        public void OldCall_StillWorks() {
            var scenario = buildScenario(new Rule {
                id = "r_empty", trigger = "sig", condition = "flags.ready == true",
                once = true, actor = string.Empty,
                command = new Command()
            });

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V010", all: results).Count(), Is.EqualTo(1),
                "Adding a second argument must change nothing for the checks already there.");
        }
    }
}
