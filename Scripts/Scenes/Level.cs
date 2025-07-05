// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;

namespace Germio {
    /// <summary>
    /// Manages a level scene, including pause functionality and level transitions.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Level : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        /// <summary>
        /// Gets the reference to the game system.
        /// </summary>
        protected GameSystem _game_system;

        /// <summary>
        /// Gets the reference to the sound system.
        /// </summary>
        protected SoundSystem _sound_system;

        /// <summary>
        /// Gets a value indicating whether the game is currently paused.
        /// </summary>
        protected bool _is_pausing = false;

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
        /// Occurs when the level starts.
        /// </summary>
        public event Action? OnStart;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
            _sound_system = Find(name: SOUND_SYSTEM).Get<SoundSystem>();

            /// <summary>
            /// When the game system returns home.
            /// </summary>
            _game_system.OnCameBackHome += () => {
                _sound_system.Play(type: SEClip.Item);
                Time.timeScale = 0f;
                _sound_system.Play(type: BGMClip.BeatLevel);
            };

            /// <summary>
            /// Sets the default beat flag.
            /// </summary>
            _game_system.beat = true;

            /// <summary>
            /// Sets load Methods handler.
            /// </summary>
            abilities_OnAwake();
        }

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            /// <summary>
            /// Pauses or unpauses the game.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _start_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    if (_is_pausing) {
                        Time.timeScale = 1f;
                        OnPauseOff?.Invoke();
                    } else {
                        Time.timeScale = 0f;
                        OnPauseOn?.Invoke();
                    }
                    _is_pausing = !_is_pausing;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Loads the next level based on the current scene.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    (_start_button.wasPressedThisFrame || _a_button.wasPressedThisFrame) && 
                    _game_system.home && _game_system.beat)
                .Subscribe(onNext: _ => {
                    switch (GetActiveScene().name) {
                        case SCENE_LEVEL_1:
                            Time.timeScale = 1f;
                            LoadScene(sceneName: SCENE_LEVEL_2);
                            break;
                        case SCENE_LEVEL_2:
                            Time.timeScale = 1f;
                            LoadScene(sceneName: SCENE_LEVEL_3);
                            break;
                        case SCENE_LEVEL_3:
                            Time.timeScale = 1f;
                            LoadScene(sceneName: SCENE_ENDING);
                            break;
                    }
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Restarts the game by reloading the current scene.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _select_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    LoadScene(sceneName: GetActiveScene().name);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Sets up the update method handler.
            /// </summary>
            abilities_OnStart();

            /// <summary>
            /// Triggers the start event.
            /// </summary>
            OnStart?.Invoke();
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
    }
}