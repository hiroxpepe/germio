// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;

namespace Germio {
    /// <summary>
    /// Manages the title scene, including navigation to other scenes.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Title : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        /// <summary>
        /// Gets the reference to the game system.
        /// </summary>
        GameSystem _game_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
        }

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            /// <summary>
            /// Loads the select scene from the title screen.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _select_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    LoadScene(sceneName: SCENE_SELECT);
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// Loads the first level scene from the title screen.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _start_button.wasPressedThisFrame || _a_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    LoadScene(sceneName: SCENE_LEVEL_1);
                }).AddTo(gameObjectComponent: this);
        }
    }
}