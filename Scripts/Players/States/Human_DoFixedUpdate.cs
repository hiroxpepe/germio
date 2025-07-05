// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// Controls the Human player, acceleration and movement logic.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Human : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region inner Classes

        /// <summary>
        /// Handles the FixedUpdate() method logic.
        /// </summary>
        protected class DoFixedUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            /// <summary>
            /// Indicates whether the player is idle.
            /// </summary>
            bool _idol;

            /// <summary>
            /// Indicates whether the player is running.
            /// </summary>
            bool _run;

            /// <summary>
            /// Indicates whether the player is walking.
            /// </summary>
            bool _walk;

            /// <summary>
            /// Indicates whether the player is jumping.
            /// </summary>
            bool _jump;

            /// <summary>
            /// Indicates whether the player is aborting a jump.
            /// </summary>
            bool _abort_jump;

            /// <summary>
            /// Indicates whether the player is moving backward.
            /// </summary>
            bool _backward;

            /// <summary>
            /// Indicates whether the player is stopping.
            /// </summary>
            bool _stop;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            /// <summary>
            /// Gets whether the player is idle.
            /// </summary>
            public bool idol { get => _idol; }

            /// <summary>
            /// Gets whether the player is running.
            /// </summary>
            public bool run { get => _run; }

            /// <summary>
            /// Gets whether the player is walking.
            /// </summary>
            public bool walk { get => _walk; }

            /// <summary>
            /// Gets whether the player is jumping.
            /// </summary>
            public bool jump { get => _jump; }

            /// <summary>
            /// Gets whether the player is aborting a jump.
            /// </summary>
            public bool abortJump { get => _abort_jump; }

            /// <summary>
            /// Gets whether the player is moving backward.
            /// </summary>
            public bool backward { get => _backward; }

            /// <summary>
            /// Gets whether the player is stopping.
            /// </summary>
            public bool stop { get => _stop; }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// Creates and returns an initialized DoFixedUpdate instance.
            /// </summary>
            public static DoFixedUpdate GetInstance() {
                return new DoFixedUpdate();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            /// <summary>
            /// Applies the specified FixedUpdate state.
            /// </summary>
            /// <param name="type">The FixedUpdate state to apply.</param>
            public void Apply(FixedUpdate type) {
                switch (type) {
                    case FixedUpdate.Idol: _idol = true; _run = _walk = _backward = _jump = false; break;
                    case FixedUpdate.Run: _idol = _walk = _backward = false; _run = true; break;
                    case FixedUpdate.Walk: _idol = _run = _backward = false; _walk = true; break;
                    case FixedUpdate.Backward: _idol = _run = _walk = false; _backward = true; break;
                    case FixedUpdate.Jump: _jump = true; break;
                    case FixedUpdate.AbortJump: _abort_jump = true; break;
                    case FixedUpdate.Stop: _stop = true; break;
                }
            }

            /// <summary>
            /// Cancels the specified FixedUpdate state.
            /// </summary>
            /// <param name="type">The FixedUpdate state to cancel.</param>
            public void Cancel(FixedUpdate type) {
                switch (type) {
                    case FixedUpdate.Idol: break;
                    case FixedUpdate.Run: _run = false; break;
                    case FixedUpdate.Walk: _walk = false; break;
                    case FixedUpdate.Backward: _backward = false; break;
                    case FixedUpdate.Jump: _jump = false; break;
                    case FixedUpdate.AbortJump: _abort_jump = false; break;
                    case FixedUpdate.Stop: _stop = false; break;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Specifies the FixedUpdate states.
    /// </summary>
    public enum FixedUpdate {
        /// <summary>
        /// Idle state.
        /// </summary>
        Idol,

        /// <summary>
        /// Running state.
        /// </summary>
        Run,

        /// <summary>
        /// Walking state.
        /// </summary>
        Walk,

        /// <summary>
        /// Backward movement state.
        /// </summary>
        Backward,

        /// <summary>
        /// Jumping state.
        /// </summary>
        Jump,

        /// <summary>
        /// Aborting jump state.
        /// </summary>
        AbortJump,

        /// <summary>
        /// Stopping state.
        /// </summary>
        Stop
    }
}