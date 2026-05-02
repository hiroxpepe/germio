// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;

using static Germio.Env;
using static Germio.Utils;

using Germio;
using Germio.Core;
using Germio.Levels;
using Germio.Triggers;
using Scenario = Germio.Model.Scenario;

namespace Germio.Systems {
    /// <summary>
    /// Manages the game system, including levels, home interactions, and the JSON-driven state machine.
    /// Initialises <see cref="Store"/>, <see cref="Bus"/>,
    /// and <see cref="SceneLoader"/> from <c>StreamingAssets/germio.json</c>.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class GameSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        /// <summary>Runtime data state machine.</summary>
        Store _store = null!;

        /// <summary>Trigger dispatch bus (G2 Layer-1 guard).</summary>
        Bus _bus = null!;

        /// <summary>Subscribes to transition requests and calls SceneManager.</summary>
        SceneLoader _scene_loader = null!;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives]

        /// <summary>
        /// Gets or sets the current game mode for the system.
        /// </summary>
        public string mode { get => Status.mode; set => Status.mode = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the player is at home.
        /// </summary>
        public bool home { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the level is beaten.
        /// </summary>
        public bool beat { get; set; }

        /// <summary>
        /// Exposes the Bus for use by Zone, Home, and Despawn.
        /// </summary>
        public Bus? bus => _bus;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        /// <summary>
        /// Occurs when the game is paused.
        /// </summary>
        public event Action? OnPauseOn;

        /// <summary>
        /// Occurs when the game is unpaused.
        /// </summary>
        public event Action? OnPauseOff;

        /// <summary>
        /// Occurs when a level starts.
        /// </summary>
        public event Action? OnStartLevel;

        /// <summary>
        /// Occurs when the player returns home.
        /// </summary>
        public event Action? OnCameBackHome;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
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
                    // Bridge: persist current_node to PlayerPrefs so it survives the scene reload.
                    // When the new scene's GameSystem reads InitializeAsync, it restores this value.
                    PlayerPrefs.SetString(key: CURRENT_SCENE_KEY, value: _store.scenario.initial_state.current_node);
                    PlayerPrefs.Save();
                    // Reset time scale so the next scene does not start frozen.
                    // Level.cs's OnCameBackHome handler sets timeScale=0f; this undoes that
                    // before Unity commits the scene load at end-of-frame.
                    Time.timeScale = 1f;
                    LoadScene(sceneName: name);
                }
            );
            // -----------------------------------------------

            if (HasLevel()) {
                // Gets the level by name.
                Level level = Find(name: LEVEL_TYPE).Get<Level>();

                /// <summary>
                /// Invokes the pause event when the level is paused.
                /// </summary>
                level.OnPauseOn += () => { OnPauseOn?.Invoke(); };

                /// <summary>
                /// Invokes the resume event when the level is unpaused.
                /// </summary>
                level.OnPauseOff += () => { OnPauseOff?.Invoke(); };

                /// <summary>
                /// Invokes the start event when the level starts.
                /// </summary>
                level.OnStart += () => { OnStartLevel?.Invoke(); };
            }

            if (HasHome()) {
                // Gets the home.
                Home home = Find(name: HOME_TYPE).Get<Home>();

                /// <summary>
                /// Invokes the event when the player returns home.
                /// </summary>
                this.home = false;
                home.OnCameBack += () => {
                    this.home = true;
                    OnCameBackHome?.Invoke();
                };
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
            var task = _store.InitializeAsync(Application.streamingAssetsPath);
            while (!task.IsCompleted) { yield return null; }
            if (task.IsFaulted) {
                Debug.LogError($"[Germio] Failed to load germio.json: {task.Exception}");
            }
            // Bridge: restore current_node from PlayerPrefs if a prior scene set it.
            // This fixes the "die in Level 2 → restart Level 1" bug caused by the
            // JSON on disk still having the initial current_node after LoadScene.
            string bridged = PlayerPrefs.GetString(key: CURRENT_SCENE_KEY, defaultValue: string.Empty);
            if (!string.IsNullOrEmpty(bridged)) {
                _store.scenario.initial_state.current_node = bridged;
            }
        }

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
        // inner Classes

        #region Status

        /// <summary>
        /// Represents the game status.
        /// </summary>
        static class Status {
#nullable enable

            ///////////////////////////////////////////////////////////////////////////////////////////
            // static Fields [nouns, noun phrases]

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
            // public static Properties [noun, noun phrase, adjective]

            /// <summary>
            /// Gets or sets the current game mode for the status.
            /// </summary>
            public static string mode {
                get => _mode; set => _mode = value;
            }
        }

        #endregion
    }
}