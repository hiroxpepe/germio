// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.Mathf;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;

namespace Germio {
    /// <summary>
    /// Represents an home object in the game.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Home : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        /// <summary>
        /// Indicates whether the object is moving.
        /// </summary>
        [SerializeField] bool _move = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        /// <summary>
        /// Reference to the game system.
        /// </summary>
        GameSystem _game_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        /// <summary>
        /// Occurs when the player comes back home.
        /// </summary>
        public event Action? OnCameBack;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
        }

        // Start is called before the first frame update.
        void Start() {
            // Saves the original y position of the object.
            float original_position = transform.position.y;
            // Updates the object's position if moving.
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _move)
                .Subscribe(onNext: _ => {
                    const int SPEED = 5; const float DISTANCE = 0.3f; 
                    transform.position = new(
                        x: transform.position.x, 
                        y: original_position + PingPong(Time.time / SPEED, DISTANCE), 
                        z: transform.position.z
                    );
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// When player collides with the object.
            /// </summary>
            this.OnCollisionEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: PLAYER_TYPE))
                .Subscribe(onNext: _ => {
                    OnCameBack?.Invoke();
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// When player triggers the object.
            /// </summary>
            this.OnTriggerEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: PLAYER_TYPE))
                .Subscribe(onNext: _ => {
                    OnCameBack?.Invoke();
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// When vehicle collides with the object.
            /// </summary>
            this.OnCollisionEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: VEHICLE_TYPE) && 
                    _game_system.beat)
                .Subscribe(onNext: _ => {
                    OnCameBack?.Invoke();
                }).AddTo(gameObjectComponent: this);
        }
    }
}