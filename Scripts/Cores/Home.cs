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
    /// The Home class
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Home : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] bool _move = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameSystem _game_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Events [verb, verb phrase]

        public event Action? OnCameBack;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods

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