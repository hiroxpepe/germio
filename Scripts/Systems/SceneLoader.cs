// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;

namespace Germio {
    /// <summary>
    /// Listens for transition requests from the Store and loads the corresponding scene.
    /// Uses an injected <see cref="Action{String}"/> for the actual load call so that
    /// the class is fully testable without a Unity runtime.
    ///
    /// Responsibilities:
    ///   1. Resolve the target level's <c>scene</c> name from <see cref="DataRoot"/>.
    ///   2. Update <see cref="DataState.currentScene"/> to the new level ID.
    ///   3. Mark state dirty so the next save persists the updated current scene.
    ///   4. Invoke the injected load delegate (Unity: SceneManager.LoadScene).
    ///
    /// Call <see cref="Dispose"/> to unsubscribe from the Store event.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class SceneLoader : IDisposable {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        readonly Store _store;
        readonly Action<string> _load_scene;
        bool _disposed;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        /// <summary>
        /// Constructs a SceneLoader and subscribes to <see cref="Store.OnTransitionRequested"/>.
        /// </summary>
        /// <param name="store">The Store whose root provides level-to-scene mapping.</param>
        /// <param name="load_scene">
        /// Delegate invoked with the resolved scene name when a transition is requested.
        /// In Unity production code: <c>name =&gt; SceneManager.LoadScene(name)</c>.
        /// In tests: any capture lambda.
        /// </param>
        public SceneLoader(Store store, Action<string> load_scene) {
            _store      = store;
            _load_scene = load_scene;
            _store.OnTransitionRequested += handleTransition;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Unsubscribes from the Store transition event.
        /// Safe to call multiple times.
        /// </summary>
        public void Dispose() {
            if (_disposed) { return; }
            _store.OnTransitionRequested -= handleTransition;
            _disposed = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Handles the <see cref="Store.OnTransitionRequested"/> event.
        /// Looks up the scene name, updates state, and invokes the load delegate.
        /// </summary>
        /// <param name="target_level_id">The target level ID to transition to.</param>
        void handleTransition(string target_level_id) {
            string? scene_name = findSceneName(level_id: target_level_id);
            // Guard: skip unknown levels and levels with empty scene names
            if (string.IsNullOrEmpty(scene_name)) { return; }
            _store.state.currentScene = target_level_id;
            _store.MarkDirty();
            _load_scene(scene_name);
        }

        /// <summary>
        /// Searches all worlds in the current DataRoot for a level matching <paramref name="level_id"/>
        /// and returns its <c>scene</c> field.
        /// Uses <see cref="Store.root"/> (not a cached reference) so that data loaded
        /// asynchronously after construction is always reflected.
        /// </summary>
        /// <param name="level_id">The level ID to search for.</param>
        /// <returns>The scene name, or null if not found.</returns>
        string? findSceneName(string level_id) {
            foreach (var world in _store.root.worlds) {
                foreach (var level in world.levels) {
                    if (level.id == level_id) {
                        return string.IsNullOrEmpty(level.scene) ? null : level.scene;
                    }
                }
            }
            return null;
        }
    }
}
