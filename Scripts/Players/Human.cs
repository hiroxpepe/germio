// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using static System.Math;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.Vector3;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;
using static Germio.Utils;

namespace Germio {
    /// <summary>
    /// Controls the Human player, including movement and interactions.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Human : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        /// <summary>
        /// Jump power of the player.
        /// </summary>
        [SerializeField] protected float _JUMP_POWER = 10.0f;

        /// <summary>
        /// Rotational speed of the player.
        /// </summary>
        [SerializeField] protected float _ROTATIONAL_SPEED = 10.0f;

        /// <summary>
        /// Forward speed limit of the player.
        /// </summary>
        [SerializeField] protected float _FORWARD_SPEED_LIMIT = 1.5f;

        /// <summary>
        /// Running speed limit of the player.
        /// </summary>
        [SerializeField] protected float _RUN_SPEED_LIMIT = 3.25f;

        /// <summary>
        /// Backward speed limit of the player.
        /// </summary>
        [SerializeField] protected float _BACKWARD_SPEED_LIMIT = 1.0f;

        /// <summary>
        /// Animation component for the player.
        /// </summary>
        [SerializeField] protected SimpleAnimation _simple_anime;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        /// <summary>
        /// Handles update logic.
        /// </summary>
        protected DoUpdate _do_update;

        /// <summary>
        /// Handles fixed update logic.
        /// </summary>
        protected DoFixedUpdate _do_fixed_update;

        /// <summary>
        /// Handles acceleration logic.
        /// </summary>
        protected Acceleration _acceleration;

        /// <summary>
        /// Stores the player's position from previous frames.
        /// </summary>
        protected Vector3[] _previous_position = new Vector3[60];

        /// <summary>
        /// Reference to the game system.
        /// </summary>
        protected GameSystem _game_system;

        /// <summary>
        /// Reference to the sound system.
        /// </summary>
        protected SoundSystem _sound_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Properties [noun, adjectives]

        /// <summary>
        /// Transform position.
        /// </summary>
        public Vector3 position { 
            get => transform.position; 
            set { 
                transform.position = value; 
                Updated?.Invoke(sender: this, e: new(nameof(position)));
            }
        }

        /// <summary>
        /// Transform rotation.
        /// </summary>
        public Quaternion rotation { 
            get => transform.rotation; 
            set { 
                transform.rotation = value; 
                Updated?.Invoke(sender: this, e: new(nameof(rotation)));
            }
        }

        /// <summary>
        /// Indicates whether the player is facing a surface.
        /// </summary>
        public bool Faceing { get => _do_update.faceing; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Events [verb, verb phrase]

        /// <summary>
        /// On grounded event handler.
        /// </summary>
        public event Action? OnGrounded;

        /// <summary>
        /// Changed event handler.
        /// </summary>
        public event Changed? Updated;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _do_update = DoUpdate.GetInstance();
            _do_fixed_update = DoFixedUpdate.GetInstance();
            _acceleration = Acceleration.GetInstance(this);
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
            _sound_system = Find(name: SOUND_SYSTEM).Get<SoundSystem>();

            /// <summary>
            /// Sets load Methods handler.
            /// </summary>
            abilities_OnAwake();
        }

        /// <summary>
        /// Called before the first frame update.
        /// </summary>
        new void Start() {
            base.Start();

            const float ADD_FORCE_VALUE = 12.0f;

            /// <remarks>
            /// Rigidbody should be only used in FixedUpdate.
            /// </remarks>
            Rigidbody rb = transform.Get<Rigidbody>();

            // FIXME: to integrate with Energy function.
            this.FixedUpdateAsObservable()
                .Subscribe(onNext: _ => {
                    _acceleration.previousSpeed = _acceleration.currentSpeed;// hold previous speed.
                    _acceleration.currentSpeed = rb.linearVelocity.magnitude; // get speed.
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Idol
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    !_up_button.isPressed && !_down_button.isPressed &&
                    _do_update.ready)
                .Subscribe(onNext: _ => {
                    _simple_anime.Play(stateName: "Default");
                    _sound_system.StopSEClip();
                    _do_fixed_update.Apply(type: FixedUpdate.Idol);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    !_do_update.climbing && _do_fixed_update.idol)
                .Subscribe(onNext: _ => {
                    rb.useGravity = true;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Walk
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _up_button.isPressed && !_y_button.isPressed && !_do_update.virtualControllerMode &&
                    _do_update.ready)
                .Subscribe(onNext: _ => {
                    if (_do_update.grounded) { 
                        _simple_anime.Play(stateName: "Walk");
                        _sound_system.Play(type: SEClip.Walk) ;
                    }
                    _do_fixed_update.Apply(type: FixedUpdate.Walk);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    _do_fixed_update.walk && _acceleration.canWalk)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 7.5f;
                    rb.AddFor​​ce(force: transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                    _do_fixed_update.Cancel(type: FixedUpdate.Walk);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Run
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _up_button.isPressed && (_y_button.isPressed || _do_update.virtualControllerMode) &&
                    _do_update.readyForAnyGround)
                .Subscribe(onNext: _ => {
                    if (_do_update.grounded && !_do_update.climbing) { 
                        _simple_anime.Play(stateName: "Run");
                        _sound_system.Play(type: SEClip.Run);
                    }
                    _do_fixed_update.Apply(type: FixedUpdate.Run);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    _do_fixed_update.run && _acceleration.canRun)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 7.5f;
                    rb.AddFor​​ce(force: transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                    _do_fixed_update.Cancel(type: FixedUpdate.Run);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Backward
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ =>  
                    _down_button.isPressed &&
                    _do_update.ready)
                .Subscribe(onNext: _ => {
                    _simple_anime.Play(stateName: "Walk");
                    _sound_system.Play(type: SEClip.Walk);
                    _do_fixed_update.Apply(type: FixedUpdate.Backward);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    _do_fixed_update.backward && _acceleration.canBackward)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 7.5f;
                    rb.AddFor​​ce(force: -transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                    _do_fixed_update.Cancel(type: FixedUpdate.Backward);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Stop TODO: stop anime fbx.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    (_up_button.wasReleasedThisFrame || _down_button.wasReleasedThisFrame) &&
                    _do_update.ready)
                .Subscribe(onNext: _ => {
                    //_simple_anime.Play("Stop");
                    _do_fixed_update.Apply(type: FixedUpdate.Stop);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    _do_fixed_update.stop)
                .Subscribe(onNext: _ => {
                    rb.linearVelocity = new(x: 0f, y: 0f, z: 0f);
                    _do_fixed_update.Cancel(type: FixedUpdate.Stop);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// On virtual controller mode.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _y_button.wasReleasedThisFrame && useVirtualController)
                .Subscribe(onNext: _ => {
                    _do_update.virtualControllerMode = true;
                    Observable.TimerFrame(dueTimeFrameCount: 45)
                        .Subscribe(onNext: _ => {
                            _do_update.virtualControllerMode = false;
                        }).AddTo(this);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Jump
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _b_button.wasPressedThisFrame &&
                    _do_update.ready)
                .Subscribe(onNext: _ => {
                    _do_update.grounded = false;
                    _simple_anime.Play(stateName: "Jump");
                    _sound_system.Play(type: SEClip.Jump);
                    _do_fixed_update.Apply(type: FixedUpdate.Jump);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    _do_fixed_update.jump)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 2.0f;
                    rb.useGravity = true;
                    rb.AddRelativeFor​​ce(force: up * _acceleration.jumpPower * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                    _do_fixed_update.Cancel(type: FixedUpdate.Jump);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Abort jump.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _b_button.wasReleasedThisFrame &&
                    !_do_update.grounded &&
                    continueUpdate)
                .Subscribe(onNext: _ => {
                    _do_fixed_update.Apply(type: FixedUpdate.AbortJump);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    !_do_update.climbing && _do_fixed_update.abortJump)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 0.05f;
                    Observable.Timer(TimeSpan.FromSeconds(value: ADJUST_VALUE))
                        .Subscribe(onNext: _ => {
                            if (!isDown()) {
                                rb.useGravity = true;
                                Vector3 velocity = rb.linearVelocity;
                                rb.linearVelocity = new(x: velocity.x, y: 0, z: velocity.z);
                            }
                            _do_fixed_update.Cancel(type: FixedUpdate.AbortJump);
                        }).AddTo(gameObjectComponent: this);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Rotate(yaw).
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _do_update.readyForAnyGround)
                .Subscribe(onNext: _ => {
                    int axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                    transform.Rotate(
                        xAngle: 0, 
                        yAngle: axis * (_ROTATIONAL_SPEED * Time.deltaTime) * ADD_FORCE_VALUE, 
                        zAngle: 0
                    );
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Freeze anime.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _game_system.home)
                .Subscribe(onNext: _ => {
                    _simple_anime.enabled = false;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// When touching blocks.
            /// TODO: to Block ?
            /// </summary>
            this.OnCollisionEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: BLOCK_TYPE) &&
                    !gameObject.isHitSide(target: x.gameObject) && 
                    !_do_update.climbing)
                .Subscribe(onNext: x => {
                    _sound_system.Play(type: SEClip.Grounded);
                    _do_update.grounded = true;
                    rb.useGravity = true;
                    rb.linearVelocity = new(x: 0f, y: 0f, z: 0f);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// When leaving blocks.
            /// TODO: to Block ?
            /// </summary>
            this.OnCollisionExitAsObservable()
                .Where(predicate: x => 
                    x.Like(type: BLOCK_TYPE) && 
                    !_do_update.climbing)
                .Subscribe(onNext: x => {
                    rb.useGravity = true;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// When touching grounds.
            /// </summary>
            this.OnCollisionEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: GROUND_TYPE))
                .Subscribe(onNext: x => {
                    _do_update.grounded = true;
                    if (isUpOrDown()) {
                        _sound_system.Play(type: SEClip.Grounded);
                        rb.useGravity = true;
                        rb.linearVelocity = new(x: 0f, y: 0f, z: 0f);
                        // Resets rotate.
                        Vector3 angle = transform.eulerAngles;
                        angle.x = angle.z = 0f;
                        transform.eulerAngles = angle;
                        // Calls event handler.
                        OnGrounded?.Invoke();
                    }
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Freeze
            /// </summary>
            this.OnCollisionStayAsObservable()
                .Where(predicate: x => 
                    (x.Like(type: GROUND_TYPE) || x.Like(type: BLOCK_TYPE)) && 
                    gameObject.isHitSide(target: x.gameObject) && 
                    (_up_button.isPressed || _down_button.isPressed) && 
                    !_do_update.climbing && !_do_update.faceing && !_do_update.pushing && _acceleration.freeze)
                .Subscribe(onNext: x => {
                    double reach = gameObject.getReach(target: x.gameObject); // FIXME: Case the block size is other than 1.
                    // Moves left or right.
                    if (_do_update.grounded && (reach < 0.5d || reach >= 0.99d)) {
                        gameObject.moveLetfOrRight(direction: GetDirection(forward_vector: transform.forward));
                        rb.useGravity = true;
                    }
                    // Forcibly moves up.
                    else if (reach >= 0.5d && reach < 0.99d) {
                        rb.useGravity = false;
                        gameObject.moveUp();
                        _do_update.grounded = true;
                        rb.useGravity = true;
                    }
                    // Forcibly moves down.
                    else {
                        gameObject.moveDown();
                        _do_update.grounded = true;
                        rb.useGravity = true;
                    }
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Sets update methods handler.
            /// </summary>
            abilities_OnStart();

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable()
                .Subscribe(onNext: _ => {
                    position = transform.position;
                    rotation = transform.rotation;
                    cashPreviousPosition();
                }).AddTo(gameObjectComponent: this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods handler.

        /// <summary>
        /// Load methods handler.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// Update methods handler.
        /// </summary>
        protected virtual void abilities_OnStart() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Protected Properties [noun, adjectives]

        /// <summary>
        /// Indicates whether updates should continue.
        /// </summary>
        protected bool continueUpdate {
            get {
                return !_look && !_do_update.pushing;
            }
        } 

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Private Methods [verb]

        /// <summary>
        /// Saves position value for the previous n frame.
        /// </summary>
        void cashPreviousPosition() {
            for (int i = _previous_position.Length - 1; i > -1; i--) {
                if (i > 0) {
                    _previous_position[i] = _previous_position[i - 1];
                } else if (i == 0) {
                    _previous_position[i] = new Vector3(
                        (float) Round(transform.position.x, 3),
                        (float) Round(transform.position.y, 3),
                        (float) Round(transform.position.z, 3)
                    );
                }
            }
        }

        /// <summary>
        /// Whether there was an up or down movement.
        /// </summary>
        bool isUpOrDown() {
            int fps = Application.targetFrameRate;
            int ADJUST_VALUE = 9;
            if (fps == 60) ADJUST_VALUE = 9;
            if (fps == 30) ADJUST_VALUE = 20;
            float current_y = (float) Round(transform.position.y, 1, MidpointRounding.AwayFromZero);
            float previous_y = (float) Round(_previous_position[ADJUST_VALUE].y, 1, MidpointRounding.AwayFromZero);
            if (current_y == previous_y) {
                return false;
            } else if (current_y != previous_y) {
                return true;
            } else {
                return true;
            }
        }

        /// <summary>
        /// Whether there was a down movement.
        /// </summary>
        bool isDown() {
            int fps = Application.targetFrameRate;
            int ADJUST_VALUE = 9;
            if (fps == 60) ADJUST_VALUE = 9;
            if (fps == 30) ADJUST_VALUE = 20;
            float current_y = (float) Round(transform.position.y, 1, MidpointRounding.AwayFromZero);
            float previous_y = (float) Round(_previous_position[ADJUST_VALUE].y, 1, MidpointRounding.AwayFromZero);
            if (current_y > previous_y) {
                return false;
            } else {
                return true;
            }
        }

        /// <summary>
        /// Faces the surface directly.
        /// </summary>
        void faceToFace(float speed = 20.0f) {
            float SPEED = speed; // Rotation speed.
            float forward_x = (float) Round(transform.forward.x);
            float forward_z = (float) Round(transform.forward.z);
            if (forward_x == 0 && forward_z == 1) { // Positive Z-axis.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, 0), SPEED * Time.deltaTime); // Gradually rotate.
            } else if (forward_x == 0 && forward_z == -1) { // Negative Z-axis.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 180, 0), SPEED * Time.deltaTime); // Gradually rotate.
            } else if (forward_x == 1 && forward_z == 0) { // Positive X-axis.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 90, 0), SPEED * Time.deltaTime); // Gradually rotate.
            } else if (forward_x == -1 && forward_z == 0) { // Negative X-axis.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 270, 0), SPEED * Time.deltaTime); // Gradually rotate.
            }
        }

        /// <summary>
        /// Changed event handler from energy.
        /// </summary>
        void onChanged(object sender, EvtArgs  e) {
        }
    }
}