// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using static System.Math;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.Mathf;
using static UnityEngine.Quaternion;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;
using static Germio.Utils;

namespace Germio {
    /// <summary>
    /// common controller
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Common : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] protected bool _CAN_HOLD = false;

        [SerializeField] protected float _HOLD_ADJUST_Y = 0.6f;

        [SerializeField] protected float _HOLD_ADJUST_X_OR_Z = 0.8f;

        [SerializeField] protected float _HOLD_ADJUST_DEGREE = 15.0f;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        protected bool _is_grounded;

        Transform _left_hand_transform, _right_hand_transform;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjectives]

        public bool holdable { get => _CAN_HOLD; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        public Transform GetLeftHandTransform() {
            return _left_hand_transform;
        }

        public Transform GetRightHandTransform() {
            return _right_hand_transform;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        protected void Start() {
            /// <summary>
            /// be holded.
            /// </summary>
            if (_CAN_HOLD) {
                _is_grounded = true;
                // initializes the hand position object when being held by its parent.
                GameObject left_hand_game_object = new(name: "LeftHand");
                GameObject right_hand_game_object = new(name: "RightHand");
                _left_hand_transform = left_hand_game_object.transform;
                _right_hand_transform = right_hand_game_object.transform;
                _left_hand_transform.parent = transform;
                _right_hand_transform.parent = transform;
                _left_hand_transform.localPosition = new(x: 0f, y: 0f, z: 0f);
                _right_hand_transform.localPosition = new(x: 0f, y: 0f, z: 0f);
            }

            /// <summary>
            /// sets the grounded flag off when the player becomes the parent.
            /// </summary>
            this.FixedUpdateAsObservable()
                .Where(predicate: x => 
                    _CAN_HOLD && 
                    transform.parent != null && transform.parent.gameObject.Like(type: PLAYER_TYPE) &&
                    _is_grounded)
                .Subscribe(onNext: x => {
                    _is_grounded = false;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// be lifted when the player keeps the parent.
            /// </summary>
            this.FixedUpdateAsObservable()
                .Where(predicate: x => 
                    _CAN_HOLD && 
                    transform.parent != null && transform.parent.gameObject.Like(type: PLAYER_TYPE) &&
                    !_is_grounded)
                .Subscribe(onNext: x => {
                    if (transform.parent.transform.position.y > transform.position.y + 0.2f) { // 0.2f: adjustment value
                        beHolded(speed: 8.0f); // lifted from above.
                    } else {
                        beHolded(); // lifted from the side.
                    }
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// falls when the player is no longer a parent.
            /// </summary>
            this.FixedUpdateAsObservable()
                .Where(predicate: x =>
                    _CAN_HOLD &&
                    (transform.parent == null || !transform.parent.gameObject.Like(type: PLAYER_TYPE)) &&
                    !_is_grounded)
                .Subscribe(onNext: x => {
                    Ray ray = new(transform.position, new(x: 0, y: -1f, z: 0)); // creates a ray to search downwards.
                    // when throwing a ray down and getting a reaction.
                    if (Physics.Raycast(ray: ray, hitInfo: out RaycastHit raycast_hit, maxDistance: 20f)) { // 20f: adjustment value
#if DEBUG
                        Debug.DrawRay(start: ray.origin, dir: ray.direction, color: Color.yellow, duration: 3, depthTest: false);
#endif
                        float distance = (float) Round(value: raycast_hit.distance, digits: 3, mode: MidpointRounding.AwayFromZero);
                        if (distance < 0.2f) { // the distance is close.
                            _is_grounded = true;
                            // gets the top position of being hit object.
                            float top = getTopOf(target: raycast_hit.transform.gameObject);
                            transform.localPosition = SwapLocalPositionY(transform: transform, value: top); // put it in that position.
                            alignAfterHold(); // adjusts position.
                        }
                    }
                    // falls when not touched the ground yet.
                    if (!_is_grounded) {
                        transform.localPosition -= new Vector3(x: 0f, y: 5.0f * Time.deltaTime, z: 0f); // 5.0f: adjustment value
                    }
                }).AddTo(gameObjectComponent: this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Methods [verb]

        /// <summary>
        /// get the player's pressed direction as an enumeration.
        /// </summary>
        protected Direction getPushedDirection(Vector3 forward_vector) {
            float forward_x = (float) Round(a: forward_vector.x);
            float forward_y = (float) Round(a: forward_vector.y);
            float forward_z = (float) Round(a: forward_vector.z);
            if (forward_x == 0 && forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
            if (forward_x == 0 && forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            if (forward_x == 1 && forward_z == 0) { return Direction.PositiveX; } // x-axis negative.
            if (forward_x == -1 && forward_z == 0) { return Direction.NegativeX; } // x-axis negative.
            // determine the difference between the two axes.
            float absolute_x = Abs(value: forward_vector.x);
            float absolute_z = Abs(value: forward_vector.z);
            if (absolute_x > absolute_z) {
                if (forward_x == 1) { return Direction.PositiveX; } // x-axis positive.
                if (forward_x == -1) { return Direction.NegativeX; } // x-axis negative.
            } else if (absolute_x < absolute_z) {
                if (forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
                if (forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            }
            return Direction.None; // unknown.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// moves to a position where being lifted by its parent.
        /// </summary>
        void beHolded(float speed = 2.0f) {
            if (transform.localPosition.y < _HOLD_ADJUST_Y) {
                Direction direction = getPushedDirection(transform.parent.forward);
                // z-axis positive.
                if (direction == Direction.PositiveZ) {
                    transform.position = new(
                        x: transform.parent.transform.position.x,
                        y: transform.position.y + speed * Time.deltaTime,
                        z: transform.parent.transform.position.z + _HOLD_ADJUST_X_OR_Z
                    );
                    transform.rotation = Euler(x: -_HOLD_ADJUST_DEGREE, y: 0f, z: 0f);
                // z-axis negative.
                } else if (direction == Direction.NegativeZ) {
                    transform.position = new(
                        x: transform.parent.transform.position.x,
                        y: transform.position.y + speed * Time.deltaTime,
                        z: transform.parent.transform.position.z - _HOLD_ADJUST_X_OR_Z
                    );
                    transform.rotation = Euler(x: _HOLD_ADJUST_DEGREE, y: 0f, z: 0f);
                // x-axis positive.
                } else if (direction == Direction.PositiveX) {
                    transform.position = new(
                        x: transform.parent.transform.position.x + _HOLD_ADJUST_X_OR_Z,
                        y: transform.position.y + speed * Time.deltaTime,
                        z: transform.parent.transform.position.z
                    );
                    transform.rotation = Euler(x: 0f, y: 0f, z: _HOLD_ADJUST_DEGREE);
                // x-axis negative.
                } else if (direction == Direction.NegativeX) {
                    transform.position = new(
                        x: transform.parent.transform.position.x - _HOLD_ADJUST_X_OR_Z,
                        y: transform.position.y + speed * Time.deltaTime,
                        z: transform.parent.transform.position.z
                    );
                    transform.rotation = Euler(x: 0f, y: 0f, z: -_HOLD_ADJUST_DEGREE);
                }
            }
        }

        /// <summary>
        /// gets the top position of the target object.
        float getTopOf(GameObject target) {
            float height = target.Get<Renderer>().bounds.size.y;
            float position_y = target.transform.position.y;
            return height + position_y;
        }

        /// <summary>
        /// fine tune the position of the block to fit the grid.
        /// </summary>
        void alignAfterHold() {
            transform.position = new(
                x: Round(f: transform.position.x / 0.25f) * 0.25f,
                y: Round(f: transform.position.y / 0.25f) * 0.25f,
                z: Round(f: transform.position.z / 0.25f) * 0.25f
            );
            transform.localRotation = new Quaternion(x: 0, y: 0f, z: 0f, w: 0f);
        }
    }
}