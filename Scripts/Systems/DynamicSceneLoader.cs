// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;

namespace Germio {
    /// <summary>
    /// Listens for transition requests from the StateManager and loads the corresponding scene.
    /// Uses an injected <see cref="Action{String}"/> for the actual load call so that
    /// the class is fully testable without a Unity runtime.
    ///
    /// Responsibilities:
    ///   1. Resolve the target level's <c>scene</c> name from <see cref="DataRoot"/>.
    ///   2. Update <see cref="DataState.currentScene"/> to the new level ID.
    ///   3. Mark state dirty so the next save persists the updated current scene.
    ///   4. Invoke the injected load delegate (Unity: SceneManager.LoadScene).
    ///
    /// Call <see cref="Dispose"/> to unsubscribe from the StateManager event.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class DynamicSceneLoader : IDisposable {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        readonly StateManager _manager;
        readonly Action<string> _load_scene;
        bool _disposed;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        /// <summary>
        /// Constructs a DynamicSceneLoader and subscribes to <see cref="StateManager.OnTransitionRequested"/>.
        /// </summary>
        /// <param name="manager">The StateManager whose root provides level-to-scene mapping.</param>
        /// <param name="loadScene">
        /// Delegate invoked with the resolved scene name when a transition is requested.
        /// In Unity production code: <c>name =&gt; SceneManager.LoadScene(name)</c>.
        /// In tests: any capture lambda.
        /// </param>
        public DynamicSceneLoader(StateManager manager, Action<string> loadScene) {
            _manager    = manager;
            _load_scene = loadScene;
            _manager.OnTransitionRequested += handleTransition;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Unsubscribes from the StateManager transition event.
        /// Safe to call multiple times.
        /// </summary>
        public void Dispose() {
            if (_disposed) { return; }
            _manager.OnTransitionRequested -= handleTransition;
            _disposed = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Handles the <see cref="StateManager.OnTransitionRequested"/> event.
        /// Looks up the scene name, updates state, and invokes the load delegate.
        /// </summary>
        /// <param name="targetLevelId">The target level ID to transition to.</param>
        void handleTransition(string targetLevelId) {
            string? sceneName = findSceneName(targetLevelId);
            // Guard: skip unknown levels and levels with empty scene names
            if (string.IsNullOrEmpty(sceneName)) { return; }
            _manager.state.currentScene = targetLevelId;
            _manager.MarkDirty();
            _load_scene(sceneName);
        }

        /// <summary>
        /// Searches all worlds in the current DataRoot for a level matching <paramref name="levelId"/>
        /// and returns its <c>scene</c> field.
        /// Uses <see cref="StateManager.root"/> (not a cached reference) so that data loaded
        /// asynchronously after construction is always reflected.
        /// </summary>
        /// <param name="levelId">The level ID to search for.</param>
        /// <returns>The scene name, or null if not found.</returns>
        string? findSceneName(string levelId) {
            foreach (var world in _manager.root.worlds) {
                foreach (var level in world.levels) {
                    if (level.id == levelId) {
                        return string.IsNullOrEmpty(level.scene) ? null : level.scene;
                    }
                }
            }
            return null;
        }
    }
}
