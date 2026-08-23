// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for Snapshot class (Phase 5.8 refactor).
    /// Verifies the combined static-dynamic data structure.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class SnapshotTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Default values

        [Test, Description("Snapshot.state is initialized with default State")]
        public void Snapshot_State_IsInitialized() {
            Snapshot snapshot = new Snapshot();
            Assert.That(snapshot.state, Is.Not.Null);
            Assert.That(snapshot.state.flags, Is.Not.Null);
        }

        [Test, Description("Snapshot.history is initialized with default History")]
        public void Snapshot_History_IsInitialized() {
            Snapshot snapshot = new Snapshot();
            Assert.That(snapshot.history, Is.Not.Null);
            Assert.That(snapshot.history.entries, Is.Not.Null);
        }

        ///////////////////////////////////////////////////////////////////////
        // Snapshot represents in-game state

        [Test, Description("Snapshot contains both static and dynamic data")]
        public void Snapshot_CombinesStateAndHistory() {
            Snapshot snapshot = new Snapshot();
            snapshot.state.current_node = "level1";
            snapshot.state.flags["tutorial"] = true;
            
            snapshot.history.entries.Add(item: new HistoryEntry {
                timestamp = 0,
                target_id = "level1",
                kind = "enter"
            });

            Assert.That(snapshot.state.current_node, Is.EqualTo("level1"));
            Assert.That(snapshot.history.entries, Has.Count.GreaterThan(0));
        }

        ///////////////////////////////////////////////////////////////////////
        // JSON round-trip

        [Test, Description("Snapshot serializes to JSON correctly")]
        public void Snapshot_Serialize_IncludesStateAndHistory() {
            Snapshot snapshot = new Snapshot();
            snapshot.state.current_node = "level1";
            snapshot.history.entries.Add(item: new HistoryEntry {
                timestamp = 100,
                target_id = "level1"
            });

            string json = JsonConvert.SerializeObject(value: snapshot);
            Assert.That(json, Does.Contain("\"state\""));
            Assert.That(json, Does.Contain("\"history\""));
            Assert.That(json, Does.Contain("\"current_node\":\"level1\""));
        }

        [Test, Description("Snapshot deserializes from JSON correctly")]
        public void Snapshot_Deserialize_RestoresStateAndHistory() {
            string json = @"{""state"":{""current_node"":""level1"",""flags"":{},""counters"":{},""inventory"":{},""current_team"":"""",""persistence"":{}},""history"":{""entries"":[{""timestamp"":0,""target_id"":""level1"",""kind"":""""}],""max_entries"":1000}}";
            Snapshot? snapshot = JsonConvert.DeserializeObject<Snapshot>(value: json);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.state.current_node, Is.EqualTo("level1"));
            Assert.That(snapshot.history.entries, Has.Count.EqualTo(1));
            Assert.That(snapshot.history.entries[index: 0].target_id, Is.EqualTo("level1"));
        }

        ///////////////////////////////////////////////////////////////////////
        // Property count enforcement

        [Test, Description("Snapshot has exactly 3 properties")]
        public void Snapshot_PropertyCount_IsExactlyTwo() {
            var props = typeof(Snapshot).GetProperties();
            Assert.That(props.Length, Is.EqualTo(3),
                "Snapshot must have exactly 3 properties: schema_version, state, history.");
        }
    }
}