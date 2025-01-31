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
    /// A level scene.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Level : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        protected GameSystem _game_system;

        protected SoundSystem _sound_system;

        protected bool _is_pausing = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Events [verb, verb phrase]

        public event Action? OnPauseOn;

        public event Action? OnPauseOff;

        public event Action? OnStart;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods

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
        /// load methods handler.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// update methods handler.
        /// </summary>
        protected virtual void abilities_OnStart() { }
    }
}