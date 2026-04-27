// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Germio {
    /// <summary>
    /// Manages the runtime DataRoot: trigger dispatch, state transitions, and persistence.
    /// G2 idempotency: once=true events are tracked in DataState.firedEvents.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Store {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        DataRoot _root;
        string   _base_path;
        bool     _is_dirty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Events

        /// <summary>
        /// Raised when an action requests a level/scene transition.
        /// The string argument is the target level ID.
        /// </summary>
        public event Action<string>? OnTransitionRequested;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor (public for unit tests — no file I/O)

        /// <summary>
        /// Constructs a Store with a pre-loaded DataRoot.
        /// Used in unit tests to inject state without file access.
        /// </summary>
        public Store(DataRoot root) {
            _root      = root;
            _base_path = string.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties

        /// <summary>The root data object. Updated after InitializeAsync completes.</summary>
        public DataRoot root => _root;

        /// <summary>The current runtime DataState.</summary>
        public DataState state => _root.state;

        /// <summary>True if state has been mutated since the last save.</summary>
        public bool isDirty => _is_dirty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Initializes the store by loading data from disk.
        /// Call this instead of the constructor for production usage.
        /// </summary>
        public async Task InitializeAsync(string base_path) {
            _base_path = base_path;
            var loaded = await Storage.LoadAsync(base_path: base_path);
            if (loaded != null) { _root = loaded; }
            _is_dirty = false;
        }

        /// <summary>
        /// Persists state to disk if dirty.
        /// </summary>
        public async Task SaveAsync(bool encrypt = false) {
            if (!_is_dirty) { return; }
            await Storage.SaveAsync(data: _root, encrypt: encrypt, base_path: _base_path);
            _is_dirty = false;
        }

        /// <summary>
        /// Marks state as needing save.
        /// Called by Executor after any mutation.
        /// </summary>
        public void MarkDirty() {
            _is_dirty = true;
        }

        /// <summary>
        /// Dispatches a trigger ID against the current scene's events.
        /// G2 once-guard: events with once=true fire at most once per DataState lifetime.
        /// </summary>
        /// <param name="trigger_id">The trigger identifier, e.g. "vol_goal".</param>
        public void DispatchTrigger(string trigger_id) {
            var level = findLevel(level_id: _root.state.currentScene);
            if (level == null) { return; }

            foreach (var evt in level.events) {
                if (evt.trigger != trigger_id) { continue; }

                // G2 Layer 2: once-shot guard via firedEvents
                if (evt.once && _root.state.firedEvents.Contains(evt.id)) { continue; }

                // Condition guard
                if (!Evaluator.Evaluate(condition: evt.condition, state: _root.state)) { continue; }

                // Execute action
                Executor.Execute(action: evt.action, store: this);

                // Record once-shot event
                if (evt.once) { _root.state.firedEvents.Add(evt.id); }
            }
        }

        /// <summary>
        /// Returns the first level that satisfies its transition condition from the given level.
        /// Returns null if no condition is met.
        /// </summary>
        public DataLevel? GetNextLevel(string current_level_id) {
            var current = findLevel(level_id: current_level_id);
            if (current == null) { return null; }

            foreach (var next in current.next) {
                if (Evaluator.Evaluate(condition: next.condition, state: _root.state)) {
                    return findLevel(level_id: next.id);
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves a level by world ID and level ID.
        /// </summary>
        public DataLevel? GetLevel(string world_id, string level_id) {
            foreach (var world in _root.worlds) {
                if (world.id != world_id) { continue; }
                foreach (var level in world.levels) {
                    if (level.id == level_id) { return level; }
                }
            }
            return null;
        }

        /// <summary>
        /// Fires the OnTransitionRequested event with the given target ID.
        /// Called by Executor for requestTransition actions.
        /// </summary>
        public void RequestTransition(string target_id) {
            OnTransitionRequested?.Invoke(target_id);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Searches all worlds for a level matching the given ID.
        /// </summary>
        DataLevel? findLevel(string level_id) {
            foreach (var world in _root.worlds) {
                foreach (var level in world.levels) {
                    if (level.id == level_id) { return level; }
                }
            }
            return null;
        }
    }
}
