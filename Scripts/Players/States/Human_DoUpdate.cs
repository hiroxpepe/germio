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
        /// Handles the Update() method logic.
        /// </summary>
        protected class DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            /// <summary>
            /// Indicates whether the player is grounded.
            /// </summary>
            bool _grounded;

            /// <summary>
            /// Indicates whether the player is climbing.
            /// </summary>
            bool _climbing;

            /// <summary>
            /// Indicates whether the player is pushing an object.
            /// </summary>
            bool _pushing;

            /// <summary>
            /// Indicates whether the player is holding an object.
            /// </summary>
            bool _holding;

            /// <summary>
            /// Indicates whether the player is facing a surface.
            /// </summary>
            bool _faceing;

            /// <summary>
            /// Indicates whether the virtual controller mode is active.
            /// </summary>
            bool _virtualControllerMode;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            /// <summary>
            /// Gets or sets whether the player is grounded.
            /// </summary>
            public bool grounded { get => _grounded; set => _grounded = value; }

            /// <summary>
            /// Gets or sets whether the player is climbing.
            /// </summary>
            public bool climbing { get => _climbing; set => _climbing = value; }

            /// <summary>
            /// Gets or sets whether the player is pushing an object.
            /// </summary>
            public bool pushing { get => _pushing; set => _pushing = value; }

            /// <summary>
            /// Gets or sets whether the player is holding an object.
            /// </summary>
            public bool holding { get => _holding; set => _holding = value; }

            /// <summary>
            /// Gets or sets whether the player is facing a surface.
            /// </summary>
            public bool faceing { get => _faceing; set => _faceing = value; }

            /// <summary>
            /// Gets or sets whether the virtual controller mode is active.
            /// </summary>
            public bool virtualControllerMode { get => _virtualControllerMode; set => _virtualControllerMode = value; }

            /// <summary>
            /// Indicates whether the player is ready for any ground interaction.
            /// </summary>
            public bool readyForAnyGround { 
                get {
                    return !_look && !_climbing && !_pushing && !_faceing ? true : false;
                }
            }

            /// <summary>
            /// Indicates whether the player is ready for interaction.
            /// </summary>
            public bool ready { 
                get {
                    return !_look && _grounded && !_climbing && !_pushing && !_faceing ? true : false;
                }
            }

            /// <summary>
            /// Indicates whether the player is ready for interaction without holding an object.
            /// </summary>
            public bool readyWithoutHold { 
                get {
                    return !_look && _grounded && !_climbing && !_pushing && !_holding && !_faceing ? true : false;
                }
            }

            /// <summary>
            /// Indicates whether the player is ready for interaction while holding an object.
            /// </summary>
            public bool readyWithHold { 
                get {
                    return !_look && !_climbing && !_pushing && _holding && !_faceing ? true : false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// Returns an initialized instance.
            /// </summary>
            public static DoUpdate GetInstance() {
                DoUpdate instance = new();
                instance.ResetState();
                return instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Public Methods [verb]

            /// <summary>
            /// Resets the state of the player.
            /// </summary>
            public void ResetState() {
                _grounded = _climbing = _pushing = _holding = _faceing = _virtualControllerMode = false;
            }
        }

        #endregion
    }
}