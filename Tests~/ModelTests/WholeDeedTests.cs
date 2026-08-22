// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using NUnit.Framework;
using Newtonsoft.Json;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for reading a whole deed off a real JSON file (germio TASK-035).
    ///
    /// Every part before this was checked on its own. This reads the two rules
    /// modio's own spec sets out, end to end, off a file on disk — actor,
    /// request_deed, the Command held inside, and $target in three places.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class WholeDeedTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Scenario loadScenario() {
            string dir = Directory.GetCurrentDirectory();
            string? found = null;
            for (int i = 0; i < 10 && found == null; i++) {
                string tryPath = Path.Combine(dir, "TestData", "deed_rule.json");
                if (File.Exists(tryPath)) { found = tryPath; break; }
                var parent = Directory.GetParent(dir);
                if (parent == null) { break; }
                dir = parent.FullName;
            }
            Assume.That(found, Is.Not.Null, "deed_rule.json must be found");
            Scenario? scenario = JsonConvert.DeserializeObject<Scenario>(
                value: File.ReadAllText(found!));
            Assert.That(scenario, Is.Not.Null);
            return scenario!;
        }

        static Rule ruleNamed(Scenario scenario, string id) {
            foreach (var rule in scenario.root.rules) {
                if (rule.id == id) { return rule; }
            }
            Assert.Fail($"rule '{id}' not found");
            return new Rule();
        }

        ///////////////////////////////////////////////////////////////////////
        // The whole of an Explore rule

        [Test, Description("A whole Explore rule reads off a real file")]
        public void Explore_ReadsEndToEnd() {
            var rule = ruleNamed(scenario: loadScenario(), id: "rule_explore");

            Assert.That(rule.actor, Is.EqualTo("place_curious_01"));
            Assert.That(rule.trigger, Is.EqualTo("sig_behavior_explore"));
            Assert.That(rule.once, Is.False);

            var deed = rule.command.request_deed;
            Assert.That(deed, Is.Not.Null);
            Assert.That(deed!.target!.kind, Is.EqualTo("Ground"));
            Assert.That(deed.target.reach, Is.EqualTo(15.0f));
            Assert.That(deed.target.spread, Is.EqualTo(90.0f));
            Assert.That(deed.motion, Is.EqualTo("walk"));
            Assert.That(deed.act, Is.EqualTo(string.Empty), "Explore needs no act");
            Assert.That(deed.until!.meets, Is.EqualTo("$target"));
        }

        [Test, Description("The Command held inside an Explore deed reads whole")]
        public void Explore_HeldCommand_ReadsWhole() {
            var rule = ruleNamed(scenario: loadScenario(), id: "rule_explore");
            var held = rule.command.request_deed!.command;

            Assert.That(held.update_need, Has.Count.EqualTo(1));
            Assert.That(held.update_need![0].key, Is.EqualTo("curiosity"));
            Assert.That(held.update_need[0].delta, Is.EqualTo(-25.0f));
            Assert.That(held.record_event!.kind, Is.EqualTo("met"));
        }

        [Test, Description("The $target mark stands in three places, unread")]
        public void Explore_TargetMark_StandsInThreePlaces() {
            var rule = ruleNamed(scenario: loadScenario(), id: "rule_explore");
            var deed = rule.command.request_deed!;

            Assert.That(deed.condition, Does.Contain("$target"));
            Assert.That(deed.until!.meets, Is.EqualTo("$target"));
            Assert.That(deed.command.record_event!.target_id, Is.EqualTo("$target"));
        }

        ///////////////////////////////////////////////////////////////////////
        // The whole of a Give rule

        [Test, Description("A whole Give rule reads off a real file, act and all")]
        public void Give_ReadsEndToEnd() {
            var rule = ruleNamed(scenario: loadScenario(), id: "rule_give");
            var deed = rule.command.request_deed!;

            Assert.That(rule.actor, Is.EqualTo("company_seeking_01"));
            Assert.That(deed.target!.kind, Is.EqualTo("Human"));
            Assert.That(deed.act, Is.EqualTo("hand_over"));
            Assert.That(deed.until!.near, Is.EqualTo(1.5f));
        }

        [Test, Description("One arrival may quiet two wants at once")]
        public void Give_QuietsTwoWants() {
            var rule = ruleNamed(scenario: loadScenario(), id: "rule_give");
            var held = rule.command.request_deed!.command;

            Assert.That(held.update_need, Has.Count.EqualTo(2),
                "Approach landing quiets both loneliness and separation, "
                + "or Call would win for ever.");
            Assert.That(held.update_need![0].key, Is.EqualTo("togetherness"));
            Assert.That(held.update_need[1].key, Is.EqualTo("loneliness"));
        }

        [Test, Description("A deed may hold three commands at once")]
        public void Give_HoldsThreeCommandsAtOnce() {
            var rule = ruleNamed(scenario: loadScenario(), id: "rule_give");
            var held = rule.command.request_deed!.command;

            Assert.That(held.update_need, Is.Not.Null);
            Assert.That(held.record_event, Is.Not.Null);
            Assert.That(held.set_flag, Is.Not.Null,
                "The Executor runs a plain row of ifs, so all three are taken.");
        }

        ///////////////////////////////////////////////////////////////////////
        // Two characters, side by side under one node

        [Test, Description("Two characters' rules sit side by side under one node")]
        public void BothRules_SitUnderOneNode() {
            var scenario = loadScenario();

            Assert.That(scenario.root.rules, Has.Count.EqualTo(2));
            Assert.That(scenario.root.rules[0].actor, Is.Not.EqualTo(scenario.root.rules[1].actor));
        }
    }
}
