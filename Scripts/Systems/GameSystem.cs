// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using UnityEngine;
using static UnityEngine.GameObject;

using static Germio.Env;
using static Germio.Utils;

namespace Germio {
    /// <summary>
    /// The game system
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class GameSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// Current game mode.
        /// </summary>
        public string mode { get => Status.mode; set => Status.mode = value; }

        /// <summary>
        /// True if the player is home.
        /// </summary>
        public bool home { get; set; }

        /// <summary>
        /// True if the level is beaten.
        /// </summary>
        public bool beat { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Events [verb, verb phrase]

        /// <summary>
        /// When the game is paused.
        /// </summary>
        public event Action? OnPauseOn;

        /// <summary>
        /// When the game is unpaused.
        /// </summary>
        public event Action? OnPauseOff;

        /// <summary>
        /// When a level starts.
        /// </summary>
        public event Action? OnStartLevel;

        /// <summary>
        /// When the player comes back home.
        /// </summary>
        public event Action? OnCameBackHome;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            // Sets frame rate.
            Application.targetFrameRate = FPS;

            if (HasLevel()) {
                // Gets the level by name.
                Level level = Find(name: LEVEL_TYPE).Get<Level>();

                /// <summary>
                /// Triggers when the level pauses.
                /// </summary>
                level.OnPauseOn += () => { OnPauseOn?.Invoke(); };

                /// <summary>
                /// Triggers when the level resumes.
                /// </summary>
                level.OnPauseOff += () => { OnPauseOff?.Invoke(); };

                /// <summary>
                /// Triggers when the level starts.
                /// </summary>
                level.OnStart += () => { OnStartLevel?.Invoke(); };
            }

            if (HasHome()) {
                // Gets the home.
                Home home = Find(name: HOME_TYPE).Get<Home>();

                /// <summary>
                /// Triggers when home is returned to.
                /// </summary>
                this.home = false;
                home.OnCameBack += () => {
                    this.home = true;
                    OnCameBackHome?.Invoke(); 
                };
            }

            /// <summary>
            /// Sets the load methods handler.
            /// </summary>
            abilities_OnAwake();
        }

        // Start is called before the first frame update.
        void Start() {
            /// <summary>
            /// Sets the update methods handler.
            /// </summary>
            abilities_OnStart();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods handler.

        /// <summary>
        /// Handles the loading of methods.
        /// </summary>
        protected virtual void abilities_OnAwake() { }

        /// <summary>
        /// Handles the updating of methods.
        /// </summary>
        protected virtual void abilities_OnStart() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // inner Classes

        #region Status

        static class Status {
#nullable enable

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Static Fields [nouns, noun phrases]

            static string _mode;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Static Constructor

            static Status() {
                _mode = MODE_NORMAL;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Public Static Properties [noun, noun phrase, adjective]

            public static string mode {
                get => _mode; set => _mode = value;
            }
        }

        #endregion
    }
}