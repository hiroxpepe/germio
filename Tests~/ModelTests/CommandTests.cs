// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for Command-related classes (Phase 5.8 refactor).
    /// Verifies rule actions and state mutations.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class CommandTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // SetFlag

        [Test, Description("SetFlag command has key and value")]
        public void SetFlag_Properties_AreSet() {
            SetFlag cmd = new SetFlag();
            cmd.key = "tutorial";
            cmd.value = true;

            Assert.That(cmd.key, Is.EqualTo("tutorial"));
            Assert.That(cmd.value, Is.True);
        }

        [Test, Description("SetFlag serializes to JSON correctly")]
        public void SetFlag_Serialize_IncludesAllFields() {
            SetFlag cmd = new SetFlag();
            cmd.key = "tutorial";
            cmd.value = true;

            string json = JsonConvert.SerializeObject(value: cmd);
            Assert.That(json, Does.Contain("\"key\":\"tutorial\""));
            Assert.That(json, Does.Contain("\"value\":true"));
        }

        [Test, Description("SetFlag deserializes from JSON correctly")]
        public void SetFlag_Deserialize_RestoresFields() {
            string json = @"{""key"":""tutorial"",""value"":true}";
            SetFlag? cmd = JsonConvert.DeserializeObject<SetFlag>(value: json);
            Assert.That(cmd, Is.Not.Null);
            Assert.That(cmd!.key, Is.EqualTo("tutorial"));
            Assert.That(cmd.value, Is.True);
        }

        ///////////////////////////////////////////////////////////////////////
        // UpdateCounter

        [Test, Description("UpdateCounter command has key, delta and op")]
        public void UpdateCounter_Properties_AreSet() {
            UpdateCounter cmd = new UpdateCounter();
            cmd.key = "score";
            cmd.op = CounterOp.Add;
            cmd.delta = 10;

            Assert.That(cmd.key, Is.EqualTo("score"));
            Assert.That(cmd.op, Is.EqualTo(CounterOp.Add));
            Assert.That(cmd.delta, Is.EqualTo(10));
        }

        [Test, Description("UpdateCounter serializes to JSON correctly")]
        public void UpdateCounter_Serialize_IncludesAllFields() {
            UpdateCounter cmd = new UpdateCounter();
            cmd.key = "score";
            cmd.op = CounterOp.Add;
            cmd.delta = 100;

            string json = JsonConvert.SerializeObject(value: cmd);
            Assert.That(json, Does.Contain("\"key\":\"score\""));
            Assert.That(json, Does.Contain("\"op\":\"Add\""));
            Assert.That(json, Does.Contain("\"delta\":100"));
        }

        [Test, Description("UpdateCounter deserializes from JSON correctly")]
        public void UpdateCounter_Deserialize_RestoresFields() {
            string json = @"{""key"":""score"",""op"":""Add"",""delta"":100}";
            UpdateCounter? cmd = JsonConvert.DeserializeObject<UpdateCounter>(value: json);
            Assert.That(cmd, Is.Not.Null);
            Assert.That(cmd!.key, Is.EqualTo("score"));
            Assert.That(cmd.op, Is.EqualTo(CounterOp.Add));
            Assert.That(cmd.delta, Is.EqualTo(100));
        }

        ///////////////////////////////////////////////////////////////////////
        // UpdateInventory

        [Test, Description("UpdateInventory command has key and delta")]
        public void UpdateInventory_Properties_AreSet() {
            UpdateInventory cmd = new UpdateInventory();
            cmd.key = "coin";
            cmd.delta = 5;

            Assert.That(cmd.key, Is.EqualTo("coin"));
            Assert.That(cmd.delta, Is.EqualTo(5));
        }

        [Test, Description("UpdateInventory serializes to JSON correctly")]
        public void UpdateInventory_Serialize_IncludesAllFields() {
            UpdateInventory cmd = new UpdateInventory();
            cmd.key = "coin";
            cmd.delta = 5;

            string json = JsonConvert.SerializeObject(value: cmd);
            Assert.That(json, Does.Contain("\"key\":\"coin\""));
            Assert.That(json, Does.Contain("\"delta\":5"));
        }

        [Test, Description("UpdateInventory deserializes from JSON correctly")]
        public void UpdateInventory_Deserialize_RestoresFields() {
            string json = @"{""key"":""coin"",""delta"":5}";
            UpdateInventory? cmd = JsonConvert.DeserializeObject<UpdateInventory>(value: json);
            Assert.That(cmd, Is.Not.Null);
            Assert.That(cmd!.key, Is.EqualTo("coin"));
            Assert.That(cmd.delta, Is.EqualTo(5));
        }

        ///////////////////////////////////////////////////////////////////////
        // SetPersistence

        [Test, Description("SetPersistence command has key and value")]
        public void SetPersistence_Properties_AreSet() {
            SetPersistence cmd = new SetPersistence();
            cmd.key = "last_visited";
            cmd.value = "level2";

            Assert.That(cmd.key, Is.EqualTo("last_visited"));
            Assert.That(cmd.value, Is.EqualTo("level2"));
        }

        [Test, Description("SetPersistence serializes to JSON correctly")]
        public void SetPersistence_Serialize_IncludesAllFields() {
            SetPersistence cmd = new SetPersistence();
            cmd.key = "last_visited";
            cmd.value = "level2";

            string json = JsonConvert.SerializeObject(value: cmd);
            Assert.That(json, Does.Contain("\"key\":\"last_visited\""));
            Assert.That(json, Does.Contain("\"value\":\"level2\""));
        }

        [Test, Description("SetPersistence deserializes from JSON correctly")]
        public void SetPersistence_Deserialize_RestoresFields() {
            string json = @"{""key"":""last_visited"",""value"":""level2""}";
            SetPersistence? cmd = JsonConvert.DeserializeObject<SetPersistence>(value: json);
            Assert.That(cmd, Is.Not.Null);
            Assert.That(cmd!.key, Is.EqualTo("last_visited"));
            Assert.That(cmd.value, Is.EqualTo("level2"));
        }

        ///////////////////////////////////////////////////////////////////////
        // RecordEvent

        [Test, Description("RecordEvent command has kind and target_id")]
        public void RecordEvent_Properties_AreSet() {
            RecordEvent cmd = new RecordEvent();
            cmd.kind = "level_complete";
            cmd.target_id = "level1";

            Assert.That(cmd.kind, Is.EqualTo("level_complete"));
            Assert.That(cmd.target_id, Is.EqualTo("level1"));
        }

        [Test, Description("RecordEvent serializes to JSON correctly")]
        public void RecordEvent_Serialize_IncludesAllFields() {
            RecordEvent cmd = new RecordEvent();
            cmd.kind = "level_complete";
            cmd.target_id = "level1";

            string json = JsonConvert.SerializeObject(value: cmd);
            Assert.That(json, Does.Contain("\"kind\":\"level_complete\""));
            Assert.That(json, Does.Contain("\"target_id\":\"level1\""));
        }

        [Test, Description("RecordEvent deserializes from JSON correctly")]
        public void RecordEvent_Deserialize_RestoresFields() {
            string json = @"{""kind"":""level_complete"",""target_id"":""level1""}";
            RecordEvent? cmd = JsonConvert.DeserializeObject<RecordEvent>(value: json);
            Assert.That(cmd, Is.Not.Null);
            Assert.That(cmd!.kind, Is.EqualTo("level_complete"));
            Assert.That(cmd.target_id, Is.EqualTo("level1"));
        }

        ///////////////////////////////////////////////////////////////////////
        // Command base class

        [Test, Description("Command subclasses can be serialized polymorphically")]
        public void Command_Polymorphic_SerializationWorks() {
            List<Command> commands = new List<Command>();
            
            Command flag_cmd = new Command { set_flag = new SetFlag { key = "test", value = true } };
            Command counter_cmd = new Command { update_counter = new UpdateCounter { key = "score", op = CounterOp.Add, delta = 10 } };
            
            commands.Add(item: flag_cmd);
            commands.Add(item: counter_cmd);

            string json = JsonConvert.SerializeObject(value: commands);
            Assert.That(json, Does.Contain("\"key\""));
            Assert.That(json, Does.Contain("\"key\""));
        }
    }
}