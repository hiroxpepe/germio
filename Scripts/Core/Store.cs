// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Threading.Tasks;

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Manages the runtime Scenario: trigger dispatch, state transitions, and persistence.
    /// G2 idempotency: once=true rules are tracked in State.fired_rules.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Store {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Scenario _scenario;
        string   _base_path;
        bool     _is_dirty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Events

        /// <summary>
        /// Raised when a command requests a level/scene transition.
        /// The string argument is the target level ID.
        /// </summary>
        public event Action<string>? OnTransitionRequested;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor (public for unit tests — no file I/O)

        /// <summary>
        /// Constructs a Store with a pre-loaded Scenario.
        /// Used in unit tests to inject state without file access.
        /// </summary>
        public Store(Scenario scenario) {
            _scenario  = scenario;
            _base_path = string.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties

        /// <summary>The scenario data object. Updated after InitializeAsync completes.</summary>
        public Scenario scenario => _scenario;

        /// <summary>The current runtime State.</summary>
        public State state => _scenario.state;

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
            if (loaded != null) { _scenario = loaded; }
            _is_dirty = false;
        }

        /// <summary>
        /// Persists state to disk if dirty.
        /// </summary>
        public async Task SaveAsync(bool encrypt = false) {
            if (!_is_dirty) { return; }
            await Storage.SaveAsync(data: _scenario, encrypt: encrypt, base_path: _base_path);
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
        /// Dispatches a trigger ID against the current scene's rules.
        /// G2 once-guard: rules with once=true fire at most once per State lifetime.
        /// </summary>
        /// <param name="trigger_id">The trigger identifier, e.g. "vol_goal".</param>
        public void Dispatch(string trigger_id) {
            var level = findLevel(level_id: _scenario.state.current_scene);
            if (level == null) { return; }

            foreach (var rule in level.rules) {
                if (rule.trigger != trigger_id) { continue; }

                // G2 Layer 2: once-shot guard via fired_rules
                if (rule.once && _scenario.state.fired_rules.Contains(rule.id)) { continue; }

                // Condition guard
                if (!Evaluator.Evaluate(condition: rule.condition, state: _scenario.state)) { continue; }

                // Execute command
                Executor.Execute(command: rule.command, store: this);

                // Record once-shot rule
                if (rule.once) { _scenario.state.fired_rules.Add(rule.id); }
            }
        }

        /// <summary>
        /// Returns the first level that satisfies its transition condition from the given level.
        /// Returns null if no condition is met.
        /// </summary>
        public Level? GetNextLevel(string current_level_id) {
            var current = findLevel(level_id: current_level_id);
            if (current == null) { return null; }

            foreach (var next in current.next) {
                if (Evaluator.Evaluate(condition: next.condition, state: _scenario.state)) {
                    return findLevel(level_id: next.id);
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves a level by world ID and level ID.
        /// </summary>
        public Level? GetLevel(string world_id, string level_id) {
            foreach (var world in _scenario.worlds) {
                if (world.id != world_id) { continue; }
                foreach (var level in world.levels) {
                    if (level.id == level_id) { return level; }
                }
            }
            return null;
        }

        /// <summary>
        /// Fires the OnTransitionRequested event with the given target ID.
        /// Called by Executor for request_transition commands.
        /// </summary>
        public void RequestTransition(string target_id) {
            OnTransitionRequested?.Invoke(target_id);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Searches all worlds for a level matching the given ID.
        /// </summary>
        Level? findLevel(string level_id) {
            foreach (var world in _scenario.worlds) {
                foreach (var level in world.levels) {
                    if (level.id == level_id) { return level; }
                }
            }
            return null;
        }
    }
}
