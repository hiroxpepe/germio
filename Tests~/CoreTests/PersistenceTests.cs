// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;
using Germio;

using Newtonsoft.Json;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Tests State.persistence, Command.set_persistence, and Executor handling (P5-T4).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class PersistenceTests {
#nullable enable

        [Test]
        public void State_Persistence_ExistsAndIsEmpty() {
            var state = new State();

            Assert.That(state.persistence, Is.Not.Null);
            Assert.That(state.persistence, Is.InstanceOf<Map<string, string>>());
            Assert.That(state.persistence.Count, Is.EqualTo(0));
        }

        [Test]
        public void Command_SetPersistence_IsNullByDefault() {
            var cmd = new Command();

            Assert.That(cmd.set_persistence, Is.Null,
                "set_persistence must be nullable with default null.");
        }

        [Test]
        public void SetPersistence_HasKeyAndValue() {
            var sp = new SetPersistence { key = "save_slot", value = "slot_1" };

            Assert.That(sp.key,   Is.EqualTo("save_slot"));
            Assert.That(sp.value, Is.EqualTo("slot_1"));
        }

        [Test]
        public void Executor_SetPersistence_WritesToStatePersistence() {
            var store = new Store(scenario: new Scenario());

            Executor.Execute(
                command: new Command {
                    set_persistence = new SetPersistence { key = "save_slot", value = "slot_2" }
                },
                store: store);

            Assert.That(store.Scenario.initial_state.persistence.ContainsKey("save_slot"), Is.True);
            Assert.That(store.Scenario.initial_state.persistence["save_slot"], Is.EqualTo("slot_2"));
        }

        [Test]
        public void Executor_SetPersistence_OverwritesExistingKey() {
            var scenario = new Scenario();
            scenario.initial_state.persistence["save_slot"] = "slot_1";
            var store = new Store(scenario: scenario);

            Executor.Execute(
                command: new Command {
                    set_persistence = new SetPersistence { key = "save_slot", value = "slot_99" }
                },
                store: store);

            Assert.That(store.Scenario.initial_state.persistence["save_slot"], Is.EqualTo("slot_99"));
        }

        [Test]
        public void Executor_SetPersistence_MarksDirty() {
            var store = new Store(scenario: new Scenario());

            Executor.Execute(
                command: new Command {
                    set_persistence = new SetPersistence { key = "k", value = "v" }
                },
                store: store);

            Assert.That(store.IsDirty, Is.True);
        }

        [Test]
        public void State_Persistence_SerializesToJson() {
            var state = new State();
            state.persistence["save_slot"]   = "slot_3";
            state.persistence["player_name"] = "Hirox";

            string json = JsonConvert.SerializeObject(state, Formatting.Indented);

            Assert.That(json, Does.Contain("save_slot"));
            Assert.That(json, Does.Contain("slot_3"));
            Assert.That(json, Does.Contain("player_name"));
        }

        [Test]
        public void State_Persistence_SurvivesRoundTrip() {
            var state = new State();
            state.persistence["coins"] = "100";
            state.persistence["name"]  = "Hirox";

            string json = JsonConvert.SerializeObject(state);
            var restored = JsonConvert.DeserializeObject<State>(json);

            Assert.That(restored!.persistence["coins"], Is.EqualTo("100"));
            Assert.That(restored.persistence["name"],   Is.EqualTo("Hirox"));
        }

        [Test]
        public void Executor_NullSetPersistence_IsNoOp() {
            var store = new Store(scenario: new Scenario());

            Executor.Execute(
                command: new Command { set_persistence = null },
                store: store);

            Assert.That(store.Scenario.initial_state.persistence.Count, Is.EqualTo(0));
            Assert.That(store.IsDirty, Is.False);
        }
    }
}