// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using UnityEngine;
using static UnityEngine.GameObject;

using static Germio.Env;
using static Germio.Utils;

namespace Germio {
    /// <summary>
    /// Manages the game system, including levels and home interactions.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class GameSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// Gets or sets the current game mode.
        /// </summary>
        public string mode { get => Status.mode; set => Status.mode = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the player is home.
        /// </summary>
        public bool home { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the level is beaten.
        /// </summary>
        public bool beat { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        /// <summary>
        /// Occurs when the game is paused.
        /// </summary>
        public event Action? OnPauseOn;

        /// <summary>
        /// Occurs when the game is unpaused.
        /// </summary>
        public event Action? OnPauseOff;

        /// <summary>
        /// Occurs when a level starts.
        /// </summary>
        public event Action? OnStartLevel;

        /// <summary>
        /// Occurs when the player comes back home.
        /// </summary>
        public event Action? OnCameBackHome;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            // Sets the frame rate.
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

        /// <summary>
        /// Represents the game status.
        /// </summary>
        static class Status {
#nullable enable

            ///////////////////////////////////////////////////////////////////////////////////////////
            // static Fields [nouns, noun phrases]

            /// <summary>
            /// The current game mode.
            /// </summary>
            static string _mode;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // static Constructor

            static Status() {
                _mode = MODE_NORMAL;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public static Properties [noun, noun phrase, adjective]

            /// <summary>
            /// Gets or sets the current game mode.
            /// </summary>
            public static string mode {
                get => _mode; set => _mode = value;
            }
        }

        #endregion
    }
}