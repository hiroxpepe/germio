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
                _is_grounded = true; // false; MEMO: 初期設定の位置そのまま？
                // 持たれる時の手の位置オブジェクト初期化
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
            /// 持たれる実装用: 親が Player になった時
            /// </summary>
            this.FixedUpdateAsObservable()
                .Where(predicate: x => 
                    _CAN_HOLD && 
                    transform.parent != null && transform.parent.gameObject.Like(type: PLAYER_TYPE) &&
                    _is_grounded)
                .Subscribe(onNext: x => {
                    Debug.Log("hold step 8: 親が Player になった時");
                    _is_grounded = false; // 接地フラグOFF
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 持たれる実装用: 親が Player 継続なら
            /// </summary>
            this.FixedUpdateAsObservable()
                .Where(predicate: x => 
                    _CAN_HOLD && 
                    transform.parent != null && transform.parent.gameObject.Like(type: PLAYER_TYPE) &&
                    !_is_grounded)
                .Subscribe(onNext: x => {
                    Debug.Log("hold step 9: 親が Player 継続なら");
                    //if (!transform.parent.Get<Player>().Faceing) { // プレイヤーの移動・回転を待つ
                        Debug.Log("hold step 10: 親 Player Faceing 完了");
                        if (transform.parent.transform.position.y > transform.position.y + 0.2f) { // 0.2fは調整値
                            beHolded(speed: 8.0f); // 上から持ち上げられる
                        } else {
                            beHolded(); // 横から持ち上げられる
                        }
                    //}
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// 持たれる実装用: 親が Player でなくなれば落下する
            /// </summary>
            this.FixedUpdateAsObservable()
                .Where(predicate: x =>
                    _CAN_HOLD &&
                    (transform.parent == null || !transform.parent.gameObject.Like(type: PLAYER_TYPE)) &&
                    !_is_grounded)
                .Subscribe(onNext: x => {
                    Debug.Log("hold step 11: 親が Player でなくなれば落下する");
                    Ray ray = new(transform.position, new(x: 0, y: -1f, z: 0)); // 下方サーチするレイ作成
                    if (Physics.Raycast(ray: ray, hitInfo: out RaycastHit hit, maxDistance: 20f)) { // 下方にレイを投げて反応があった場合
#if DEBUG
                        Debug.DrawRay(ray.origin, ray.direction, Color.yellow, 3, false);
#endif
                        float distance = (float) Round(value: hit.distance, digits: 3, mode: MidpointRounding.AwayFromZero);
                        if (distance < 0.2f) { // ある程度距離が近くなら
                            _is_grounded = true; // 接地とする
                            float top = getHitTop(hit: hit.transform.gameObject); // その後、接地したとするオブジェクトのTOPを調べて
                            transform.localPosition = ReplaceLocalPositionY(t: transform, value: top); // その位置に置く
                            alignAfterHold(); // 位置調整
                        }
                    }
                    if (!_is_grounded) { // まだ接地してなければ落下する
                        transform.localPosition -= new Vector3(x: 0f, y: 5.0f * Time.deltaTime, z: 0f); // 5.0f は調整値
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
        /// プレイヤーに持ち上げられる。
        /// </summary>
        void beHolded(float speed = 2.0f/*, float hold_adjust_y = 0.6f, float hold_adjust_x_or_z = 0.8f, float hold_adjust_degree = 15.0f*/) {

            //Vector3 size = gameObject.Get<BoxCollider>().size;
            //Debug.Log($"size: x:{size.x} y:{size.y} z:{size.z}");
            //hold_adjust_y = hold_adjust_y * size.x;
            //hold_adjust_x_or_z = hold_adjust_x_or_z * 0.75f;// size.x;

            if (transform.localPosition.y < _HOLD_ADJUST_Y) { // 親に持ち上げられた位置に移動する: 0.6fは調整値
                Direction direction = getPushedDirection(transform.parent.forward);
                // z-axis positive.
                if (direction == Direction.PositiveZ) {
                    transform.position = new(
                        x: transform.parent.transform.position.x,
                        y: transform.position.y + speed * Time.deltaTime, // 調整値
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
                } else if (direction == Direction.NegativeX) { // X軸負方向
                    transform.position = new(
                        x: transform.parent.transform.position.x - _HOLD_ADJUST_X_OR_Z,
                        y: transform.position.y + speed * Time.deltaTime,
                        z: transform.parent.transform.position.z
                    );
                    transform.rotation = Euler(x: 0f, y: 0f, z: -_HOLD_ADJUST_DEGREE);
                }
            }
        }

        // 衝突したオブジェクトの側面に当たったか判定する
        float getHitTop(GameObject hit) {
            float height = hit.Get<Renderer>().bounds.size.y; // 対象オブジェクトの高さ取得 
            float position_y = hit.transform.position.y; // 対象オブジェクトのy座標取得(※0基点)
            float top = height + position_y; // 対象オブジェクトのTOP取得
            return top;
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