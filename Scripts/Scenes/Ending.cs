// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;

namespace Germio {
    /// <summary>
    /// The ending scene.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Ending : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameSystem _game_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
        }

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            /// <summary>
            /// Moves to the title scene.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    (_start_button.wasPressedThisFrame || _a_button.wasPressedThisFrame))
                .Subscribe(onNext: _ => {
                    LoadScene(sceneName: SCENE_TITLE);
                }).AddTo(gameObjectComponent: this);
        }
    }
}
