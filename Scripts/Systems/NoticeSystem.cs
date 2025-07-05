// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;
using static Germio.Utils;

namespace Germio {
    /// <summary>
    /// Manages game status notifications.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class NoticeSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        /// <summary>
        /// Gets the text field for displaying messages.
        /// </summary>
        [SerializeField] protected Text _message_text;

        /// <summary>
        /// Gets the text field for displaying target information.
        /// </summary>
        [SerializeField] protected Text _targets_text;

        /// <summary>
        /// Gets the text field for displaying points.
        /// </summary>
        [SerializeField] protected Text _points_text;

        /// <summary>
        /// Gets the text field for displaying the game mode.
        /// </summary>
        [SerializeField] protected Text _mode_text;

        /// <summary>
        /// Gets the text field for displaying energy information (used for development).
        /// </summary>
        [SerializeField] protected Text _energy_text;

        /// <summary>
        /// Gets the text field for displaying power information (used for development).
        /// </summary>
        [SerializeField] protected Text _power_text;

        /// <summary>
        /// Gets the text field for displaying FPS information (used for development).
        /// </summary>
        [SerializeField] protected Text _fps_text;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        /// <summary>
        /// Gets the reference to the game system.
        /// </summary>
        protected GameSystem _game_system;

        /// <summary>
        /// Gets the frame count for FPS calculation.
        /// </summary>
        int _frame_count;

        /// <summary>
        /// Gets the elapsed time for FPS calculation.
        /// </summary>
        float _elapsed_time;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();

            /// <summary>
            /// Handles the event when the game is paused.
            /// </summary>
            _game_system.OnPauseOn += () => { if (!_game_system.home) { _message_text.text = MESSAGE_GAME_PAUSE; }};

            /// <summary>
            /// Handles the event when the game is unpaused.
            /// </summary>
            _game_system.OnPauseOff += () => { _message_text.text = string.Empty; };

            /// <summary>
            /// Handles the event when the player returns home.
            /// </summary>
            _game_system.OnCameBackHome += () => { _message_text.text = MESSAGE_LEVEL_CLEAR; };

            /// <summary>
            /// Handles the event when a new level starts.
            /// </summary>
            _game_system.OnStartLevel += () => {
                switch (GetActiveScene().name) {
                    case SCENE_LEVEL_1:
                        _message_text.text = SCENE_LEVEL_1;
                        break;
                    case SCENE_LEVEL_2:
                        _message_text.text = SCENE_LEVEL_2;
                        break;
                    case SCENE_LEVEL_3:
                        _message_text.text = SCENE_LEVEL_3;
                        break;
                }
                // Waits 1.5 seconds, then shows the start message.
                Observable.Timer(TimeSpan.FromSeconds(1.5))
                    .Subscribe(onNext: _ => {
                        _message_text.text = MESSAGE_LEVEL_START;
                    }).AddTo(gameObjectComponent: this);
                // Waits 3 seconds, then clears the message.
                Observable.Timer(TimeSpan.FromSeconds(3.0))
                    .Subscribe(onNext: _ => {
                        _message_text.text = string.Empty;
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

        /// <summary>
        /// Updates the game mode status display.
        /// </summary>
        void updateGameStatus() {
            _mode_text.text = string.Format("Mode: {0}", _game_system.mode);
            switch (_game_system.mode) {
                case MODE_EASY: _mode_text.color = yellow; break;
                case MODE_NORMAL: _mode_text.color = green; break;
                case MODE_HARD: _mode_text.color = purple; break;
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
                _fps_text.text = fps_rate;
                _frame_count = 0;
                _elapsed_time = 0f;
            }
        }
    }
}