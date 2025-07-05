// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Vector3;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;

namespace Germio {
    /// <summary>
    /// Controls the camera system, including rotation and transparency effects.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class CameraSystem : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        /// <summary>
        /// Gets the horizontal axis object for camera rotation.
        /// </summary>
        [SerializeField] GameObject _horizontal_axis;

        /// <summary>
        /// Gets the vertical axis object for camera rotation.
        /// </summary>
        [SerializeField] GameObject _vertical_axis;

        /// <summary>
        /// Gets the main camera object.
        /// </summary>
        [SerializeField] GameObject _main_camera;

        /// <summary>
        /// Gets the target object for camera focus.
        /// </summary>
        [SerializeField] GameObject _look_target;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        /// <summary>
        /// Gets the default local position of the camera.
        /// </summary>
        Vector3 _default_local_position;

        /// <summary>
        /// Gets the default local rotation of the camera.
        /// </summary>
        Quaternion _default_local_rotation;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            /// <summary>
            /// Saves the camera's default local position and rotation.
            /// </summary>
            _default_local_position = transform.localPosition;
            _default_local_rotation = transform.localRotation;

            /// <summary>
            /// Enables or resets the look-around camera mode.
            /// </summary>
            this.UpdateAsObservable()
                .Subscribe(_ => {
                    if (_x_button.wasReleasedThisFrame) {
                        resetLookAround();
                        _look = false;
                        return;
                    }
                    if (_x_button.isPressed) {
                        _look = true;
                        lookAround();
                        return;
                    }
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Rotates the camera view each frame.
            /// </summary>
            this.UpdateAsObservable()
                .Subscribe(onNext: _ => {
                    rotateView();
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Resets the camera view to its default state.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _right_stick_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    resetRotateView();
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Makes the wall transparent when the camera collides with it.
            /// </summary>
            this.OnTriggerEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: WALL_TYPE))
                .Subscribe(onNext: x => {
                    List<Material> material_list = x.gameObject.Get<MeshRenderer>().materials.ToList();
                    material_list.ForEach(action: x => { x.ToTransparent(); });
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Restores the wall to opaque when the camera leaves it.
            /// </summary>
            this.OnTriggerExitAsObservable()
                .Where(predicate: x => 
                    x.Like(type: WALL_TYPE))
                .Subscribe(onNext: x => {
                    List<Material> material_list = x.gameObject.Get<MeshRenderer>().materials.ToList();
                    material_list.ForEach(action: x => { x.ToOpaque(); });
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Makes the ground transparent when the camera collides with it.
            /// </summary>
            this.OnTriggerEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: GROUND_TYPE))
                .Subscribe(onNext: x => {
                    List<Material> material_list = x.gameObject.Get<MeshRenderer>().materials.ToList();
                    material_list.ForEach(action: x => { x.ToTransparent(); });
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Restores the ground to opaque when the camera leaves it.
            /// </summary>
            this.OnTriggerExitAsObservable()
                .Where(predicate: x => 
                    x.Like(type: GROUND_TYPE))
                .Subscribe(onNext: x => {
                    List<Material> material_list = x.gameObject.Get<MeshRenderer>().materials.ToList();
                    material_list.ForEach(action: x => { x.ToOpaque(); });
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Makes the block transparent when the camera collides with it.
            /// </summary>
            this.OnTriggerEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: BLOCK_TYPE))
                .Subscribe(onNext: x => {
                    List<Material> material_list = x.gameObject.Get<MeshRenderer>().materials.ToList();
                    material_list.ForEach(action: x => { x.ToTransparent(); });
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Restores the block to opaque when the camera leaves it.
            /// </summary>
            this.OnTriggerExitAsObservable()
                .Where(predicate: x => 
                    x.Like(type: BLOCK_TYPE))
                .Subscribe(onNext: x => {
                    List<Material> material_list = x.gameObject.Get<MeshRenderer>().materials.ToList();
                    material_list.ForEach(action: x => { x.ToOpaque(); });
                }).AddTo(gameObjectComponent: this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Rotates the camera view based on the input.
        /// </summary>
        void rotateView() {
            const float ADJUST = 120.0f;
            Vector3 player_position = transform.parent.gameObject.transform.position;
            // Rotate left.
            if (_right_stick_left_button.isPressed) {
                transform.RotateAround(point: player_position, axis: up, angle: 1.0f * ADJUST * Time.deltaTime);
            }
            // Rotate right.
            else if (_right_stick_right_button.isPressed) {
                transform.RotateAround(point: player_position, axis: up, angle: -1.0f * ADJUST * Time.deltaTime);
            }
        }

        /// <summary>
        /// Resets the camera view to its default position and rotation.
        /// </summary>
        void resetRotateView() {
            transform.localPosition = _default_local_position;
            transform.localRotation = _default_local_rotation;
        }

        /// <summary>
        /// Resets the camera and the look-around state to its initial values.
        /// </summary>
        void resetLookAround() {
            transform.localPosition = _default_local_position;
            transform.localRotation = _default_local_rotation;
            _horizontal_axis.transform.localRotation = new Quaternion(x: 0f, y: 0f, z: 0f, w: 0f);
            _vertical_axis.transform.localRotation = new Quaternion(x: 0f, y: 0f, z: 0f, w: 0f);
        }

        /// <summary>
        /// Allows the player to look around using the up, down, left, and right buttons.
        /// </summary>
        void lookAround() {
            const float ADJUST = 80.0f;
            transform.localEulerAngles = new(x: 0f, y: 0f, z: 0f); // Keep the camera system horizontally fixed.
            // Look up.
            if (_up_button.isPressed) {
                _vertical_axis.transform.Rotate(xAngle: 1.0f * Time.deltaTime * ADJUST, yAngle: 0f, zAngle: 0f);
            }
            // Look down.
            else if (_down_button.isPressed) {
                _vertical_axis.transform.Rotate(xAngle: -1.0f * Time.deltaTime * ADJUST, yAngle: 0f, zAngle: 0f);
            }
            // Look left.
            else if (_left_button.isPressed) {
                _horizontal_axis.transform.Rotate(xAngle: 0f, yAngle: -1.0f * Time.deltaTime * ADJUST, zAngle: 0f);
            }
            // Look right.
            else if (_right_button.isPressed) {
                _horizontal_axis.transform.Rotate(xAngle: 0f, yAngle: 1.0f * Time.deltaTime * ADJUST, zAngle: 0f);
            }
            // Moves the camera towards the character's eyes if it's too close.
            if (transform.localPosition.z < 0.1f) {
                transform.localPosition += new Vector3(x: 0f,  y: -0.01f, z: 0.075f * Time.deltaTime * ADJUST);
            }
        }
    }
}