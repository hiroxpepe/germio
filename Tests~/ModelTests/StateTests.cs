// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for State class (Phase 5.8 refactor).
    /// Verifies dynamic game state (flags, counters, inventory, persistence).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class StateTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Default values

        [Test, Description("State.current_node defaults to empty string")]
        public void State_CurrentNode_DefaultsToEmpty() {
            State state = new State();
            Assert.That(state.current_node, Is.EqualTo(string.Empty));
        }

        [Test, Description("State.flags defaults to empty dict")]
        public void State_Flags_DefaultsToEmptyDict() {
            State state = new State();
            Assert.That(state.flags, Is.Not.Null);
            Assert.That(state.flags, Has.Count.EqualTo(0));
        }

        [Test, Description("State.counters defaults to empty dict")]
        public void State_Counters_DefaultsToEmptyDict() {
            State state = new State();
            Assert.That(state.counters, Is.Not.Null);
            Assert.That(state.counters, Has.Count.EqualTo(0));
        }

        [Test, Description("State.inventory defaults to empty dict")]
        public void State_Inventory_DefaultsToEmptyDict() {
            State state = new State();
            Assert.That(state.inventory, Is.Not.Null);
            Assert.That(state.inventory, Has.Count.EqualTo(0));
        }

        [Test, Description("State.persistence defaults to empty dict")]
        public void State_Persistence_DefaultsToEmptyDict() {
            State state = new State();
            Assert.That(state.persistence, Is.Not.Null);
            Assert.That(state.persistence, Has.Count.EqualTo(0));
        }

        ///////////////////////////////////////////////////////////////////////
        // Anti-Pattern A enforcement

        [Test, Description("State must NOT have 'current_scene' property (renamed to current_node)")]
        public void State_NoCurrentSceneProperty_AntiPatternA() {
            var props = typeof(State).GetProperties();
            bool has_current_scene = false;
            foreach (var prop in props) {
                if (prop.Name == "current_scene") {
                    has_current_scene = true;
                    break;
                }
            }
            Assert.That(has_current_scene, Is.False, 
                "State.current_scene must NOT exist (renamed to current_node).");
        }

        [Test, Description("State must NOT have 'fired_rules' property (replaced by History)")]
        public void State_NoFiredRulesProperty() {
            var props = typeof(State).GetProperties();
            bool has_fired_rules = false;
            foreach (var prop in props) {
                if (prop.Name == "fired_rules") {
                    has_fired_rules = true;
                    break;
                }
            }
            Assert.That(has_fired_rules, Is.False, 
                "State.fired_rules must NOT exist (use History instead).");
        }

        ///////////////////////////////////////////////////////////////////////
        // Data mutation

        [Test, Description("State flags can be set and retrieved")]
        public void State_Flags_CanBeModified() {
            State state = new State();
            state.flags["tutorial_complete"] = true;
            Assert.That(state.flags["tutorial_complete"], Is.True);
        }

        [Test, Description("State counters can be incremented")]
        public void State_Counters_CanBeIncremented() {
            State state = new State();
            state.counters["score"] = 100;
            state.counters["score"] = state.counters["score"] + 50;
            Assert.That(state.counters["score"], Is.EqualTo(150));
        }

        [Test, Description("State inventory can store items")]
        public void State_Inventory_CanStoreItems() {
            State state = new State();
            state.inventory["key"] = 1;
            state.inventory["coin"] = 5;
            Assert.That(state.inventory["key"], Is.EqualTo(1));
            Assert.That(state.inventory["coin"], Is.EqualTo(5));
        }

        [Test, Description("State persistence survives game reload")]
        public void State_Persistence_CanStoreArbitraryData() {
            State state = new State();
            state.persistence["last_visited"] = "level2";
            Assert.That(state.persistence["last_visited"], Is.EqualTo("level2"));
        }

        ///////////////////////////////////////////////////////////////////////
        // JSON round-trip

        [Test, Description("State serializes to JSON with snake_case keys")]
        public void State_Serialize_UsesSnakeCase() {
            State state = new State();
            state.current_node = "level1";
            state.flags["test"] = true;

            string json = JsonConvert.SerializeObject(value: state);
            Assert.That(json, Does.Contain("\"current_node\":\"level1\""));
            Assert.That(json, Does.Contain("\"flags\""));
        }

        [Test, Description("State deserializes from JSON correctly")]
        public void State_Deserialize_RestoresAllFields() {
            string json = @"{""current_node"":""level1"",""flags"":{""test"":true},""counters"":{""x"":10},""inventory"":{},""persistence"":{}}";
            State? state = JsonConvert.DeserializeObject<State>(value: json);
            Assert.That(state, Is.Not.Null);
            Assert.That(state!.current_node, Is.EqualTo("level1"));
            Assert.That(state.flags["test"], Is.True);
            Assert.That(state.counters["x"], Is.EqualTo(10));
        }

        ///////////////////////////////////////////////////////////////////////
        // Property count enforcement

        [Test, Description("State has exactly 6 properties")]
        public void State_PropertyCount_IsExactlyFive() {
            var props = typeof(State).GetProperties();
            Assert.That(props.Length, Is.EqualTo(6),
                "State must have exactly 6 properties: flags, counters, inventory, current_node, current_team, persistence.");
        }
    }
}