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
    /// Handles player despawn logic, including scene reloads.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Despawn : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        /// <summary>
        /// Reference to the game system.
        /// </summary>
        GameSystem _game_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        /// <summary>
        /// Occurs when the player despawns.
        /// </summary>
        public event Action? OnDespawn;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
        }

        // Start is called before the first frame update.
        void Start() {
            /// <summary>
            /// When the player touches the object.
            /// </summary>
            this.OnTriggerEnterAsObservable()
                .Where(predicate: x =>
                    x.Like(type: PLAYER_TYPE))
                .Subscribe(onNext: _ => {
                    OnDespawn?.Invoke();
                    LoadScene(sceneName: GetActiveScene().name);
                }).AddTo(gameObjectComponent: this);
        }
    }
}