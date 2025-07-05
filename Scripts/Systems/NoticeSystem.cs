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
        /// Text field for displaying messages.
        /// </summary>
        [SerializeField] protected Text _message_text;

        /// <summary>
        /// Text field for displaying target information.
        /// </summary>
        [SerializeField] protected Text _targets_text;

        /// <summary>
        /// Text field for displaying points.
        /// </summary>
        [SerializeField] protected Text _points_text;

        /// <summary>
        /// Text field for displaying the game mode.
        /// </summary>
        [SerializeField] protected Text _mode_text;

        /// <summary>
        /// Text field for displaying energy information (used for development).
        /// </summary>
        [SerializeField] protected Text _energy_text;

        /// <summary>
        /// Text field for displaying power information (used for development).
        /// </summary>
        [SerializeField] protected Text _power_text;

        /// <summary>
        /// Text field for displaying FPS information (used for development).
        /// </summary>
        [SerializeField] protected Text _fps_text;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        /// <summary>
        /// Reference to the game system.
        /// </summary>
        protected GameSystem _game_system;

        /// <summary>
        /// Frame count for FPS calculation.
        /// </summary>
        int _frame_count;

        /// <summary>
        /// Elapsed time for FPS calculation.
        /// </summary>
        float _elapsed_time;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();

            /// <summary>
            /// When the game is paused.
            /// </summary>
            _game_system.OnPauseOn += () => { if (!_game_system.home) { _message_text.text = MESSAGE_GAME_PAUSE; }};

            /// <summary>
            /// When the game is unpaused.
            /// </summary>
            _game_system.OnPauseOff += () => { _message_text.text = string.Empty; };

            /// <summary>
            /// When the game comes back home.
            /// </summary>
            _game_system.OnCameBackHome += () => { _message_text.text = MESSAGE_LEVEL_CLEAR; };

            /// <summary>
            /// When a new level starts.
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
            /// Sets up the load methods.
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
            /// Sets up the update methods.
            /// </summary>
            abilities_OnStart();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods handler.

        /// <summary>
        /// Handles the loading of methods.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// Handles the updating of methods.
        /// </summary>
        protected virtual void abilities_OnStart() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Updates the game status.
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
        /// Updates the vehicle status.
        /// </summary>
        void updateVehicleStatus() {
        }

        /// <summary>
        /// Updates the FPS status.
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