// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for Scenario class (Phase 5.8 refactor).
    /// Verifies the static-side root data structure.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ScenarioTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Default values

        [Test, Description("Scenario default schema_version is 1")]
        public void Scenario_SchemaVersion_DefaultsToOne() {
            Scenario scenario = new Scenario();
            Assert.That(scenario.schema_version, Is.EqualTo(1));
        }

        [Test, Description("Scenario.initial_state is initialized with default State")]
        public void Scenario_InitialState_IsInitialized() {
            Scenario scenario = new Scenario();
            Assert.That(scenario.initial_state, Is.Not.Null);
            Assert.That(scenario.initial_state.flags, Is.Not.Null);
        }

        [Test, Description("Scenario.root is initialized with default Node")]
        public void Scenario_Root_IsInitialized() {
            Scenario scenario = new Scenario();
            Assert.That(scenario.root, Is.Not.Null);
            Assert.That(scenario.root.id, Is.EqualTo(string.Empty));
        }

        ///////////////////////////////////////////////////////////////////////
        // Anti-Pattern A enforcement

        [Test, Description("Scenario must NOT have a 'state' property (only initial_state)")]
        public void Scenario_NoStateProperty_AntiPatternA() {
            // This test ensures Scenario.state property is NOT added.
            // If the test fails to compile, Scenario.state has been wrongly added.
            var props = typeof(Scenario).GetProperties();
            bool has_state = false;
            foreach (var prop in props) {
                if (prop.Name == "state") {
                    has_state = true;
                    break;
                }
            }
            Assert.That(has_state, Is.False, "Scenario.state property must NOT exist (Anti-Pattern A). Use initial_state.");
        }

        [Test, Description("Scenario must NOT have a 'worlds' property (Phase 5.8 removed)")]
        public void Scenario_NoWorldsProperty() {
            var props = typeof(Scenario).GetProperties();
            bool has_worlds = false;
            foreach (var prop in props) {
                if (prop.Name == "worlds") {
                    has_worlds = true;
                    break;
                }
            }
            Assert.That(has_worlds, Is.False, "Scenario.worlds must NOT exist after Phase 5.8.");
        }

        ///////////////////////////////////////////////////////////////////////
        // JSON round-trip

        [Test, Description("Scenario serializes to JSON with snake_case keys")]
        public void Scenario_Serialize_UsesSnakeCase() {
            Scenario scenario = new Scenario();
            scenario.root.id = "game";
            string json = JsonConvert.SerializeObject(value: scenario);
            Assert.That(json, Does.Contain("\"schema_version\":1"));
            Assert.That(json, Does.Contain("\"initial_state\""));
            Assert.That(json, Does.Contain("\"root\""));
        }

        [Test, Description("Scenario deserializes from JSON correctly")]
        public void Scenario_Deserialize_HandlesAllFields() {
            string json = @"{""schema_version"":1,""initial_state"":{""current_node"":""title""},""root"":{""id"":""game"",""kind"":""world""}}";
            Scenario? scenario = JsonConvert.DeserializeObject<Scenario>(value: json);
            Assert.That(scenario, Is.Not.Null);
            Assert.That(scenario!.schema_version, Is.EqualTo(1));
            Assert.That(scenario.initial_state.current_node, Is.EqualTo("title"));
            Assert.That(scenario.root.id, Is.EqualTo("game"));
        }

        ///////////////////////////////////////////////////////////////////////
        // Property count enforcement

        [Test, Description("Scenario has exactly 3 properties")]
        public void Scenario_PropertyCount_IsExactlyThree() {
            var props = typeof(Scenario).GetProperties();
            Assert.That(props.Length, Is.EqualTo(3),
                "Scenario must have exactly 3 properties: schema_version, initial_state, root.");
        }
    }
}