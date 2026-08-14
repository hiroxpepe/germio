// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using UniRx;
using UniRx.Triggers;
using Germio;
using Germio.Systems;
using static Germio.Env;

namespace Germio.Triggers {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Handles player despawn logic.
    /// Directly reloads the active scene (original behavior retained as primary fallback),
    /// and also emits "sig_despawn" to <see cref="Bus"/> so that
    /// Store can update counters/flags as needed (Strangler Fig Pattern).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Despawn : MonoBehaviour {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        /// <summary>
        /// Holds a reference to the game system instance.
        /// </summary>
        GameSystem _game_system = null!;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        /// <summary>
        /// Fires once when the player has been despawned from the scene.
        /// (past participle: a single, completed happening)
        /// </summary>
        public event Action? Despawned;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

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
            ///   2. Emit "sig_despawn" signal so Store can react (counters, flags).
            /// Both LoadScene calls targeting the same scene are idempotent in Unity.
            /// </summary>
            this.OnTriggerEnterAsObservable()
                .Where(predicate: x =>
                    x.Like(type: PLAYER_TYPE))
                .Subscribe(onNext: _ => {
                    Despawned?.Invoke();
                    LoadScene(sceneName: GetActiveScene().name);
                    _game_system.Bus?.Publish(signal_id: "sig_despawn");
                }).AddTo(gameObjectComponent: this);
        }
    }
}