// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

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
        /// Handles the Update() method logic.
        /// </summary>
        protected class DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

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
            bool _facing;

            /// <summary>
            /// Indicates whether the virtual controller mode is active.
            /// </summary>
            bool _virtual_controller_mode;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Properties [noun, adjective]

            /// <summary>
            /// Gets or sets whether the player is grounded.
            /// </summary>
            public bool Grounded { get => _grounded; set => _grounded = value; }

            /// <summary>
            /// Gets or sets whether the player is climbing.
            /// </summary>
            public bool Climbing { get => _climbing; set => _climbing = value; }

            /// <summary>
            /// Gets or sets whether the player is pushing an object.
            /// </summary>
            public bool Pushing { get => _pushing; set => _pushing = value; }

            /// <summary>
            /// Gets or sets whether the player is holding an object.
            /// </summary>
            public bool Holding { get => _holding; set => _holding = value; }

            /// <summary>
            /// Gets or sets whether the player is facing a surface.
            /// </summary>
            public bool Facing { get => _facing; set => _facing = value; }

            /// <summary>
            /// Gets or sets whether the virtual controller mode is active.
            /// </summary>
            public bool VirtualControllerMode { get => _virtual_controller_mode; set => _virtual_controller_mode = value; }

            /// <summary>
            /// Indicates whether the player is ready for any ground interaction.
            /// </summary>
            public bool ReadyForAnyGround { 
                get {
                    return !Look && !_climbing && !_pushing && !_facing ? true : false;
                }
            }

            /// <summary>
            /// Indicates whether the player is ready for interaction.
            /// </summary>
            public bool Ready { 
                get {
                    return !Look && _grounded && !_climbing && !_pushing && !_facing ? true : false;
                }
            }

            /// <summary>
            /// Indicates whether the player is ready for interaction without holding an object.
            /// </summary>
            public bool ReadyWithoutHold { 
                get {
                    return !Look && _grounded && !_climbing && !_pushing && !_holding && !_facing ? true : false;
                }
            }

            /// <summary>
            /// Indicates whether the player is ready for interaction while holding an object.
            /// </summary>
            public bool ReadyWithHold { 
                get {
                    return !Look && !_climbing && !_pushing && _holding && !_facing ? true : false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public static Methods [verb]

            /// <summary>
            /// Creates and returns an initialized DoUpdate instance with the default state.
            /// </summary>
            public static DoUpdate GetInstance() {
                DoUpdate instance = new();
                instance.ResetState();
                return instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            /// <summary>
            /// Resets all state flags for the player to their default values.
            /// </summary>
            public void ResetState() {
                _grounded = _climbing = _pushing = _holding = _facing = _virtual_controller_mode = false;
            }
        }

    }
}