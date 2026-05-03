// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;

using Germio.Core;
using Germio.Model;

namespace Germio.Systems {
    /// <summary>
    /// Listens for transition requests from the Store and loads the corresponding scene.
    /// Uses an injected <see cref="Action{String}"/> for the actual load call so that
    /// the class is fully testable without a Unity runtime.
    ///
    /// Responsibilities:
    ///   1. Resolve the target node's <c>scene</c> name from the Node tree.
    ///   2. Update <see cref="State.current_node"/> to the new node ID.
    ///   3. Mark state dirty so the next save persists the updated current node.
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
        readonly Bus? _bus;
        bool _disposed;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        /// <summary>
        /// Constructs a SceneLoader and subscribes to <see cref="Store.OnTransitionRequested"/>.
        /// </summary>
        /// <param name="store">The Store whose scenario provides node-to-scene mapping.</param>
        /// <param name="load_scene">
        /// Delegate invoked with the resolved scene name when a transition is requested.
        /// In Unity production code: <c>name =&gt; SceneManager.LoadScene(name)</c>.
        /// In tests: any capture lambda.
        /// </param>
        /// <param name="bus">
        /// Optional Bus instance. When provided, <see cref="Bus.ClearActiveZones"/> is called
        /// on each scene transition to prevent stale zone IDs from persisting (P5-T3).
        /// </param>
        public SceneLoader(Store store, Action<string> load_scene, Bus? bus = null) {
            _store      = store;
            _load_scene = load_scene;
            _bus        = bus;
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
        /// Looks up the scene name from the target node, updates state, and invokes the load delegate.
        /// </summary>
        /// <param name="target_node_id">The target node ID to transition to.</param>
        void handleTransition(string target_node_id) {
            // Clear stale active zones before entering the new scene (P5-T3)
            _bus?.ClearActiveZones();
            string? scene_name = findSceneName(node_id: target_node_id);
            // Guard: skip unknown nodes and nodes with empty scene names
            if (string.IsNullOrEmpty(scene_name)) { return; }

            // Update current_node in both Scenario.initial_state (legacy compatibility)
            // and Snapshot.state (the source of truth as of Phase 5.8 v2 fix6).
            _store.scenario.initial_state.current_node = target_node_id;
            if (_store.snapshot != null) {
                _store.snapshot.state.current_node = target_node_id;
            }
            _store.MarkDirty();

            // Persist the snapshot so the next scene can resume from this current_node.
            // SYNCHRONOUS write: the next scene's GameSystem will start reading the
            // snapshot file immediately after LoadScene returns, so we must guarantee
            // the file is fully written before SceneManager.LoadScene is invoked.
            // (fix6 hotfix7: was async fire-and-forget, which raced with the new
            // scene's LoadSnapshotAsync and caused current_node to revert to the
            // germio.json initial value, breaking all scene-to-scene transitions.)
            if (_store.snapshot != null) {
                Storage.SaveSnapshot(snapshot: _store.snapshot, slot: 0);
            }

            // Auto-fire _on_enter_node trigger so DSL rules can react to node entry
            // (e.g. reset_flags on title entry). This is the symmetric counterpart to
            // external triggers (vol_*, sig_*, signal_btn_*) — internal state changes
            // also flow through the same Bus/Store dispatch mechanism.
            _store.DispatchTrigger(trigger_id: "_on_enter_node");

            _load_scene(scene_name);
        }

        /// <summary>
        /// Searches the Node tree for a node matching <paramref name="node_id"/>
        /// and returns its <c>scene</c> field.
        /// Uses <see cref="Store.FindNode"/> to search the entire tree.
        /// </summary>
        /// <param name="node_id">The node ID to search for.</param>
        /// <returns>The scene name, or null if not found.</returns>
        string? findSceneName(string node_id) {
            var node = _store.FindNode(node_id: node_id);
            if (node == null) { return null; }
            return string.IsNullOrEmpty(node.scene) ? null : node.scene;
        }
    }
}