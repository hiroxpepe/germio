// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for Executor.
    /// Verifies that each Command variant correctly mutates state via Store.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ExecutorTests {
#nullable enable
        Store _store = null!;

        [SetUp]
        public void SetUp() {
            var root = new Scenario();
            root.initial_state.counters["score"] = 50f;
            root.initial_state.flags["gate_open"] = false;
            root.initial_state.inventory["item_key"] = 2;
            _store = new Store(root);
        }

        // -----------------------------------------------------------------------------------------
        // set_flag

        [Test]
        public void Execute_SetFlag_SetsValue() {
            Executor.Execute(command: new Command {
                set_flag = new SetFlag { key = "gate_open", value = true }
            }, store: _store);
            Assert.That(_store.Scenario.initial_state.flags["gate_open"], Is.True);
        }

        [Test]
        public void Execute_SetFlag_MarksStoreDirty() {
            Executor.Execute(command: new Command {
                set_flag = new SetFlag { key = "gate_open", value = true }
            }, store: _store);
            Assert.That(_store.IsDirty, Is.True);
        }

        // -----------------------------------------------------------------------------------------
        // update_counter

        [Test]
        public void Execute_UpdateCounter_Add() {
            Executor.Execute(command: new Command {
                update_counter = new UpdateCounter { key = "score", delta = 100f, op = CounterOp.Add }
            }, store: _store);
            Assert.That(_store.Scenario.initial_state.counters["score"], Is.EqualTo(150f));
        }

        [Test]
        public void Execute_UpdateCounter_Sub() {
            Executor.Execute(command: new Command {
                update_counter = new UpdateCounter { key = "score", delta = 30f, op = CounterOp.Sub }
            }, store: _store);
            Assert.That(_store.Scenario.initial_state.counters["score"], Is.EqualTo(20f));
        }

        [Test]
        public void Execute_UpdateCounter_Set() {
            Executor.Execute(command: new Command {
                update_counter = new UpdateCounter { key = "score", delta = 999f, op = CounterOp.Set }
            }, store: _store);
            Assert.That(_store.Scenario.initial_state.counters["score"], Is.EqualTo(999f));
        }

        [Test]
        public void Execute_UpdateCounter_MissingKey_TreatsAsZero() {
            Executor.Execute(command: new Command {
                update_counter = new UpdateCounter { key = "new_counter", delta = 10f, op = CounterOp.Add }
            }, store: _store);
            Assert.That(_store.Scenario.initial_state.counters["new_counter"], Is.EqualTo(10f));
        }

        // -----------------------------------------------------------------------------------------
        // update_inventory

        [Test]
        public void Execute_UpdateInventory_Add() {
            Executor.Execute(command: new Command {
                update_inventory = new UpdateInventory { key = "item_key", delta = 3 }
            }, store: _store);
            Assert.That(_store.Scenario.initial_state.inventory["item_key"], Is.EqualTo(5));
        }

        [Test]
        public void Execute_UpdateInventory_RemovesKeyWhenZero() {
            Executor.Execute(command: new Command {
                update_inventory = new UpdateInventory { key = "item_key", delta = -2 }
            }, store: _store);
            Assert.That(_store.Scenario.initial_state.inventory.ContainsKey("item_key"), Is.False);
        }

        [Test]
        public void Execute_UpdateInventory_RemovesKeyWhenBelowZero() {
            Executor.Execute(command: new Command {
                update_inventory = new UpdateInventory { key = "item_key", delta = -100 }
            }, store: _store);
            Assert.That(_store.Scenario.initial_state.inventory.ContainsKey("item_key"), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // request_transition

        [Test]
        public void Execute_RequestTransition_RaisesEvent() {
            string? received = null;
            _store.TransitionRequested += id => received = id;

            Executor.Execute(command: new Command {
                request_transition = "level_02"
            }, store: _store);

            Assert.That(received, Is.EqualTo("level_02"));
        }

        // -----------------------------------------------------------------------------------------
        // record_event

        [Test, Description("Executor_RecordEvent_AppendsToHistory")]
        public void Executor_RecordEvent_AppendsToHistory() {
            var snapshot = new Snapshot { state = _store.Scenario.initial_state };
            _store.SetSnapshot(snapshot: snapshot);

            Executor.Execute(command: new Command {
                record_event = new RecordEvent { kind = "boss_defeated", target_id = "boss_01" }
            }, store: _store);

            Assert.That(snapshot.history.entries.Count, Is.EqualTo(1));
            Assert.That(snapshot.history.entries[0].kind, Is.EqualTo("boss_defeated"));
            Assert.That(snapshot.history.entries[0].target_id, Is.EqualTo("boss_01"));
        }

        [Test, Description("Executor_RecordEvent_PreservesOtherCommands")]
        public void Executor_RecordEvent_PreservesOtherCommands() {
            var snapshot = new Snapshot { state = _store.Scenario.initial_state };
            _store.SetSnapshot(snapshot: snapshot);

            Executor.Execute(command: new Command {
                set_flag = new SetFlag { key = "test_flag", value = true },
                record_event = new RecordEvent { kind = "event_test", target_id = "test" }
            }, store: _store);

            Assert.That(_store.Scenario.initial_state.flags["test_flag"], Is.True);
            Assert.That(snapshot.history.entries.Count, Is.EqualTo(1));
            Assert.That(snapshot.history.entries[0].kind, Is.EqualTo("event_test"));
        }
    }
}