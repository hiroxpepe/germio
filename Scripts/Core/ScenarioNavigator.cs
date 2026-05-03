// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Pure-function navigator over the scenario node tree.
    /// 
    /// Introduced in Phase 5.8 v2 fix6 hotfix9 to support the "Unity Scene is the
    /// single source of truth for current_node" principle.
    /// 
    /// On scene start, GameSystem.initializeStateCoroutine resolves the current
    /// Unity Scene name (via SceneManager.GetActiveScene().name) to a node id by
    /// calling FindNodeIdBySceneName, then sets scenario.initial_state.current_node
    /// to that value. The persisted snapshot's current_node field is intentionally
    /// ignored — what Unity is showing is the truth.
    /// 
    /// This class has no Unity dependencies and is fully unit-testable from
    /// dotnet test (see tests/IntegrationTests/Scripts/Core/ScenarioNavigatorTests.cs).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class ScenarioNavigator {
#nullable enable

        /// <summary>
        /// Returns the id of the node whose <c>scene</c> field equals
        /// <paramref name="scene_name"/>, or null if no such node exists in the
        /// subtree rooted at <paramref name="root"/>.
        /// 
        /// Searches the entire subtree depth-first; if multiple nodes share the
        /// same <c>scene</c> value, the first one encountered (in tree order) is
        /// returned. Authoring should keep <c>scene</c> values unique per node.
        /// </summary>
        /// <param name="root">Root of the node tree (typically scenario.root).</param>
        /// <param name="scene_name">Unity Scene name as returned by SceneManager.GetActiveScene().name.</param>
        /// <returns>Matching node id, or null if not found.</returns>
        public static string? FindNodeIdBySceneName(Node? root, string? scene_name) {
            if (root == null || string.IsNullOrEmpty(value: scene_name)) {
                return null;
            }
            if (root.scene == scene_name) {
                return root.id;
            }
            if (root.children != null) {
                foreach (Node child in root.children) {
                    string? hit = FindNodeIdBySceneName(root: child, scene_name: scene_name);
                    if (hit != null) {
                        return hit;
                    }
                }
            }
            return null;
        }
    }
}