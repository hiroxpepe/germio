// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using static System.Math;
using UnityEngine;
using UnityEngine.Serialization;
using static UnityEngine.GameObject;
using static UnityEngine.Vector3;
using UniRx;
using UniRx.Triggers;
using static Germio.Env;
using static Germio.Utils;
using Germio;
using Germio.Systems;

namespace Germio.Players {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Controls the Human player, including movement and interactions.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Human : InputMapper {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Fields

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        /// <summary>
        /// Gets the jump power of the player.
        /// </summary>
        [SerializeField] protected float _JUMP_POWER = 10.0f;

        /// <summary>
        /// Gets the rotational speed of the player.
        /// </summary>
        [SerializeField] protected float _ROTATIONAL_SPEED = 10.0f;

        /// <summary>
        /// Gets the forward speed limit of the player.
        /// </summary>
        [SerializeField] protected float _FORWARD_SPEED_LIMIT = 1.5f;

        /// <summary>
        /// Gets the running speed limit of the player.
        /// </summary>
        [SerializeField] protected float _RUN_SPEED_LIMIT = 3.25f;

        /// <summary>
        /// Gets the backward speed limit of the player.
        /// </summary>
        [SerializeField] protected float _BACKWARD_SPEED_LIMIT = 1.0f;

        /// <summary>
        /// Gets the animation component for the player.
        /// </summary>
        [FormerlySerializedAs("_simple_anime")]
        [SerializeField] protected SimpleAnimation SimpleAnime;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Fields

        /// <summary>
        /// Handles update logic for the player.
        /// </summary>
        protected Human_DoUpdate DoUpdate;

        /// <summary>
        /// Handles fixed update logic for the player.
        /// </summary>
        protected Human_DoFixedUpdate DoFixedUpdate;

        /// <summary>
        /// Handles acceleration logic for the player.
        /// </summary>
        protected Human_Acceleration Acceleration;

        /// <summary>
        /// Stores the player's positions from previous frames.
        /// </summary>
        protected Vector3[] PreviousPosition = new Vector3[60];

        /// <summary>
        /// Gets the reference to the game system.
        /// </summary>
        protected GameSystem GameSystem;

        /// <summary>
        /// Gets the reference to the sound system.
        /// </summary>
        protected SoundSystem SoundSystem;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        /// <summary>
        /// Occurs when the player is grounded.
        /// </summary>
        public event Action? OnGrounded;

        /// <summary>
        /// Occurs when the player state is updated.
        /// </summary>
        public event Changed? Updated;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>
        /// Gets or sets the transform position of the player.
        /// </summary>
        public Vector3 Position { 
            get => transform.position; 
            set { 
                transform.position = value; 
                Updated?.Invoke(sender: this, e: new(nameof(Position)));
            }
        }

        /// <summary>
        /// Gets or sets the transform rotation of the player.
        /// </summary>
        public Quaternion Rotation { 
            get => transform.rotation; 
            set { 
                transform.rotation = value; 
                Updated?.Invoke(sender: this, e: new(nameof(Rotation)));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is facing a surface.
        /// </summary>
        public bool Facing { get => DoUpdate.Facing; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Properties [noun, adjective]

        /// <summary>
        /// Gets a value indicating whether the update process should continue.
        /// </summary>
        protected bool ContinueUpdate {
            get {
                return !Look && !DoUpdate.Pushing;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods handler.

        /// <summary>
        /// Handles ability initialization for the player. Override to add custom ability setup.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// Handles update method initialization for the player. Override to add custom update setup.
        /// </summary>
        protected virtual void abilities_OnStart() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            DoUpdate = Human_DoUpdate.GetInstance();
            DoFixedUpdate = Human_DoFixedUpdate.GetInstance();
            Acceleration = Human_Acceleration.GetInstance(this);
            GameSystem = Find(name: GAME_SYSTEM).Get<GameSystem>();
            SoundSystem = Find(name: SOUND_SYSTEM).Get<SoundSystem>();

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
                    Acceleration.PreviousSpeed = Acceleration.CurrentSpeed;// hold previous speed.
                    Acceleration.CurrentSpeed = rb.linearVelocity.magnitude; // get speed.
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Idol
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    !Up_Button.isPressed && !Down_Button.isPressed &&
                    DoUpdate.Ready)
                .Subscribe(onNext: _ => {
                    SimpleAnime.Play(stateName: "Default");
                    SoundSystem.StopSfxClip();
                    DoFixedUpdate.Apply(type: FixedUpdate.Idol);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    !DoUpdate.Climbing && DoFixedUpdate.Idol)
                .Subscribe(onNext: _ => {
                    rb.useGravity = true;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Walk
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    Up_Button.isPressed && !Y_Button.isPressed && !DoUpdate.VirtualControllerMode &&
                    DoUpdate.Ready)
                .Subscribe(onNext: _ => {
                    if (DoUpdate.Grounded) { 
                        SimpleAnime.Play(stateName: "Walk");
                        SoundSystem.Play(type: SfxClip.Walk) ;
                    }
                    DoFixedUpdate.Apply(type: FixedUpdate.Walk);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    DoFixedUpdate.Walk && Acceleration.CanWalk)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 7.5f;
                    rb.AddForce(force: transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                    DoFixedUpdate.Cancel(type: FixedUpdate.Walk);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Run
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    Up_Button.isPressed && (Y_Button.isPressed || DoUpdate.VirtualControllerMode) &&
                    DoUpdate.ReadyForAnyGround)
                .Subscribe(onNext: _ => {
                    if (DoUpdate.Grounded && !DoUpdate.Climbing) { 
                        SimpleAnime.Play(stateName: "Run");
                        SoundSystem.Play(type: SfxClip.Run);
                    }
                    DoFixedUpdate.Apply(type: FixedUpdate.Run);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    DoFixedUpdate.Run && Acceleration.CanRun)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 7.5f;
                    rb.AddForce(force: transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                    DoFixedUpdate.Cancel(type: FixedUpdate.Run);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Backward
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ =>  
                    Down_Button.isPressed &&
                    DoUpdate.Ready)
                .Subscribe(onNext: _ => {
                    SimpleAnime.Play(stateName: "Walk");
                    SoundSystem.Play(type: SfxClip.Walk);
                    DoFixedUpdate.Apply(type: FixedUpdate.Backward);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    DoFixedUpdate.Backward && Acceleration.CanBackward)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 7.5f;
                    rb.AddForce(force: -transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                    DoFixedUpdate.Cancel(type: FixedUpdate.Backward);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Stop TODO: stop anime fbx.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    (Up_Button.wasReleasedThisFrame || Down_Button.wasReleasedThisFrame) &&
                    DoUpdate.Ready)
                .Subscribe(onNext: _ => {
                    //SimpleAnime.Play("Stop");
                    DoFixedUpdate.Apply(type: FixedUpdate.Stop);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    DoFixedUpdate.Stop)
                .Subscribe(onNext: _ => {
                    rb.linearVelocity = new(x: 0f, y: 0f, z: 0f);
                    DoFixedUpdate.Cancel(type: FixedUpdate.Stop);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// On virtual controller mode.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    Y_Button.wasReleasedThisFrame && UseVirtualController)
                .Subscribe(onNext: _ => {
                    DoUpdate.VirtualControllerMode = true;
                    Observable.TimerFrame(dueTimeFrameCount: 45)
                        .Subscribe(onNext: _ => {
                            DoUpdate.VirtualControllerMode = false;
                        }).AddTo(this);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Jump
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    B_Button.wasPressedThisFrame &&
                    DoUpdate.Ready)
                .Subscribe(onNext: _ => {
                    DoUpdate.Grounded = false;
                    SimpleAnime.Play(stateName: "Jump");
                    SoundSystem.Play(type: SfxClip.Jump);
                    DoFixedUpdate.Apply(type: FixedUpdate.Jump);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    DoFixedUpdate.Jump)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 2.0f;
                    rb.useGravity = true;
                    rb.AddRelativeForce(force: up * Acceleration.JumpPower * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                    DoFixedUpdate.Cancel(type: FixedUpdate.Jump);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Abort jump.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    B_Button.wasReleasedThisFrame &&
                    !DoUpdate.Grounded &&
                    ContinueUpdate)
                .Subscribe(onNext: _ => {
                    DoFixedUpdate.Apply(type: FixedUpdate.AbortJump);
                }).AddTo(gameObjectComponent: this);

            this.FixedUpdateAsObservable()
                .Where(predicate: _ => 
                    !DoUpdate.Climbing && DoFixedUpdate.AbortJump)
                .Subscribe(onNext: _ => {
                    const float ADJUST_VALUE = 0.05f;
                    Observable.Timer(TimeSpan.FromSeconds(value: ADJUST_VALUE))
                        .Subscribe(onNext: _ => {
                            if (!isDown()) {
                                rb.useGravity = true;
                                Vector3 velocity = rb.linearVelocity;
                                rb.linearVelocity = new(x: velocity.x, y: 0, z: velocity.z);
                            }
                            DoFixedUpdate.Cancel(type: FixedUpdate.AbortJump);
                        }).AddTo(gameObjectComponent: this);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Rotate(yaw).
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    DoUpdate.ReadyForAnyGround)
                .Subscribe(onNext: _ => {
                    int axis = Right_Button.isPressed ? 1 : Left_Button.isPressed ? -1 : 0;
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
                    GameSystem.Home)
                .Subscribe(onNext: _ => {
                    SimpleAnime.enabled = false;
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// When touching blocks.
            /// TODO: to Block ?
            /// </summary>
            this.OnCollisionEnterAsObservable()
                .Where(predicate: x => 
                    x.Like(type: BLOCK_TYPE) &&
                    !gameObject.IsHitSide(target: x.gameObject) && 
                    !DoUpdate.Climbing)
                .Subscribe(onNext: x => {
                    SoundSystem.Play(type: SfxClip.Grounded);
                    DoUpdate.Grounded = true;
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
                    !DoUpdate.Climbing)
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
                    DoUpdate.Grounded = true;
                    if (isUpOrDown()) {
                        SoundSystem.Play(type: SfxClip.Grounded);
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
                    gameObject.IsHitSide(target: x.gameObject) && 
                    (Up_Button.isPressed || Down_Button.isPressed) && 
                    !DoUpdate.Climbing && !DoUpdate.Facing && !DoUpdate.Pushing && Acceleration.Freeze)
                .Subscribe(onNext: x => {
                    double reach = gameObject.GetReach(target: x.gameObject); // FIXME: Case the block size is other than 1.
                    // Moves left or right.
                    if (DoUpdate.Grounded && (reach < 0.5d || reach >= 0.99d)) {
                        gameObject.MoveLeftOrRight(direction: GetDirection(forward_vector: transform.forward));
                        rb.useGravity = true;
                    }
                    // Forcibly moves up.
                    else if (reach >= 0.5d && reach < 0.99d) {
                        rb.useGravity = false;
                        gameObject.MoveUp();
                        DoUpdate.Grounded = true;
                        rb.useGravity = true;
                    }
                    // Forcibly moves down.
                    else {
                        gameObject.MoveDown();
                        DoUpdate.Grounded = true;
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
                    Position = transform.position;
                    Rotation = transform.rotation;
                    cashPreviousPosition();
                }).AddTo(gameObjectComponent: this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Saves position value for the previous n frame.
        /// </summary>
        void cashPreviousPosition() {
            for (int i = PreviousPosition.Length - 1; i > -1; i--) {
                if (i > 0) {
                    PreviousPosition[i] = PreviousPosition[i - 1];
                } else if (i == 0) {
                    PreviousPosition[i] = new Vector3(
                        (float) Round(transform.position.x, 3),
                        (float) Round(transform.position.y, 3),
                        (float) Round(transform.position.z, 3)
                    );
                }
            }
        }

        /// <summary>
        /// Determines whether there was an upward or downward movement in the player's Y position.
        /// </summary>
        /// <returns>True if the Y position changed; otherwise, false.</returns>
        bool isUpOrDown() {
            int fps = Application.targetFrameRate;
            int adjust_value = 9;
            if (fps == 60) adjust_value = 9;
            if (fps == 30) adjust_value = 20;
            float current_y = (float) Round(transform.position.y, 1, MidpointRounding.AwayFromZero);
            float previous_y = (float) Round(PreviousPosition[adjust_value].y, 1, MidpointRounding.AwayFromZero);
            if (current_y == previous_y) {
                return false;
            } else if (current_y != previous_y) {
                return true;
            } else {
                return true;
            }
        }

        /// <summary>
        /// Determines whether there was a downward movement in the player's Y position.
        /// </summary>
        /// <returns>True if the Y position decreased or stayed the same; otherwise, false.</returns>
        bool isDown() {
            int fps = Application.targetFrameRate;
            int adjust_value = 9;
            if (fps == 60) adjust_value = 9;
            if (fps == 30) adjust_value = 20;
            float current_y = (float) Round(transform.position.y, 1, MidpointRounding.AwayFromZero);
            float previous_y = (float) Round(PreviousPosition[adjust_value].y, 1, MidpointRounding.AwayFromZero);
            if (current_y > previous_y) {
                return false;
            } else {
                return true;
            }
        }

        /// <summary>
        /// Rotates the player to face the surface directly.
        /// </summary>
        /// <param name="speed">Rotation speed for facing the surface.</param>
        void faceToFace(float speed = 20.0f) {
            float forward_x = (float) Round(transform.forward.x);
            float forward_z = (float) Round(transform.forward.z);
            if (forward_x == 0 && forward_z == 1) { // Positive Z-axis.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, 0), speed * Time.deltaTime); // Gradually rotate.
            } else if (forward_x == 0 && forward_z == -1) { // Negative Z-axis.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 180, 0), speed * Time.deltaTime); // Gradually rotate.
            } else if (forward_x == 1 && forward_z == 0) { // Positive X-axis.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 90, 0), speed * Time.deltaTime); // Gradually rotate.
            } else if (forward_x == -1 && forward_z == 0) { // Negative X-axis.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 270, 0), speed * Time.deltaTime); // Gradually rotate.
            }
        }

        /// <summary>
        /// Handles the changed event from the energy system.
        /// </summary>
        /// <param name="sender">Event source object.</param>
        /// <param name="e">Event arguments.</param>
        void onChanged(object sender, EvtArgs e) {
        }
    }
}