// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEngine.GameObject;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;
using Germio.Model;
using Germio.Systems;

namespace Germio {
    /// <summary>
    /// Base class for all scene controllers in a Germio-driven application.
    /// Bridges Unity input world and the Germio DSL world by:
    ///   1. Publishing input button events to the Bus as signal_btn_* triggers.
    ///   2. Invoking ancestor-order handlers ([GermioSceneHandler] attribute) on Start.
    ///
    /// Subclasses (typically a single partial class like Scene_Handlers) implement
    /// scene-specific logic by adding methods marked with [GermioSceneHandler(id: "...")].
    /// 
    /// When the scene starts, Scene reads scenario.initial_state.current_node and
    /// invokes all matching handlers from root to leaf (ancestor order).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Scene : InputMapper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        /// <summary>
        /// Reference to the game system, used to access Bus and Store.
        /// </summary>
        protected GameSystem _game_system = null!;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        protected virtual void Awake() {
            GermioLog.Write(message: "[Germio Scene] Awake");
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
            GermioLog.Write(message: $"[Germio Scene] Awake done; _game_system={(_game_system != null ? "OK" : "NULL")}");
        }

        // Start is called before the first frame update.
        protected new void Start() {
            base.Start();
            GermioLog.Write(message: "[Germio Scene] Start");

            this.UpdateAsObservable()
                .Where(predicate: _ =>
                    _start_button.wasPressedThisFrame || _a_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    GermioLog.Write(message: "[Germio Scene] publish signal_btn_start_pressed");
                    _game_system.bus?.Publish(signal_id: "signal_btn_start_pressed");
                }).AddTo(gameObjectComponent: this);

            this.UpdateAsObservable()
                .Where(predicate: _ =>
                    _select_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    GermioLog.Write(message: "[Germio Scene] publish signal_btn_select_pressed");
                    _game_system.bus?.Publish(signal_id: "signal_btn_select_pressed");
                }).AddTo(gameObjectComponent: this);

            this.UpdateAsObservable()
                .Where(predicate: _ =>
                    _up_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    _game_system.bus?.Publish(signal_id: "signal_btn_up_pressed");
                }).AddTo(gameObjectComponent: this);

            this.UpdateAsObservable()
                .Where(predicate: _ =>
                    _down_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    _game_system.bus?.Publish(signal_id: "signal_btn_down_pressed");
                }).AddTo(gameObjectComponent: this);

            // Defer handler invocation until GameSystem finishes loading germio.json.
            StartCoroutine(invokeAfterReady());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods

        /// <summary>
        /// Coroutine that waits for GameSystem to finish loading germio.json,
        /// then invokes ancestor-order handlers.
        /// </summary>
        IEnumerator invokeAfterReady() {
            GermioLog.Write(message: "[Germio Scene] waiting for GameSystem.isReady...");
            yield return new WaitUntil(predicate: () => _game_system != null && _game_system.isReady);
            GermioLog.Write(message: "[Germio Scene] GameSystem.isReady=true; invoking handlers");
            invokeAncestorHandlers();
        }

        /// <summary>
        /// Walks the scenario tree from root to the current node and invokes
        /// all handlers whose [GermioSceneHandler] attribute matches an ancestor's id.
        /// </summary>
        void invokeAncestorHandlers() {
            if (_game_system?.store?.scenario == null) {
                GermioLog.Write(message: "[Germio Scene] store.scenario is null; aborting");
                return;
            }
            string current_id = _game_system.store.scenario.initial_state.current_node;
            GermioLog.Write(message: $"[Germio Scene] current_node='{current_id}'");
            if (string.IsNullOrEmpty(value: current_id)) { return; }

            List<Node> ancestors = new List<Node>();
            findAncestorPath(node: _game_system.store.scenario.root, target_id: current_id, path: ancestors);
            if (ancestors.Count == 0) {
                GermioLog.Write(message: $"[Germio Scene] node '{current_id}' not found in tree");
                return;
            }
            string ancestor_list = string.Join(", ", ancestors.ConvertAll<string>(n => n.id));
            GermioLog.Write(message: $"[Germio Scene] ancestors=[{ancestor_list}]");

            var methods = GetType().GetMethods(
                bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (Node node in ancestors) {
                MethodInfo? handler = methods.FirstOrDefault(predicate: m => {
                    var attr = m.GetCustomAttribute<GermioSceneHandlerAttribute>();
                    return attr != null && attr.id == node.id;
                });
                if (handler != null) {
                    GermioLog.Write(message: $"[Germio Scene] invoking '{handler.Name}' for node '{node.id}'");
                    try {
                        handler.Invoke(obj: this, parameters: null);
                    } catch (Exception ex) {
                        GermioLog.Write(message: $"[Germio Scene] handler '{handler.Name}' threw: {ex.InnerException?.Message ?? ex.Message}");
                    }
                } else {
                    GermioLog.Write(message: $"[Germio Scene] no handler for node '{node.id}'");
                }
            }
        }

        bool findAncestorPath(Node node, string target_id, List<Node> path) {
            path.Add(item: node);
            if (node.id == target_id) {
                return true;
            }
            if (node.children != null) {
                foreach (Node child in node.children) {
                    if (findAncestorPath(node: child, target_id: target_id, path: path)) {
                        return true;
                    }
                }
            }
            path.RemoveAt(index: path.Count - 1);
            return false;
        }
    }

    /// <summary>
    /// Marks a method as a scene handler bound to a germio.json node id.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class GermioSceneHandlerAttribute : Attribute {
        public string id { get; }

        public GermioSceneHandlerAttribute(string id) {
            this.id = id;
        }
    }
}