// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.IO;
using System.Linq;

using Newtonsoft.Json.Linq;

// NJsonSchema is a dotnet tool dependency; not available in Unity runtime.
// Dynamic schema generation is used in tests and CLI tools only.
#if !UNITY_5_3_OR_NEWER
using NJsonSchema;
using NJsonSchema.Generation;
#endif

using Germio.Model;

namespace Germio.Schema {

    /// <author>h.adachi (STUDIO MeowToon)</author>
    /// <summary>
    /// Provides the germio JSON Schema for LLM prompt injection and tooling.
    /// In non-Unity contexts (tests, dotnet CLI): dynamically generates schema from C# types
    /// using NJsonSchema so the schema always reflects the current model.
    /// In Unity contexts: reads the pre-generated static schema file via GetSchemaJson().
    /// </summary>
    public static class SchemaExporter {
        #nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        /// <summary>The schema file name shared across all export methods.</summary>
        public const string SCHEMA_FILE_NAME = "germio.schema.json";

        const string DRAFT_2020_12_URI = "https://json-schema.org/draft/2020-12/schema";
        const string SCHEMA_ID = "https://germio.dev/schemas/germio.schema.json";

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

#if !UNITY_5_3_OR_NEWER
        /// <summary>
        /// Dynamically generates the germio JSON Schema from the C# model types.
        /// Uses NJsonSchema reflection on <see cref="Scenario"/> and related types.
        /// Output is post-processed to conform to JSON Schema Draft 2020-12 ($defs, $schema URI).
        /// </summary>
        public static string Export() {
            var settings = new SystemTextJsonSchemaGeneratorSettings {
                SchemaType = SchemaType.JsonSchema,
                FlattenInheritanceHierarchy = false,
            };

            var schema = JsonSchema.FromType<Scenario>(settings: settings);
            schema.Title = "Germio Scenario Configuration";
            schema.Id    = SCHEMA_ID;

            string raw_json = schema.ToJson();

            // Post-process: upgrade draft-04 output to Draft 2020-12 conventions.
            var json_obj = JObject.Parse(raw_json);
            normalizeToDraft2020(json_obj: json_obj);
            return json_obj.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Generates the schema and writes it to <paramref name="output_dir"/>/germio.schema.json.
        /// Call this to regenerate the committed schema file after model changes.
        /// </summary>
        public static void SaveToFile(string output_dir) {
            string path = Path.Combine(output_dir, SCHEMA_FILE_NAME);
            File.WriteAllText(path: path, contents: Export());
        }

        // -----------------------------------------------------------------
        // private helpers — Draft 2020-12 normalization

        static void normalizeToDraft2020(JObject json_obj) {
            // Rename "definitions" → "$defs"
            if (json_obj.ContainsKey("definitions")) {
                json_obj["$defs"] = json_obj["definitions"]!;
                json_obj.Remove("definitions");
            }
            // Rewrite all "$ref": "#/definitions/X" → "#/$defs/X"
            fixRefs(token: json_obj);
            // Stamp the Draft 2020-12 URI (overwrite NJsonSchema's draft-04 value)
            json_obj["$schema"] = DRAFT_2020_12_URI;
        }

        static void fixRefs(JToken token) {
            if (token is JObject obj) {
                foreach (var prop in obj.Properties().ToList()) {
                    if (prop.Name == "$ref") {
                        string ref_val = prop.Value.ToString();
                        if (ref_val.StartsWith("#/definitions/")) {
                            prop.Value = new JValue(ref_val.Replace("#/definitions/", "#/$defs/"));
                        }
                    } else {
                        fixRefs(token: prop.Value);
                    }
                }
            } else if (token is JArray arr) {
                foreach (var item in arr) { fixRefs(token: item); }
            }
        }
#endif

        /// <summary>
        /// Reads and returns the germio.schema.json content from the given directory.
        /// Useful in Unity (read the pre-generated committed file from StreamingAssets)
        /// or in tests that want to verify the on-disk committed schema.
        /// </summary>
        public static string GetSchemaJson(string schema_dir) {
            string path = Path.Combine(schema_dir, SCHEMA_FILE_NAME);
            return File.ReadAllText(path);
        }
    }
}

