// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Germio.Editor {
    /// <summary>
    /// Unity Editor menu: Tools/Germio/Sync Scene Code.
    /// 
    /// Reads <c>Assets/StreamingAssets/germio.json</c> and synchronises
    /// C# scene classes under <c>Assets/Scripts/Scenes/</c> with the node tree.
    /// 
    /// See <c>docs/development_plan_phase_5_19_spec_JP.md</c> for the full specification.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class SceneCodeSyncMenu {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        [MenuItem("Tools/Germio/Sync Scene Code")]
        public static void SyncSceneCode() {
            string germio_json_path = Path.Combine(
                path1: Application.streamingAssetsPath,
                path2: "germio.json");
            string scenes_root = Path.Combine(
                path1: Application.dataPath,
                path2: "Scripts/Scenes");

            if (!File.Exists(path: germio_json_path)) {
                Debug.LogError(message: $"[Germio SceneCodeSync] germio.json not found: {germio_json_path}");
                return;
            }

            var result = SceneCodeSyncer.Sync(
                germio_json_path: germio_json_path,
                scenes_root:      scenes_root);

            // Report errors first.
            foreach (string err in result.errors) {
                Debug.LogError(message: $"[Germio SceneCodeSync] {err}");
            }
            foreach (string warn in result.warnings) {
                Debug.LogWarning(message: $"[Germio SceneCodeSync] {warn}");
            }

            // Summary log.
            Debug.Log(message:
                $"[Germio SceneCodeSync] done — " +
                $"created: {result.created_files.Count}, " +
                $"modified: {result.modified_files.Count}, " +
                $"moved: {result.moved_files.Count}, " +
                $"created_dirs: {result.created_directories.Count}, " +
                $"deleted_dirs: {result.deleted_directories.Count}, " +
                $"warnings: {result.warnings.Count}, " +
                $"errors: {result.errors.Count}");

            // Refresh AssetDatabase so Unity picks up new / renamed / moved files.
            AssetDatabase.Refresh();
        }
    }
}
#endif
