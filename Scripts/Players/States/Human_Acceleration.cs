// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using static System.Math;

namespace Germio {
    /// <summary>
    /// A Human controller
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Human : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region Inner Classes

        protected class Acceleration {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            Human _parent;

            float _current_speed, _previous_speed;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public float currentSpeed { get => _current_speed; set => _current_speed = value; }

            public float previousSpeed { get => _previous_speed; set => _previous_speed = value; }

            public bool canWalk { get => _current_speed < _parent._FORWARD_SPEED_LIMIT; }

            public bool canRun { get => _current_speed < _parent._RUN_SPEED_LIMIT; }

            public bool canBackward { get => _current_speed < _parent._BACKWARD_SPEED_LIMIT; }

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

            public float jumpPower  {
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
            /// Hides the constructor.
            /// </summary>
            Acceleration(Human parent) {
                _parent = parent;
            }

            /// <summary>
            /// Returns an initialized instance.
            /// </summary>
            public static Acceleration GetInstance(Human parent) {
                return new Acceleration(parent);
            }
        }

        #endregion
    }
}