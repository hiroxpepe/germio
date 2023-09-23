// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.GameObject;
using static UnityEngine.SceneManagement.SceneManager;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

using static Germio.Env;
using static Germio.Utils;

namespace Germio {
    /// <summary>
    /// select scene
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Select : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const int SELECT_COUNT = 3;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] Image _easy, _normal, _hard;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameSystem _game_system;

        Map<int, string> _focus = new();

        string _selected = MODE_NORMAL;

        int _idx = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
            // set default focus.
            _focus.Add(key: 0, value: MODE_EASY);
            _focus.Add(key: 1, value: MODE_NORMAL);
            _focus.Add(key: 2, value: MODE_HARD);
            _idx = 1; // FIXME:
            _selected = _focus[_idx];
            changeSelectedColor();
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            /// <summary>
            /// select up.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _up_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    _idx--;
                    if (_idx == -1) {
                        _idx = SELECT_COUNT - 1;
                    }
                    _selected = _focus[_idx];
                    _game_system.mode = _selected;
                    changeSelectedColor();
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// select down.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _down_button.wasPressedThisFrame || _select_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    _idx++;
                    if (_idx == SELECT_COUNT) {
                        _idx = 0;
                    }
                    _selected = _focus[_idx];
                    _game_system.mode = _selected;
                    changeSelectedColor();
                }).AddTo(gameObjectComponent: this);

            /// <summary>
            /// return title.
            /// </summary>
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _start_button.wasPressedThisFrame || _a_button.wasPressedThisFrame)
                .Subscribe(onNext: _ => {
                    LoadScene(sceneName: SCENE_TITLE);
                }).AddTo(gameObjectComponent: this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        void changeSelectedColor() {
            switch (_selected) {
                case MODE_EASY:
                    _easy.color = yellow;
                    _normal.color = white;
                    _hard.color = white;
                    break;
                case MODE_NORMAL:
                    _easy.color = white;
                    _normal.color = yellow;
                    _hard.color = white;
                    break;
                case MODE_HARD:
                    _easy.color = white;
                    _normal.color = white;
                    _hard.color = yellow;
                    break;
            }
        }
    }
}