// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for checking a Rule's own update_need entries against the
    /// Need names the Rule's own actor's persona truly holds (germio
    /// TASK-065).
    ///
    /// V037: a command.update_need can name any given Need key at all. A
    /// slip in the name would leave a Rule that moves nothing at all, and
    /// nothing would say so.
    ///
    /// germio knows nothing of animo, and holds no Need names of its own. So
    /// the names are handed in: given none, this check does not run at all,
    /// and every caller standing today goes on as it was.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ValidatorNeedKnowledgeTests {
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

        static Rule buildRule(string id, string actor, params (string key, float delta)[] needs) {
            return new Rule {
                id = id, trigger = "sig_behavior_explore", condition = string.Empty,
                once = false, actor = actor,
                command = new Command {
                    update_need = needs.Select(n => new UpdateNeed { key = n.key, delta = n.delta }).ToList()
                }
            };
        }

        static IEnumerable<ValidationResult> resultsFor(string rule_id, IList<ValidationResult> all) {
            return all.Where(r => r.RuleID == rule_id);
        }

        ///////////////////////////////////////////////////////////////////////
        // With no names handed in, nothing is checked

        [Test, Description("V037 does not run at all where no names are handed in")]
        public void V037_NoNamesGiven_DoesNotRun() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "place_curious_01", ("phantom_need", 10f)));

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V037", all: results), Is.Empty,
                "germio holds no Need names of its own, so with no names handed in it can say nothing.");
        }

        ///////////////////////////////////////////////////////////////////////
        // With names handed in

        [Test, Description("V037: a Need no persona answers to is an error")]
        public void V037_UnknownNeed_ReturnsError() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "place_curious_01", ("phantom_need", 10f)));
            var known = new Dictionary<string, IReadOnlyCollection<string>> {
                ["place_curious_01"] = new[] { "curiosity", "hunger" }
            };

            var results = Validator.Validate(scenario: scenario, known_needs: known);

            var v037 = resultsFor(rule_id: "V037", all: results).ToList();
            Assert.That(v037, Has.Count.EqualTo(1));
            Assert.That(v037[0].Severity, Is.EqualTo(ValidationLevel.Error));
        }

        [Test, Description("V037: a Need a persona answers to raises nothing")]
        public void V037_KnownNeed_ReturnsNothing() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "place_curious_01", ("curiosity", 10f)));
            var known = new Dictionary<string, IReadOnlyCollection<string>> {
                ["place_curious_01"] = new[] { "curiosity", "hunger" }
            };

            var results = Validator.Validate(scenario: scenario, known_needs: known);

            Assert.That(resultsFor(rule_id: "V037", all: results), Is.Empty);
        }

        [Test, Description("V037: an empty actor raises nothing, since it names no persona at all")]
        public void V037_EmptyActor_ReturnsNothing() {
            var scenario = buildScenario(buildRule(id: "r_world", actor: string.Empty, ("phantom_need", 10f)));
            var known = new Dictionary<string, IReadOnlyCollection<string>> {
                ["place_curious_01"] = new[] { "curiosity" }
            };

            var results = Validator.Validate(scenario: scenario, known_needs: known);

            Assert.That(resultsFor(rule_id: "V037", all: results), Is.Empty);
        }

        [Test, Description("V037: an actor no known_needs entry answers to raises nothing of its own — V036 already names that")]
        public void V037_ActorNotInKnownNeeds_ReturnsNothing() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "nobody_at_all", ("phantom_need", 10f)));
            var known = new Dictionary<string, IReadOnlyCollection<string>> {
                ["place_curious_01"] = new[] { "curiosity" }
            };

            var results = Validator.Validate(scenario: scenario, known_needs: known);

            Assert.That(resultsFor(rule_id: "V037", all: results), Is.Empty,
                "An actor no persona answers to is V036's own true job, never V037's.");
        }

        [Test, Description("V037: two bad Need names raise two errors")]
        public void V037_TwoUnknownNeeds_ReturnsTwoErrors() {
            var scenario = buildScenario(
                buildRule(id: "r_a", actor: "place_curious_01", ("phantom_need", 10f)),
                buildRule(id: "r_b", actor: "place_curious_01", ("another_phantom", 5f)));
            var known = new Dictionary<string, IReadOnlyCollection<string>> {
                ["place_curious_01"] = new[] { "curiosity" }
            };

            var results = Validator.Validate(scenario: scenario, known_needs: known);

            Assert.That(resultsFor(rule_id: "V037", all: results).Count(), Is.EqualTo(2));
        }

        [Test, Description("V037: letters count, so a name off by case is an error")]
        public void V037_NeedOffByCase_ReturnsError() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "place_curious_01", ("Curiosity", 10f)));
            var known = new Dictionary<string, IReadOnlyCollection<string>> {
                ["place_curious_01"] = new[] { "curiosity" }
            };

            var results = Validator.Validate(scenario: scenario, known_needs: known);

            Assert.That(resultsFor(rule_id: "V037", all: results).Count(), Is.EqualTo(1));
        }

        [Test, Description("V037: an empty Need set for a known actor calls out every Need named")]
        public void V037_EmptyNeedSetForKnownActor_CallsOutEveryNeed() {
            var scenario = buildScenario(buildRule(id: "r_npc", actor: "place_curious_01", ("curiosity", 10f)));
            var known = new Dictionary<string, IReadOnlyCollection<string>> {
                ["place_curious_01"] = new string[0]
            };

            var results = Validator.Validate(scenario: scenario, known_needs: known);

            Assert.That(resultsFor(rule_id: "V037", all: results).Count(), Is.EqualTo(1),
                "An empty set is not the same as none at all: it says this persona holds no Need named at all.");
        }

        ///////////////////////////////////////////////////////////////////////
        // Every caller standing today

        [Test, Description("The old calls still work, and every old check still runs")]
        public void OldCalls_StillWork() {
            var scenario = buildScenario(new Rule {
                id = "r_empty", trigger = "sig", condition = "flags.ready == true",
                once = true, actor = string.Empty,
                command = new Command()
            });

            var results = Validator.Validate(scenario: scenario);

            Assert.That(resultsFor(rule_id: "V010", all: results).Count(), Is.EqualTo(1),
                "Adding a third argument must change nothing for the checks already there.");
        }
    }
}
