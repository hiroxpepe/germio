// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for Validator.ValidateJSON (germio TASK-065): the read +
    /// deserialize + Validate step an AssetPostprocessor needs, held Unity-
    /// free so it can be checked with no real Unity open at all. The real
    /// Editor/Dashboard.cs already holds this same three-step shape, held
    /// by a [MenuItem] alone, and untested until now.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ValidatorJsonTests {
#nullable enable

        const string MINIMAL_SCENARIO_JSON = @"{
            ""initial_state"": {
                ""flags"": {}, ""counters"": {}, ""inventory"": {}
            },
            ""root"": {
                ""id"": ""node1"", ""name"": ""Node 1"", ""kind"": ""world"",
                ""scene"": ""Level_1"", ""children"": [], ""next"": [],
                ""rules"": [{
                    ""id"": ""r_npc"", ""trigger"": ""sig_behavior_explore"",
                    ""condition"": """", ""once"": false, ""actor"": ""place_curious_01"",
                    ""command"": { ""update_need"": [{ ""key"": ""phantom_need"", ""delta"": 10.0 }] }
                }]
            }
        }";

        [Test, Description("ValidateJSON reads a real given JSON string the same way Validate reads a Scenario object")]
        public void ValidateJSON_GoodJson_MatchesValidate() {
            var scenario = Newtonsoft.Json.JsonConvert.DeserializeObject<Scenario>(MINIMAL_SCENARIO_JSON)!;
            var known = new Dictionary<string, IReadOnlyCollection<string>> {
                ["place_curious_01"] = new[] { "curiosity" }
            };

            var from_object = Validator.Validate(scenario: scenario, known_needs: known);
            var from_json = Validator.ValidateJSON(json: MINIMAL_SCENARIO_JSON, known_needs: known);

            Assert.That(from_json.Select(r => r.RuleID), Is.EqualTo(from_object.Select(r => r.RuleID)));
        }

        [Test, Description("ValidateJSON on broken JSON throws, rather than swallowing the fault silently")]
        public void ValidateJSON_BrokenJson_Throws() {
            Assert.Catch<Newtonsoft.Json.JsonException>(code: () =>
                Validator.ValidateJSON(json: "{ not real json at all"));
        }

        [Test, Description("ValidateJSON on JSON that deserializes to null throws a clear, given fault")]
        public void ValidateJSON_NullScenario_Throws() {
            Assert.Throws<InvalidOperationException>(code: () =>
                Validator.ValidateJSON(json: "null"));
        }
    }
}
