// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json.Linq;

using Germio.Schema;

namespace Germio.Tests.Schema {
    /// <summary>
    /// Unit tests for the exported schema knowing the three new words
    /// (germio TASK-054).
    ///
    /// The schema is what a writer — or an LLM — reads to know what a germio.json
    /// may hold. Left out, nothing outside would know a deed may be written at
    /// all.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class SchemaDeedTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static JObject exportedSchema() {
            return JObject.Parse(json: SchemaExporter.Export());
        }

        static JObject? definitionOf(JObject schema, string type_name) {
            return schema["$defs"]?[type_name] as JObject;
        }

        static bool holdsProperty(JObject? definition, string property_name) {
            return definition?["properties"]?[property_name] != null;
        }

        ///////////////////////////////////////////////////////////////////////
        // actor, on a Rule

        [Test, Description("The schema knows a Rule may name an actor")]
        public void Schema_Rule_KnowsActor() {
            var schema = exportedSchema();

            Assert.That(holdsProperty(definition: definitionOf(schema: schema, type_name: "Rule"),
                property_name: "actor"), Is.True);
        }

        [Test, Description("The schema still knows every word a Rule held before")]
        public void Schema_Rule_StillKnowsTheOldWords() {
            var schema = exportedSchema();
            var rule = definitionOf(schema: schema, type_name: "Rule");

            foreach (string word in new[] { "id", "trigger", "condition", "command", "once" }) {
                Assert.That(holdsProperty(definition: rule, property_name: word), Is.True,
                    $"'{word}' stood before, and must stand still.");
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // update_need and request_deed, on a Command

        [Test, Description("The schema knows a Command may hold an update_need")]
        public void Schema_Command_KnowsUpdateNeed() {
            var schema = exportedSchema();

            Assert.That(holdsProperty(definition: definitionOf(schema: schema, type_name: "Command"),
                property_name: "update_need"), Is.True);
        }

        [Test, Description("The schema knows a Command may hold a request_deed")]
        public void Schema_Command_KnowsRequestDeed() {
            var schema = exportedSchema();

            Assert.That(holdsProperty(definition: definitionOf(schema: schema, type_name: "Command"),
                property_name: "request_deed"), Is.True);
        }

        [Test, Description("The schema still knows every command a Command held before")]
        public void Schema_Command_StillKnowsTheOldWords() {
            var schema = exportedSchema();
            var command = definitionOf(schema: schema, type_name: "Command");

            foreach (string word in new[] { "set_flag", "update_counter", "update_inventory",
                                            "request_transition", "request_notify",
                                            "set_persistence", "record_event" }) {
                Assert.That(holdsProperty(definition: command, property_name: word), Is.True,
                    $"'{word}' stood before, and must stand still.");
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // The three new types, each with all its own parts

        [Test, Description("The schema knows an UpdateNeed, with a key and a delta")]
        public void Schema_KnowsUpdateNeedType() {
            var schema = exportedSchema();
            var need = definitionOf(schema: schema, type_name: "UpdateNeed");

            Assert.That(need, Is.Not.Null);
            Assert.That(holdsProperty(definition: need, property_name: "key"), Is.True);
            Assert.That(holdsProperty(definition: need, property_name: "delta"), Is.True);
        }

        [Test, Description("The schema knows a RequestDeed, with every one of its parts")]
        public void Schema_KnowsRequestDeedType() {
            var schema = exportedSchema();
            var deed = definitionOf(schema: schema, type_name: "RequestDeed");

            Assert.That(deed, Is.Not.Null);
            foreach (string part in new[] { "target", "condition", "motion", "act", "until", "command" }) {
                Assert.That(holdsProperty(definition: deed, property_name: part), Is.True,
                    $"a deed holds '{part}'.");
            }
        }

        [Test, Description("The schema knows a Target, with a kind, a reach and a spread")]
        public void Schema_KnowsTargetType() {
            var schema = exportedSchema();
            var target = definitionOf(schema: schema, type_name: "Target");

            Assert.That(target, Is.Not.Null);
            foreach (string part in new[] { "kind", "reach", "spread" }) {
                Assert.That(holdsProperty(definition: target, property_name: part), Is.True);
            }
        }

        [Test, Description("The schema knows an Until, with all four of its ways")]
        public void Schema_KnowsUntilType() {
            var schema = exportedSchema();
            var until = definitionOf(schema: schema, type_name: "Until");

            Assert.That(until, Is.Not.Null);
            foreach (string way in new[] { "near", "meets", "elapsed", "while" }) {
                Assert.That(holdsProperty(definition: until, property_name: way), Is.True,
                    $"an until may be written with '{way}'.");
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // update_need is a list, never one alone

        [Test, Description("The schema says an update_need is a list")]
        public void Schema_UpdateNeed_IsAList() {
            var schema = exportedSchema();
            var command = definitionOf(schema: schema, type_name: "Command");
            var update_need = command?["properties"]?["update_need"];

            Assert.That(update_need, Is.Not.Null);
            string as_text = update_need!.ToString();
            Assert.That(as_text, Does.Contain("array").IgnoreCase,
                "A list even holding one: a single arrival may quiet more than one want.");
        }
    }
}
