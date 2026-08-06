// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;
using static Germio.Utils;

using Germio;

namespace Germio.Systems {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Manages game status notifications.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class NoticeSystem : MonoBehaviour {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Fields

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        /// <summary>
        /// Gets the text field for displaying messages.
        /// </summary>
        [SerializeField] protected Text MessageText;

        /// <summary>
        /// Gets the text field for displaying target information.
        /// </summary>
        [SerializeField] protected Text TargetsText;

        /// <summary>
        /// Gets the text field for displaying points.
        /// </summary>
        [SerializeField] protected Text PointsText;

        /// <summary>
        /// Gets the text field for displaying the game mode.
        /// </summary>
        [SerializeField] protected Text ModeText;

        /// <summary>
        /// Gets the text field for displaying energy information (used for development).
        /// </summary>
        [SerializeField] protected Text EnergyText;

        /// <summary>
        /// Gets the text field for displaying power information (used for development).
        /// </summary>
        [SerializeField] protected Text PowerText;

        /// <summary>
        /// Gets the text field for displaying FPS information (used for development).
        /// </summary>
        [SerializeField] protected Text FpsText;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Fields

        /// <summary>
        /// Gets the reference to the game system.
        /// </summary>
        protected GameSystem GameSystem;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        /// <summary>
        /// Gets the frame count for FPS calculation.
        /// </summary>
        int _frame_count;

        /// <summary>
        /// Gets the elapsed time for FPS calculation.
        /// </summary>
        float _elapsed_time;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods handler.

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
            GameSystem = Find(name: GAME_SYSTEM).Get<GameSystem>();

            /// <summary>
            /// Handles the event when the game is paused.
            /// </summary>
            GameSystem.OnPauseOn += () => { if (!GameSystem.Home) { MessageText.text = MESSAGE_GAME_PAUSE; }};

            /// <summary>
            /// Handles the event when the game is unpaused.
            /// </summary>
            GameSystem.OnPauseOff += () => { MessageText.text = string.Empty; };

            /// <summary>
            /// Handles the event when the player returns home.
            /// </summary>
            GameSystem.OnCameBackHome += () => { MessageText.text = MESSAGE_LEVEL_CLEAR; };

            /// <summary>
            /// Handles the event when a new level starts.
            /// </summary>
            GameSystem.OnStartLevel += () => {
                // Phase 5.13: Changed from switching based on the Unity Scene name (e.g., "Level_1")
                // to displaying the 'name' property of the node in germio.json (e.g., "Level 1").
                // Decoupled the Unity Scene filename from the display name (the human-readable string shown in the UI).
                // Looking up the Node using the current_node id and displaying its name.
                string current_id = GameSystem.Store.Scenario.initial_state.current_node;
                Germio.Model.Node? node = GameSystem.Store.FindNode(node_id: current_id);
                if (node != null) {
                    MessageText.text = node.name;
                }
                // Waits 1.5 seconds, then shows the start message.
                Observable.Timer(TimeSpan.FromSeconds(1.5))
                    .Subscribe(onNext: _ => {
                        MessageText.text = MESSAGE_LEVEL_START;
                    }).AddTo(gameObjectComponent: this);
                // Waits 3 seconds, then clears the message.
                Observable.Timer(TimeSpan.FromSeconds(3.0))
                    .Subscribe(onNext: _ => {
                        MessageText.text = string.Empty;
                    }).AddTo(gameObjectComponent: this);
            };

            /// <summary>
            /// Calls the ability load handler for initialization.
            /// </summary>
            abilities_OnAwake();
        }

        // Start is called before the first frame update.
        void Start() {
            // Updates the UI with the latest game, vehicle, and FPS status.
            this.UpdateAsObservable()
                .Subscribe(onNext: _ => {
                    updateGameStatus();
                    updateVehicleStatus();
                    updateFpsStatus();
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Calls the ability update handler for initialization.
            /// </summary>
            abilities_OnStart();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Updates the game mode status display.
        /// </summary>
        void updateGameStatus() {
            ModeText.text = string.Format("Mode: {0}", GameSystem.Mode);
            switch (GameSystem.Mode) {
                case MODE_EASY: ModeText.color = Yellow; break;
                case MODE_NORMAL: ModeText.color = Green; break;
                case MODE_HARD: ModeText.color = Purple; break;
            }
        }

        /// <summary>
        /// Updates the vehicle status display.
        /// </summary>
        void updateVehicleStatus() {
        }

        /// <summary>
        /// Updates the FPS status display.
        /// </summary>
        void updateFpsStatus() {
            _frame_count++;
            _elapsed_time += Time.deltaTime;
            if (_elapsed_time >= 1.0f) {
                float fps = 1.0f * _frame_count / _elapsed_time;
                string fps_rate = $"FPS {fps.ToString(format: "F2")}";
                FpsText.text = fps_rate;
                _frame_count = 0;
                _elapsed_time = 0f;
            }
        }
    }
}