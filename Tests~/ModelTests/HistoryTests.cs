// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for History and HistoryEntry classes (Phase 5.8 refactor).
    /// Verifies event logging and historical tracking.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class HistoryTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // HistoryEntry defaults

        [Test, Description("HistoryEntry.timestamp defaults to 0")]
        public void HistoryEntry_Timestamp_DefaultsToZero() {
            HistoryEntry entry = new HistoryEntry();
            Assert.That(entry.timestamp, Is.EqualTo(0));
        }

        [Test, Description("HistoryEntry.target_id defaults to empty string")]
        public void HistoryEntry_TargetId_DefaultsToEmpty() {
            HistoryEntry entry = new HistoryEntry();
            Assert.That(entry.target_id, Is.EqualTo(string.Empty));
        }

        [Test, Description("HistoryEntry.kind defaults to empty string")]
        public void HistoryEntry_Kind_DefaultsToEmpty() {
            HistoryEntry entry = new HistoryEntry();
            Assert.That(entry.kind, Is.EqualTo(string.Empty));
        }

        ///////////////////////////////////////////////////////////////////////
        // History defaults

        [Test, Description("History.entries defaults to empty list")]
        public void History_Entries_DefaultsToEmptyList() {
            History history = new History();
            Assert.That(history.entries, Is.Not.Null);
            Assert.That(history.entries, Has.Count.EqualTo(0));
        }

        ///////////////////////////////////////////////////////////////////////
        // History entry recording

        [Test, Description("HistoryEntry records node transitions")]
        public void HistoryEntry_RecordsNodeTransition() {
            HistoryEntry entry = new HistoryEntry();
            entry.timestamp = 100;
            entry.target_id = "level1";
            entry.kind = "enter";

            Assert.That(entry.timestamp, Is.EqualTo(100));
            Assert.That(entry.target_id, Is.EqualTo("level1"));
            Assert.That(entry.kind, Is.EqualTo("enter"));
        }

        [Test, Description("HistoryEntry has minimal fields")]
        public void HistoryEntry_FieldsAreMinimal() {
            HistoryEntry entry = new HistoryEntry();
            // Just verify the entry can be created and the basic properties exist
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.timestamp, Is.EqualTo(0));
            Assert.That(entry.target_id, Is.EqualTo(string.Empty));
            Assert.That(entry.kind, Is.EqualTo(string.Empty));
        }

        [Test, Description("History accumulates multiple entries")]
        public void History_Entries_CanAccumulateEvents() {
            History history = new History();
            
            history.entries.Add(item: new HistoryEntry {
                timestamp = 0,
                target_id = "title",
                kind = "enter"
            });

            history.entries.Add(item: new HistoryEntry {
                timestamp = 100,
                target_id = "level1",
                kind = "enter"
            });

            Assert.That(history.entries, Has.Count.EqualTo(2));
            Assert.That(history.entries[index: 0].target_id, Is.EqualTo("title"));
            Assert.That(history.entries[index: 1].target_id, Is.EqualTo("level1"));
        }

        ///////////////////////////////////////////////////////////////////////
        // JSON round-trip

        [Test, Description("HistoryEntry serializes to JSON with snake_case")]
        public void HistoryEntry_Serialize_UsesSnakeCase() {
            HistoryEntry entry = new HistoryEntry();
            entry.timestamp = 50;
            entry.target_id = "level1";
            entry.kind = "enter";

            string json = JsonConvert.SerializeObject(value: entry);
            Assert.That(json, Does.Contain("\"timestamp\":50"));
            Assert.That(json, Does.Contain("\"target_id\":\"level1\""));
            Assert.That(json, Does.Contain("\"kind\":\"enter\""));
        }

        [Test, Description("HistoryEntry deserializes from JSON correctly")]
        public void HistoryEntry_Deserialize_RestoresAllFields() {
            string json = @"{""timestamp"":50,""target_id"":""level1"",""kind"":""enter""}";
            HistoryEntry? entry = JsonConvert.DeserializeObject<HistoryEntry>(value: json);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.timestamp, Is.EqualTo(50));
            Assert.That(entry.target_id, Is.EqualTo("level1"));
            Assert.That(entry.kind, Is.EqualTo("enter"));
        }

        [Test, Description("History serializes to JSON correctly")]
        public void History_Serialize_IncludesAllEntries() {
            History history = new History();
            history.entries.Add(item: new HistoryEntry {
                timestamp = 0,
                target_id = "title"
            });

            string json = JsonConvert.SerializeObject(value: history);
            Assert.That(json, Does.Contain("\"entries\""));
            Assert.That(json, Does.Contain("\"target_id\":\"title\""));
        }

        [Test, Description("History deserializes from JSON correctly")]
        public void History_Deserialize_RestoresEntries() {
            string json = @"{""entries"":[{""timestamp"":0,""target_id"":""title"",""kind"":""""},{""timestamp"":100,""target_id"":""level1"",""kind"":""""}],""max_entries"":1000}";
            History? history = JsonConvert.DeserializeObject<History>(value: json);
            Assert.That(history, Is.Not.Null);
            Assert.That(history!.entries, Has.Count.EqualTo(2));
            Assert.That(history.entries[index: 0].target_id, Is.EqualTo("title"));
            Assert.That(history.entries[index: 1].target_id, Is.EqualTo("level1"));
        }

        ///////////////////////////////////////////////////////////////////////
        // Property count enforcement

        [Test, Description("HistoryEntry has exactly 3 properties")]
        public void HistoryEntry_PropertyCount_IsExactlyThree() {
            var props = typeof(HistoryEntry).GetProperties();
            Assert.That(props.Length, Is.EqualTo(3),
                "HistoryEntry must have exactly 3 properties: timestamp, target_id, kind.");
        }

        [Test, Description("History has exactly 2 properties")]
        public void History_PropertyCount_IsExactlyTwo() {
            var props = typeof(History).GetProperties();
            Assert.That(props.Length, Is.EqualTo(2),
                "History must have exactly 2 properties: entries, max_entries.");
        }
    }
}