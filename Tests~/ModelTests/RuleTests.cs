// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for Rule class.
    /// Verifies the actor field added for modio (germio TASK-016).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class RuleTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Default values

        [Test, Description("Rule defaults to an empty actor, meaning the world's own rule")]
        public void Rule_Actor_DefaultsToEmpty() {
            Rule rule = new Rule();

            Assert.That(rule.actor, Is.EqualTo(string.Empty));
        }

        ///////////////////////////////////////////////////////////////////////
        // actor

        [Test, Description("Rule holds an actor when one is set")]
        public void Rule_Actor_SetCorrectly() {
            Rule rule = new Rule();
            rule.actor = "place_curious_01";

            Assert.That(rule.actor, Is.EqualTo("place_curious_01"));
        }

        [Test, Description("Rule serializes actor to JSON correctly")]
        public void Rule_Serialize_IncludesActor() {
            Rule rule = new Rule();
            rule.id = "rule_explore";
            rule.actor = "place_curious_01";

            string json = JsonConvert.SerializeObject(value: rule);

            Assert.That(json, Does.Contain("\"actor\":\"place_curious_01\""));
        }

        [Test, Description("Rule deserializes actor from JSON correctly")]
        public void Rule_Deserialize_RestoresActor() {
            string json = @"{""id"":""rule_explore"",""actor"":""place_curious_01""}";

            Rule? rule = JsonConvert.DeserializeObject<Rule>(value: json);

            Assert.That(rule, Is.Not.Null);
            Assert.That(rule!.actor, Is.EqualTo("place_curious_01"));
        }

        [Test, Description("Rule deserializes to an empty actor when the JSON holds none")]
        public void Rule_Deserialize_NoActor_GivesEmpty() {
            string json = @"{""id"":""rule_gate"",""trigger"":""sig_gate""}";

            Rule? rule = JsonConvert.DeserializeObject<Rule>(value: json);

            Assert.That(rule, Is.Not.Null);
            Assert.That(rule!.actor, Is.EqualTo(string.Empty));
        }

        ///////////////////////////////////////////////////////////////////////
        // Property count enforcement

        [Test, Description("Rule has exactly 6 properties")]
        public void Rule_PropertyCount_IsExactlySix() {
            var props = typeof(Rule).GetProperties();

            Assert.That(props.Length, Is.EqualTo(6),
                "Rule must have exactly 6 properties: id, trigger, condition, command, once, actor.");
        }
    }
}
