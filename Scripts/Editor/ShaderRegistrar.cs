// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Germio.Editor {

    /// <author>h.adachi (STUDIO MeowToon)</author>
    /// <summary>
    /// Keeps every shader under this package's own Shaders/ folder in
    /// GraphicsSettings' own Always Included Shaders list, checked on
    /// every editor load. A shader reached only through UsePass (an
    /// OutLine shader borrowing another shader's own pass by name) is
    /// never followed by Unity's own build-time shader stripping, so
    /// leaving it out turns a real material pink in a built player —
    /// this closes that gap for every shader this package ever holds,
    /// new ones included, with no manual GraphicsSettings edit again.
    /// </summary>
    [InitializeOnLoad]
    public static class ShaderRegistrar {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Const [nouns]

        const string SHADER_SEARCH_FOLDER = "Packages/com.meowtoon.germio/Shaders";

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // static Constructor

        static ShaderRegistrar() {
            registerAllShaders();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        /// <summary>
        /// Finds every real Shader asset under this package's own
        /// Shaders/ folder, and adds any not yet held in
        /// GraphicsSettings' own Always Included Shaders list.
        /// </summary>
        static void registerAllShaders() {
            var found_shaders = AssetDatabase.FindAssets(filter: "t:Shader", searchInFolders: new[] { SHADER_SEARCH_FOLDER })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(shader => shader != null)
                .ToList();
            if (found_shaders.Count == 0) { return; }

            var graphics_settings = GraphicsSettings.GetGraphicsSettings();
            var serialized = new SerializedObject(graphics_settings);
            var always_included = serialized.FindProperty(propertyPath: "m_AlwaysIncludedShaders");

            var already_held = new HashSet<Shader>();
            for (var i = 0; i < always_included.arraySize; i++) {
                var held = always_included.GetArrayElementAtIndex(index: i).objectReferenceValue as Shader;
                if (held != null) { already_held.Add(held); }
            }

            var added_any = false;
            foreach (var shader in found_shaders) {
                if (already_held.Contains(shader)) { continue; }
                var new_index = always_included.arraySize;
                always_included.InsertArrayElementAtIndex(index: new_index);
                always_included.GetArrayElementAtIndex(index: new_index).objectReferenceValue = shader;
                added_any = true;
            }

            if (!added_any) { return; }
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log(message: $"[Germio] {found_shaders.Count} own shader(s) checked against Always Included Shaders.");
        }
    }
}
#endif
