// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using UniRx;
using UniRx.Triggers;
using Germio;
using static Germio.Env;
using static Germio.Utils;

namespace Germio.Systems {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Manages game status notifications.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class NoticeSystem : MonoBehaviour {
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
        // protected Fields

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        /// <summary>
        /// Gets the text field for displaying messages.
        /// </summary>
        [FormerlySerializedAs("_message_text")]
        [SerializeField] protected Text MessageText;

        /// <summary>
        /// Gets the text field for displaying target information.
        /// </summary>
        [FormerlySerializedAs("_targets_text")]
        [SerializeField] protected Text TargetsText;

        /// <summary>
        /// Gets the text field for displaying points.
        /// </summary>
        [FormerlySerializedAs("_points_text")]
        [SerializeField] protected Text PointsText;

        /// <summary>
        /// Gets the text field for displaying the game mode.
        /// </summary>
        [FormerlySerializedAs("_mode_text")]
        [SerializeField] protected Text ModeText;

        /// <summary>
        /// Gets the text field for displaying energy information (used for development).
        /// </summary>
        [FormerlySerializedAs("_energy_text")]
        [SerializeField] protected Text EnergyText;

        /// <summary>
        /// Gets the text field for displaying power information (used for development).
        /// </summary>
        [FormerlySerializedAs("_power_text")]
        [SerializeField] protected Text PowerText;

        /// <summary>
        /// Gets the text field for displaying FPS information (used for development).
        /// </summary>
        [FormerlySerializedAs("_fps_text")]
        [SerializeField] protected Text FpsText;

        /// <summary>
        /// Gets the reference to the game system.
        /// </summary>
        protected GameSystem GameSystem;

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
            /// Handles the event when the game has been paused.
            /// </summary>
            GameSystem.Paused += () => {
                GermioLog.Write(message: "[Germio NoticeSystem] Paused fired");
                if (!GameSystem.Home) { MessageText.text = MESSAGE_GAME_PAUSE; }
            };

            /// <summary>
            /// Handles the event when the game has been resumed.
            /// </summary>
            GameSystem.Resumed += () => {
                GermioLog.Write(message: "[Germio NoticeSystem] Resumed fired");
                MessageText.text = string.Empty;
            };

            /// <summary>
            /// Handles a notify request from the Store. The notify id is a
            /// free-form string the game gives meaning to (the same way
            /// Rule.trigger and HistoryEntry.kind already are). "level_clear"
            /// is the one meaning this class currently knows.
            /// </summary>
            GameSystem.Store.NotifyRequested += (notify_id) => {
                GermioLog.Write(message: $"[Germio NoticeSystem] NotifyRequested fired, notify_id='{notify_id}'");
                if (notify_id == "level_clear") {
                    MessageText.text = MESSAGE_LEVEL_CLEAR;
                    GermioLog.Write(message: $"[Germio NoticeSystem] MessageText.text set to '{MESSAGE_LEVEL_CLEAR}'");
                } else {
                    GermioLog.Write(message: $"[Germio NoticeSystem] notify_id='{notify_id}' has no known meaning here, ignored");
                }
            };

            /// <summary>
            /// Handles the event when a new level has started.
            /// </summary>
            GameSystem.LevelStarted += () => {
                GermioLog.Write(message: "[Germio NoticeSystem] LevelStarted fired");
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