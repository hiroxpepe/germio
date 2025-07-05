// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using static System.Math;

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
        /// Handles acceleration logic for the Human player.
        /// </summary>
        protected class Acceleration {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            /// <summary>
            /// Reference to the parent Human object.
            /// </summary>
            Human _parent;

            /// <summary>
            /// Current speed of the player.
            /// </summary>
            float _current_speed;

            /// <summary>
            /// Previous speed of the player.
            /// </summary>
            float _previous_speed;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            /// <summary>
            /// Gets or sets the current speed of the player.
            /// </summary>
            public float currentSpeed { get => _current_speed; set => _current_speed = value; }

            /// <summary>
            /// Gets or sets the previous speed of the player.
            /// </summary>
            public float previousSpeed { get => _previous_speed; set => _previous_speed = value; }

            /// <summary>
            /// Indicates whether the player can walk.
            /// </summary>
            public bool canWalk { get => _current_speed < _parent._FORWARD_SPEED_LIMIT; }

            /// <summary>
            /// Indicates whether the player can run.
            /// </summary>
            public bool canRun { get => _current_speed < _parent._RUN_SPEED_LIMIT; }

            /// <summary>
            /// Indicates whether the player can move backward.
            /// </summary>
            public bool canBackward { get => _current_speed < _parent._BACKWARD_SPEED_LIMIT; }

            /// <summary>
            /// Indicates whether the player's movement is frozen.
            /// </summary>
            public bool freeze {
                get {
                    if (Round(value: _previous_speed, digits: 2) < 0.02 &&
                        Round(value: _current_speed, digits: 2) < 0.02 &&
                        Round(value: _previous_speed, digits: 2) == Round(value: _current_speed, digits: 2)) {
                        return true;
                    }
                    return false;
                }
            }

            /// <summary>
            /// Gets the jump power of the player based on the current state.
            /// </summary>
            public float jumpPower {
                get {
                    float value = 0f;
                    if (_parent._y_button.isPressed || _parent._do_update.virtualControllerMode) {
                        value = _parent._JUMP_POWER * 1.25f;
                    }
                    else if (_parent._up_button.isPressed || _parent._down_button.isPressed) {
                        value = _parent._JUMP_POWER;
                    }
                    else if (!_parent._up_button.isPressed && !_parent._down_button.isPressed) {
                        value = _parent._JUMP_POWER * 1.25f;
                    }
                    return value;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// Initializes a new instance of the Acceleration class with the specified parent.
            /// </summary>
            /// <param name="parent">Parent Human object to associate with this acceleration logic.</param>
            Acceleration(Human parent) {
                _parent = parent;
            }

            /// <summary>
            /// Creates and returns an initialized Acceleration instance for the specified parent.
            /// </summary>
            /// <param name="parent">Parent Human object to associate with the new Acceleration instance.</param>
            public static Acceleration GetInstance(Human parent) {
                return new Acceleration(parent);
            }
        }

        #endregion
    }
}