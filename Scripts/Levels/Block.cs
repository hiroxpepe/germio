// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using static System.Math;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.Mathf;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;
using static Germio.Utils;

namespace Germio {
    /// <summary>
    /// Represents a block object in the game.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Block : Common {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        /// <summary>
        /// Holds a reference to the player object.
        /// </summary>
        protected GameObject _player_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjectives]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            /// <summary>
            /// Initializes the block's abilities when the script instance is loaded.
            /// </summary>
            abilities_OnAwake();
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();
            /// <summary>
            /// Initializes update method handlers for the block.
            /// </summary>
            abilities_OnStart();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods handler.

        /// <summary>
        /// Handles the initialization of block abilities during the Awake phase.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// Handles the initialization of block abilities during the Start phase.
        /// </summary>
        protected virtual void abilities_OnStart() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]
    }
}