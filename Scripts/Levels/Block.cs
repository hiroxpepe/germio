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
    /// block controller
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
            /// set load Methods handler.
            /// </summary>
            abilities_OnAwake();
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();
            /// <summary>
            /// set update methods handler.
            /// </summary>
            abilities_OnStart();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods handler.

        /// <summary>
        /// load methods handler.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// update methods handler.
        /// </summary>
        protected virtual void abilities_OnStart() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]
    }
}