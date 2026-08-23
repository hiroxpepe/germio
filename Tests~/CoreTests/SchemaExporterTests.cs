// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.IO;
using Newtonsoft.Json.Linq;
using Germio.Schema;

namespace Germio.Tests.Schema {

    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class SchemaExporterTests {
        #nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        static readonly string SCHEMA_DIR = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../../schemas"));

        string? _schema_json;
        JObject? _schema_obj;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // SetUp — uses NJsonSchema dynamic export

        [SetUp]
        public void SetUp() {
            _schema_json = SchemaExporter.Export();
            _schema_obj  = JObject.Parse(_schema_json);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: SchemaExporter constants and static-file API

        [Test]
        public void SchemaExporter_SchemaFileName_IsCorrect() {
            Assert.That(SchemaExporter.SCHEMA_FILE_NAME, Is.EqualTo("germio.schema.json"));
        }



        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: Export() returns non-empty valid JSON

        [Test]
        public void Export_ReturnsNonEmpty() {
            Assert.That(_schema_json, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Export_ReturnsValidJson() {
            Assert.DoesNotThrow(() => JObject.Parse(_schema_json!));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: Schema structure — $schema keyword

        [Test]
        public void Schema_HasDollarSchema_Draft2020_12() {
            string? schema_ver = _schema_obj!["$schema"]?.ToString();
            Assert.That(schema_ver, Does.Contain("2020-12"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: Schema structure — $defs

        [Test]
        public void Schema_HasAllRequiredDefs() {
            var defs = _schema_obj!["$defs"];
            Assert.That(defs, Is.Not.Null, "$defs is missing from schema");

            // Root type (Scenario) is inlined; all referenced sub-types must appear in $defs.
            // Phase 5.8: World and Level replaced with Node
            // Note: HistoryEntry and History are not included in germio schema (Snapshot schema is internal)
            string[] required = {
                "State", "Node",
                "Next", "Rule", "Command",
                "SetFlag", "UpdateCounter", "UpdateInventory", "CounterOp", "RecordEvent",
                "SetPersistence"
            };

            foreach (string name in required) {
                Assert.That(defs![name], Is.Not.Null, $"$defs is missing type: {name}");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: Schema content — snake_case keys (G17)

        [Test]
        public void Schema_ContainsSnakeCaseKey_CurrentScene() {
            // Phase 5.8: current_scene was replaced with current_node in State
            Assert.That(_schema_json, Does.Contain("\"current_node\""),
                "schema must have current_node (replacement for current_scene)");
        }

        [Test]
        public void Schema_ContainsSnakeCaseKey_FiredRules() {
            // Phase 5.8: fired_rules was removed; rules are now part of Node definition
            // Verify rules exist in schema instead
            Assert.That(_schema_json, Does.Contain("\"rules\""),
                "schema must have rules (replacement for fired_rules)");
        }

        [Test]
        public void Schema_ContainsSnakeCaseKey_SetFlag() {
            Assert.That(_schema_json, Does.Contain("\"set_flag\""),
                "schema must have set_flag (not setFlag)");
        }

        [Test]
        public void Schema_ContainsSnakeCaseKey_UpdateCounter() {
            Assert.That(_schema_json, Does.Contain("\"update_counter\""),
                "schema must have update_counter");
        }

        [Test]
        public void Schema_ContainsSnakeCaseKey_UpdateInventory() {
            Assert.That(_schema_json, Does.Contain("\"update_inventory\""),
                "schema must have update_inventory");
        }

        [Test]
        public void Schema_ContainsSnakeCaseKey_RequestTransition() {
            Assert.That(_schema_json, Does.Contain("\"request_transition\""),
                "schema must have request_transition");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Tests: Schema content — old / camelCase names must NOT appear

        [Test]
        public void Schema_DoesNotContainOldName_DataRoot() {
            Assert.That(_schema_json, Does.Not.Contain("\"DataRoot\""),
                "DataRoot is an internal C# class name and must not appear in schema");
        }

        [Test]
        public void Schema_DoesNotContainOldName_DataEvent() {
            Assert.That(_schema_json, Does.Not.Contain("\"DataEvent\""),
                "DataEvent is an internal C# class name and must not appear in schema");
        }

        [Test]
        public void Schema_DoesNotContainOldFieldName_FiredEvents() {
            Assert.That(_schema_json, Does.Not.Contain("\"firedEvents\""),
                "firedEvents is an old camelCase name replaced by fired_rules");
        }

        [Test]
        public void Schema_DoesNotContainOldFieldName_SetFlag_camelCase() {
            Assert.That(_schema_json, Does.Not.Contain("\"setFlag\""),
                "setFlag is an old camelCase name replaced by set_flag");
        }

        [Test]
        public void Schema_DoesNotContainOldArrayName_Events() {
            // "events" was the old array name inside worlds/levels; now it's "rules"
            Assert.That(_schema_json, Does.Not.Contain("\"events\""),
                "events is the old array name; schema must use rules");
        }

        [Test]
        public void SchemaExporterTests_IsInCorrectNamespace() {
            Assert.That(typeof(SchemaExporterTests).Namespace,
                Is.EqualTo("Germio.Tests.Schema"),
                "G18: test namespace must match folder layer (Germio.Tests.Schema)");
        }

    }
}