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
    /// A Block controller
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Block : Common {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        protected GameObject _player_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Properties [noun, adjectives]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Events [verb, verb phrase]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            /// <summary>
            /// Sets load Methods handler.
            /// </summary>
            abilities_OnAwake();
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();
            /// <summary>
            /// Sets update methods handler.
            /// </summary>
            abilities_OnStart();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods handler.

        /// <summary>
        /// Handles the load methods.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// Handles the update methods.
        /// </summary>
        protected virtual void abilities_OnStart() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Private Methods [verb]
    }
}