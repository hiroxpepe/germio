// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Germio.Schema;

namespace Germio.Editor {

    /// <author>h.adachi (STUDIO MeowToon)</author>
    /// <summary>
    /// Unity Editor menu: Tools > Germio > Export Schema.
    /// Copies the germio JSON Schema to the system clipboard for LLM prompt injection.
    /// </summary>
    public static class SchemaExportMenu {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        [MenuItem("Tools/Germio/Export Schema to Clipboard")]
        public static void ExportSchemaToClipboard() {
            string schema_dir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", "schemas"));
            if (!Directory.Exists(schema_dir)) {
                Debug.LogWarning($"[Germio] Schema directory not found: {schema_dir}");
                return;
            }
            string json = SchemaExporter.GetSchemaJson(schema_dir: schema_dir);
            GUIUtility.systemCopyBuffer = json;
            Debug.Log($"[Germio] Schema copied to clipboard ({json.Length} chars). Paste into your LLM prompt.");
        }
    }
}
#endif
