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
    /// Handles player despawn logic.
    /// Directly reloads the active scene (original behavior retained as primary fallback),
    /// and also emits "sig_despawn" to <see cref="UniversalTriggerSystem"/> so that
    /// StateManager can update counters/flags as needed (Strangler Fig Pattern).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Despawn : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        /// <summary>
        /// Holds a reference to the game system instance.
        /// </summary>
        GameSystem _game_system = null!;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        /// <summary>
        /// Occurs when the player is despawned from the scene.
        /// Retained for backward compatibility with observers (e.g., SoundSystem).
        /// </summary>
        public event Action? OnDespawn;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            /// <summary>
            /// Initializes the game system reference when the script instance is loaded.
            /// </summary>
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
        }

        // Start is called before the first frame update.
        void Start() {
            /// <summary>
            /// When the player enters the despawn trigger:
            ///   1. Reload the active scene directly (primary, always works).
            ///   2. Emit "sig_despawn" signal so StateManager can react (counters, flags).
            /// Both LoadScene calls targeting the same scene are idempotent in Unity.
            /// </summary>
            this.OnTriggerEnterAsObservable()
                .Where(predicate: x =>
                    x.Like(type: PLAYER_TYPE))
                .Subscribe(onNext: _ => {
                    OnDespawn?.Invoke();
                    LoadScene(sceneName: GetActiveScene().name);
                    _game_system.universalTriggerSystem?.OnSignalReceived("sig_despawn");
                }).AddTo(gameObjectComponent: this);
        }
    }
}