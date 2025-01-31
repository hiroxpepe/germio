// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// A Human controller
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Human : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region Inner Classes

        /// <summary>
        /// The Class for the FixedUpdate() method.
        /// </summary>
        protected class DoFixedUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            bool _idol, _run, _walk, _jump, _abort_jump, _backward, _stop;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool idol { get => _idol; }

            public bool run { get => _run; }

            public bool walk { get => _walk; }

            public bool jump { get => _jump; }

            public bool abortJump { get => _abort_jump; }

            public bool backward { get => _backward; }

            public bool stop { get => _stop; }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// Returns an initialized instance.
            /// </summary>
            public static DoFixedUpdate GetInstance() {
                return new DoFixedUpdate();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Public Methods [verb]

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

    public enum FixedUpdate {
        Idol,
        Run,
        Walk,
        Backward,
        Jump,
        AbortJump,
        Stop
    }
}