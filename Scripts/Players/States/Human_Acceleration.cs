// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using static System.Math;

using Germio;

namespace Germio.Players {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Controls the Human player, acceleration and movement logic.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Human : InputMapper {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected inner Classes

        /// <summary>
        /// Handles acceleration logic for the Human player.
        /// </summary>
        protected class Acceleration {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

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

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// Initializes a new instance of the Acceleration class with the specified parent.
            /// </summary>
            /// <param name="parent">Parent Human object to associate with this acceleration logic.</param>
            Acceleration(Human parent) {
                _parent = parent;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Properties [noun, adjective]

            /// <summary>
            /// Gets or sets the current speed of the player.
            /// </summary>
            public float CurrentSpeed { get => _current_speed; set => _current_speed = value; }

            /// <summary>
            /// Gets or sets the previous speed of the player.
            /// </summary>
            public float PreviousSpeed { get => _previous_speed; set => _previous_speed = value; }

            /// <summary>
            /// Indicates whether the player can walk.
            /// </summary>
            public bool CanWalk { get => _current_speed < _parent._FORWARD_SPEED_LIMIT; }

            /// <summary>
            /// Indicates whether the player can run.
            /// </summary>
            public bool CanRun { get => _current_speed < _parent._RUN_SPEED_LIMIT; }

            /// <summary>
            /// Indicates whether the player can move backward.
            /// </summary>
            public bool CanBackward { get => _current_speed < _parent._BACKWARD_SPEED_LIMIT; }

            /// <summary>
            /// Indicates whether the player's movement is frozen.
            /// </summary>
            public bool Freeze {
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
            public float JumpPower {
                get {
                    float value = 0f;
                    if (_parent.Y_Button.isPressed || _parent.DoUpdate.VirtualControllerMode) {
                        value = _parent._JUMP_POWER * 1.25f;
                    }
                    else if (_parent.Up_Button.isPressed || _parent.Down_Button.isPressed) {
                        value = _parent._JUMP_POWER;
                    }
                    else if (!_parent.Up_Button.isPressed && !_parent.Down_Button.isPressed) {
                        value = _parent._JUMP_POWER * 1.25f;
                    }
                    return value;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public static Methods [verb]

            /// <summary>
            /// Creates and returns an initialized Acceleration instance for the specified parent.
            /// </summary>
            /// <param name="parent">Parent Human object to associate with the new Acceleration instance.</param>
            public static Acceleration GetInstance(Human parent) {
                return new Acceleration(parent);
            }
        }

    }
}