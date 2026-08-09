// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using Germio;
using Germio.Core;
using Germio.Triggers;
using Germio.Model;
using static Germio.Env;
using static Germio.Utils;
using Scenario = Germio.Model.Scenario;
using Snapshot = Germio.Model.Snapshot;

namespace Germio.Systems {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Manages the game system, including levels, home interactions, and the JSON-driven state machine.
    /// Initialises <see cref="Store"/>, <see cref="Bus"/>,
    /// and <see cref="SceneLoader"/> from <c>StreamingAssets/germio.json</c>.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class GameSystem : MonoBehaviour {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        /// <summary>Runtime data state machine.</summary>
        Store _store = null!;

        /// <summary>Trigger dispatch bus (G2 Layer-1 guard).</summary>
        Bus _bus = null!;

        /// <summary>Subscribes to transition requests and calls SceneManager.</summary>
        SceneLoader _scene_loader = null!;

        bool _home;
        bool _beat;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb]

        /// <summary>
        /// Fires once when the game has been paused.
        /// (past participle: a single, completed happening)
        /// </summary>
        public event Action? Paused;

        /// <summary>
        /// Fires once when the game has been resumed from a paused state.
        /// (past participle: a single, completed happening)
        /// </summary>
        public event Action? Resumed;

        /// <summary>
        /// Fires once when a level has started.
        /// (past participle: a single, completed happening)
        /// </summary>
        public event Action? LevelStarted;

        /// <summary>
        /// Fires once when the player has returned home.
        /// (past participle: a single, completed happening)
        /// </summary>
        public event Action? HomeReturned;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>
        /// Gets or sets the current game mode for the system.
        /// </summary>
        public string Mode { get => Status.Mode; set => Status.Mode = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the player is at home.
        /// Setter mirrors to flags.player_at_home so DSL rules can read it.
        /// </summary>
        public bool Home {
            get => _home;
            set {
                _home = value;
                if (_store?.Scenario?.initial_state?.flags != null) {
                    _store.Scenario.initial_state.flags["player_at_home"] = value;
                    GermioLog.Write(message: $"[Germio GameSystem] home={value}, flags.player_at_home={value}");
                } else {
                    GermioLog.Write(message: $"[Germio GameSystem] home={value} BUT flags dict is null");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the level is beaten.
        /// Setter mirrors to flags.is_beat so DSL rules can read it.
        /// </summary>
        public bool Beat {
            get => _beat;
            set {
                _beat = value;
                if (_store?.Scenario?.initial_state?.flags != null) {
                    _store.Scenario.initial_state.flags["is_beat"] = value;
                    GermioLog.Write(message: $"[Germio GameSystem] beat={value}, flags.is_beat={value}");
                } else {
                    GermioLog.Write(message: $"[Germio GameSystem] beat={value} BUT flags dict is null");
                }
            }
        }

        /// <summary>Exposes the Store for use by Scene base class.</summary>
        public Store Store => _store;

        /// <summary>Indicates whether async initialization has completed.</summary>
        public bool IsReady { get; private set; } = false;

        /// <summary>
        /// Exposes the Bus for use by Zone, Home, and Despawn.
        /// </summary>
        public Bus? Bus => _bus;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>Fires the Paused event. Called by application Scene controllers.</summary>
        public void FirePaused() {
            GermioLog.Write(message: "[Germio GameSystem] FirePaused relay");
            Paused?.Invoke();
        }

        /// <summary>Fires the Resumed event.</summary>
        public void FireResumed() {
            GermioLog.Write(message: "[Germio GameSystem] FireResumed relay");
            Resumed?.Invoke();
        }

        /// <summary>Fires the LevelStarted event.</summary>
        public void FireLevelStarted() {
            GermioLog.Write(message: "[Germio GameSystem] FireLevelStarted relay");
            LevelStarted?.Invoke();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods handler.

        /// <summary>
        /// Handles the loading of ability-related methods during Awake.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// Handles the updating of ability-related methods during Start.
        /// </summary>
        protected virtual void abilities_OnStart() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            GermioLog.Write(message: "[Germio GameSystem] Awake start");
            // Sets the frame rate.
            Application.targetFrameRate = FPS;

            // --- Germio state machine bootstrap ---
            // Create objects synchronously with an empty root so that Zone,
            // Home, and Despawn can safely obtain a reference to bus
            // in their Start() methods. The actual JSON config is loaded in Start().
            _store = new Store(new Scenario());
            _bus = new Bus(_store);
            _scene_loader = new SceneLoader(
                store:     _store,
                load_scene: name => {
                    GermioLog.Write(message: $"[Germio GameSystem] LoadScene('{name}'), resetting timeScale=1");
                    Time.timeScale = 1f;
                    LoadScene(sceneName: name);
                }
            );
            // -----------------------------------------------

            // hotfix1: Push reversal — Scene-side controllers call FirePaused / FireResumed /
            // FireLevelStarted relay methods directly. GameSystem (Germio) does not import Stemic types.

            if (HasHome()) {
                // Gets the home.
                Home home = Find(name: HOME_TYPE).Get<Home>();

                /// <summary>
                /// Invokes the event when the player returns home.
                /// </summary>
                this.Home = false;
                home.Returned += () => {
                    GermioLog.Write(message: "[Germio GameSystem] Home.Returned fired");
                    this.Home = true;
                    HomeReturned?.Invoke();
                };
                GermioLog.Write(message: "[Germio GameSystem] Home.Returned subscription registered");
            }

            /// <summary>
            /// Calls the ability load handler for initialization.
            /// </summary>
            abilities_OnAwake();
        }

        // OnDestroy is called when the MonoBehaviour will be destroyed.
        void OnDestroy() {
            _scene_loader?.Dispose();
        }

        // Start is called before the first frame update.
        void Start() {
            /// <summary>
            /// Loads germio.json asynchronously from StreamingAssets.
            /// The coroutine waits until the Task completes so the JSON state is ready
            /// before any trigger fires.
            /// </summary>
            StartCoroutine(initializeStateCoroutine());

            /// <summary>
            /// Calls the ability update handler for initialization.
            /// </summary>
            abilities_OnStart();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Coroutines

        /// <summary>
        /// Coroutine that wraps async Store.InitializeAsync so it runs inside Unity's
        /// update loop without blocking the main thread.
        /// </summary>
        IEnumerator initializeStateCoroutine() {
            GermioLog.Write(message: "[Germio GameSystem] initializeStateCoroutine start");

            // Step 1: Load germio.json
            GermioLog.Write(message: "[Germio GameSystem]   step1: loading germio.json...");
            var scenario_task = _store.InitializeAsync(Application.streamingAssetsPath);
            while (!scenario_task.IsCompleted) { yield return null; }
            if (scenario_task.IsFaulted) {
                GermioLog.Write(message: $"[Germio GameSystem]   step1 FAULTED: {scenario_task.Exception}");
            } else {
                GermioLog.Write(message: $"[Germio GameSystem]   step1 done; scenario.initial_state.current_node='{_store.Scenario.initial_state.current_node}'");
            }

            // Step 2: Load Snapshot
            GermioLog.Write(message: "[Germio GameSystem]   step2: loading snapshot slot 0...");
            var snapshot_task = Storage.LoadSnapshotAsync(slot: 0);
            while (!snapshot_task.IsCompleted) { yield return null; }
            Snapshot loaded = snapshot_task.Result;
            if (loaded == null) {
                GermioLog.Write(message: "[Germio GameSystem]   step2 done; no snapshot file (creating fresh)");
            } else {
                string ld = loaded.state == null ? "<null>" : loaded.state.current_node;
                GermioLog.Write(message: $"[Germio GameSystem]   step2 done; snapshot.state.current_node='{ld}'");
            }
            Snapshot snapshot = loaded ?? new Snapshot { state = _store.Scenario.initial_state };

            GermioLog.Write(message: "[Germio GameSystem]   step3: SetSnapshot...");
            _store.SetSnapshot(snapshot: snapshot);

            // hotfix9: Unity Scene is the single source of truth for current_node.
            // Override whatever value came from snapshot/germio.json with the result of
            // resolving the active Unity Scene name through ScenarioNavigator.
            string scene_name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string? resolved = ScenarioNavigator.FindNodeIDBySceneName(
                root: _store.Scenario.root, scene_name: scene_name);
            if (resolved != null) {
                _store.Scenario.initial_state.current_node = resolved;
                GermioLog.Write(message: $"[Germio GameSystem]   step4: Unity scene '{scene_name}' → current_node='{resolved}'");
            } else {
                GermioLog.Write(message: $"[Germio GameSystem]   step4: WARN active scene '{scene_name}' not in germio.json; current_node left at '{_store.Scenario.initial_state.current_node}'");
            }

            IsReady = true;
            GermioLog.Write(message: $"[Germio GameSystem] init complete; current_node='{_store.Scenario.initial_state.current_node}', isReady=true");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // inner Classes

        #region Status

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private inner Classes

        /// <summary>
        /// Represents the game status.
        /// </summary>
        static class Status {
            ///////////////////////////////////////////////////////////////////////////////////////////
            // private static Fields

            /// <summary>
            /// Stores the current game mode.
            /// </summary>
            static string _mode;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // static Constructor

            /// <summary>
            /// Initializes the static Status class and sets the default game mode.
            /// </summary>
            static Status() {
                _mode = MODE_NORMAL;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public static Properties [noun, adjective]

            /// <summary>
            /// Gets or sets the current game mode for the status.
            /// </summary>
            public static string Mode {
                get => _mode; set => _mode = value;
            }
        }

        #endregion
    }
}