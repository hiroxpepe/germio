// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Germio.Model;

namespace Germio.Core {
    /// <summary>
    /// Manages the runtime Scenario: trigger dispatch, state transitions, and persistence.
    /// G2 idempotency: once=true rules are tracked via History events.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Store {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Scenario _scenario;
        Snapshot? _snapshot;
        string   _base_path;
        bool     _is_dirty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Events

        /// <summary>
        /// Raised when a command requests a node transition.
        /// The string argument is the target node ID.
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
            _snapshot  = null;
            _base_path = string.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties

        /// <summary>The scenario data object. Updated after InitializeAsync completes.</summary>
        public Scenario scenario => _scenario;

        /// <summary>The current runtime snapshot (if loaded).</summary>
        public Snapshot? snapshot => _snapshot;

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
        /// Sets the snapshot for history recording.
        /// </summary>
        public void SetSnapshot(Snapshot snapshot) {
            _snapshot = snapshot;
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
        /// Dispatches a trigger ID against the current node's rules.
        /// G2 once-guard: rules with once=true fire at most once per session.
        /// </summary>
        /// <param name="trigger_id">The trigger identifier, e.g. "vol_goal".</param>
        public void DispatchTrigger(string trigger_id) {
            var node = FindNode(node_id: _scenario.initial_state.current_node);
            if (node == null) { return; }

            foreach (var rule in node.rules) {
                if (rule.trigger != trigger_id) { continue; }

                // G2 Layer-2 once-guard: check if this rule has already fired (once=true)
                if (rule.once) {
                    // Check if rule_fire event for this rule_id already exists in history
                    if (_snapshot != null) {
                        bool already_fired = _snapshot.history.entries.Any(
                            entry => entry.kind == "rule_fire" && entry.target_id == rule.id
                        );
                        if (already_fired) { continue; }  // Skip this rule
                    }
                }

                // Condition guard
                if (!Evaluator.Evaluate(condition: rule.condition, state: _scenario.initial_state)) { continue; }

                // Execute command
                Executor.Execute(command: rule.command, store: this);

                // Record once-shot rule firing to history
                if (rule.once) {
                    RecordHistoryEvent(kind: "rule_fire", target_id: rule.id);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods - Node lookup

        /// <summary>
        /// Recursively searches the scenario tree for a node with the given ID.
        /// </summary>
        public Node? FindNode(string node_id) {
            return findNodeRecursive(node: _scenario.root, node_id: node_id);
        }

        /// <summary>
        /// Returns the next node based on the current node's transitions.
        /// Evaluates the conditions and returns the first matching transition's target.
        /// Returns null if no transition matches or current node not found.
        /// </summary>
        public Node? GetNextNode(string current_node_id) {
            Node? current = FindNode(node_id: current_node_id);
            if (current == null) {
                return null;
            }
            foreach (Next transition in current.next) {
                // For now, we only check unconditional transitions.
                // The Evaluator handles conditional logic at runtime.
                if (string.IsNullOrEmpty(transition.condition)) {
                    return FindNode(node_id: transition.id);
                }
            }
            // If all transitions are conditional, return the first one without evaluation.
            // The caller (Evaluator) is responsible for condition evaluation.
            if (current.next.Count > 0) {
                return FindNode(node_id: current.next[0].id);
            }
            return null;
        }

        /// <summary>
        /// Recursively collects all nodes in the scenario tree.
        /// </summary>
        public List<Node> GetAllNodes() {
            var nodes = new List<Node>();
            collectNodesRecursive(node: _scenario.root, nodes: nodes);
            return nodes;
        }

        /// <summary>
        /// Collects only leaf nodes (nodes with no children) from the scenario tree.
        /// </summary>
        public List<Node> GetLeafNodes() {
            var leaves = new List<Node>();
            collectLeafNodesRecursive(node: _scenario.root, leaves: leaves);
            return leaves;
        }

        /// <summary>
        /// Gets the depth of a node from the root (root depth is 0).
        /// Returns -1 if the node is not found.
        /// </summary>
        public int GetNodeDepth(string node_id) {
            return getNodeDepthRecursive(node: _scenario.root, node_id: node_id, depth: 0);
        }

        /// <summary>
        /// Gets the list of ancestor nodes from the root down to (but not including) the target node.
        /// Returns an empty list if the node is not found or is the root.
        /// </summary>
        public List<Node> GetAncestors(string node_id) {
            var ancestors = new List<Node>();
            findAncestorsRecursive(node: _scenario.root, node_id: node_id, path: ancestors);
            return ancestors;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods - History

        /// <summary>
        /// Records a history event into the current Snapshot.
        /// Trims oldest entries if max_entries is exceeded.
        /// </summary>
        public void RecordHistoryEvent(string kind, string target_id) {
            if (_snapshot == null) {
                return;
            }
            HistoryEntry entry = new HistoryEntry {
                kind = kind,
                target_id = target_id,
                timestamp = getElapsedTime()
            };
            _snapshot.history.entries.Add(item: entry);
            // Trim if exceeded
            while (_snapshot.history.entries.Count > _snapshot.history.max_entries) {
                _snapshot.history.entries.RemoveAt(index: 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods - Existing (unchanged)

        /// <summary>
        /// Fires the OnTransitionRequested event with the given target ID.
        /// Called by Executor for request_transition commands.
        /// </summary>
        public void RequestTransition(string target_id) {
            OnTransitionRequested?.Invoke(target_id);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods

        /// <summary>
        /// Recursive helper for FindNode.
        /// </summary>
        Node? findNodeRecursive(Node node, string node_id) {
            if (node.id == node_id) {
                return node;
            }
            foreach (Node child in node.children) {
                Node? found = findNodeRecursive(node: child, node_id: node_id);
                if (found != null) {
                    return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Recursive helper for GetAllNodes.
        /// </summary>
        void collectNodesRecursive(Node node, List<Node> nodes) {
            nodes.Add(item: node);
            foreach (Node child in node.children) {
                collectNodesRecursive(node: child, nodes: nodes);
            }
        }

        /// <summary>
        /// Recursive helper for GetLeafNodes.
        /// </summary>
        void collectLeafNodesRecursive(Node node, List<Node> leaves) {
            if (node.children.Count == 0) {
                leaves.Add(item: node);
                return;
            }
            foreach (Node child in node.children) {
                collectLeafNodesRecursive(node: child, leaves: leaves);
            }
        }

        /// <summary>
        /// Recursive helper for GetNodeDepth.
        /// </summary>
        int getNodeDepthRecursive(Node node, string node_id, int depth) {
            if (node.id == node_id) {
                return depth;
            }
            foreach (Node child in node.children) {
                int found = getNodeDepthRecursive(node: child, node_id: node_id, depth: depth + 1);
                if (found != -1) {
                    return found;
                }
            }
            return -1;
        }

        /// <summary>
        /// Recursive helper for GetAncestors.
        /// </summary>
        bool findAncestorsRecursive(Node node, string node_id, List<Node> path) {
            if (node.id == node_id) {
                return true;
            }
            foreach (Node child in node.children) {
                path.Add(item: node);
                if (findAncestorsRecursive(node: child, node_id: node_id, path: path)) {
                    return true;
                }
                path.RemoveAt(index: path.Count - 1);
            }
            return false;
        }

        /// <summary>
        /// Returns the elapsed time since the session started.
        /// Used for history timestamps.
        /// </summary>
        float getElapsedTime() {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.Time.realtimeSinceStartup;
#else
            // For non-Unity test builds
            return 0.0f;
#endif
        }
    }
}
