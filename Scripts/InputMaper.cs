// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static UnityEngine.GameObject;
using UniRx;
using UniRx.Triggers;

namespace Germio {
    /// <summary>
    /// Maps physical gamepad inputs.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class InputMaper : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        /// <summary>
        /// Virtual controller object.
        /// </summary>
        protected GameObject _v_controller_object;

        /// <summary>
        /// Gamepad buttons.
        /// </summary>
        protected ButtonControl _a_button, _b_button, _x_button, _y_button, _up_button, _down_button, _left_button, _right_button;

        /// <summary>
        /// Shoulder and trigger buttons.
        /// </summary>
        protected ButtonControl _left_1_button, _right_1_button, _left_2_button, _right_2_button;

        /// <summary>
        /// Right stick buttons.
        /// </summary>
        protected ButtonControl _right_stick_up_button, _right_stick_down_button, _right_stick_left_button, _right_stick_right_button, _right_stick_button;

        /// <summary>
        /// Start and select buttons.
        /// </summary>
        protected ButtonControl _start_button, _select_button;

        /// <summary>
        /// Indicates whether the look functionality is active.
        /// </summary>
        protected static bool _look = false;

        /// <summary>
        /// Indicates whether vibration is enabled.
        /// </summary>
        bool _use_vibration = true;

        /// <summary>
        /// Indicates whether the virtual controller is used.
        /// </summary>
        bool _use_v_controller;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// Gets a value indicating whether virtual controllers are used.
        /// </summary>
        public bool useVirtualController { get => _use_v_controller; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        protected void Start() {
            // Gets virtual controller.
            _v_controller_object = Find(name: "VController");

            // Update is called once per frame.
            this.UpdateAsObservable()
                .Subscribe(onNext: _ => {
                    mapGamepad();
                }).AddTo(gameObjectComponent: this);

            #region mobile phone vibration.

            // Gets vibration on button press.
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    _v_controller_object && _use_vibration &&
                    (_a_button.wasPressedThisFrame || _b_button.wasPressedThisFrame || _x_button.wasPressedThisFrame || _y_button.wasPressedThisFrame ||
                    _up_button.wasPressedThisFrame || _down_button.wasPressedThisFrame || _left_button.wasPressedThisFrame || _right_button.wasPressedThisFrame ||
                    _left_1_button.wasPressedThisFrame || _right_1_button.wasPressedThisFrame || 
                    _select_button.wasPressedThisFrame || _start_button.wasPressedThisFrame))
                .Subscribe(onNext: _ => {
                    AndroidVibrator.Vibrate(milliseconds: 50L);
                }).AddTo(gameObjectComponent: this);

            // Disables vibration if start + X are pressed together.
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    (_x_button.isPressed && _start_button.wasPressedThisFrame) || 
                    (_x_button.wasPressedThisFrame && _start_button.isPressed))
                .Subscribe(onNext: _ => {
                    _use_vibration = !_use_vibration;
                }).AddTo(gameObjectComponent: this);

            #endregion
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Maps gamepad inputs.
        /// </summary>
        void mapGamepad() {
            // Checks if a physical gamepad is connected.
            string[] controller_names = Input.GetJoystickNames();
            if (controller_names.Length == 0 || controller_names[0] == "") {
                // Uses PC keyboard if no gamepad is connected.
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
                    _v_controller_object.SetActive(value: false);
                    _use_v_controller = false;
                    _up_button = Keyboard.current.upArrowKey;
                    _down_button = Keyboard.current.downArrowKey;
                    _left_button = Keyboard.current.leftArrowKey;
                    _right_button = Keyboard.current.rightArrowKey;
                    _y_button = Keyboard.current.aKey;
                    _b_button = Keyboard.current.zKey;
                    _x_button = Keyboard.current.sKey;
                    _a_button = Keyboard.current.xKey;
                    _select_button = Keyboard.current.cKey;
                    _start_button = Keyboard.current.vKey;
                    _left_1_button = Keyboard.current.qKey; 
                    _left_2_button = Keyboard.current.wKey;
                    _right_1_button = Keyboard.current.eKey;  
                    _right_2_button = Keyboard.current.rKey;
                    _right_stick_up_button = Keyboard.current.pageUpKey;
                    _right_stick_down_button = Keyboard.current.pageDownKey;
                    _right_stick_left_button = Keyboard.current.homeKey;
                    _right_stick_right_button = Keyboard.current.endKey;
                    _right_stick_button = Keyboard.current.insertKey;
                    return;
                }
                _v_controller_object.SetActive(value: true);
                _use_v_controller = true;
            } else {
                _v_controller_object.SetActive(value: false);
                _use_v_controller = false;
            }
            // Identifies the OS and sets button mappings accordingly.
            _up_button = Gamepad.current.dpad.up;
            _down_button = Gamepad.current.dpad.down;
            _left_button = Gamepad.current.dpad.left;
            _right_button = Gamepad.current.dpad.right;
            _start_button = Gamepad.current.startButton;
            _select_button = Gamepad.current.selectButton;
            if (Application.platform == RuntimePlatform.Android) {
                // For Android OS.
                _a_button = Gamepad.current.aButton;
                _b_button = Gamepad.current.bButton;
                _x_button = Gamepad.current.xButton;
                _y_button = Gamepad.current.yButton;
            } else if (Application.platform == RuntimePlatform.WindowsPlayer) {
                // For Windows OS.
                _a_button = Gamepad.current.bButton;
                _b_button = Gamepad.current.aButton;
                _x_button = Gamepad.current.yButton;
                _y_button = Gamepad.current.xButton;
            } else {
                // For other platforms (e.g., during Unity development).
                // FIXME: Can't get correct mapping during Unity development.
                _a_button = Gamepad.current.bButton;
                _b_button = Gamepad.current.aButton;
                _x_button = Gamepad.current.yButton;
                _y_button = Gamepad.current.xButton;
            }
            // Sets shoulder and trigger buttons for gamepad.
            _left_1_button = Gamepad.current.leftShoulder;
            _right_1_button = Gamepad.current.rightShoulder;
            _left_2_button = Gamepad.current.leftTrigger;
            _right_2_button = Gamepad.current.rightTrigger;
            // Sets right stick direction buttons.
            _right_stick_up_button = Gamepad.current.rightStick.up;
            _right_stick_down_button = Gamepad.current.rightStick.down;
            _right_stick_left_button = Gamepad.current.rightStick.left;
            _right_stick_right_button = Gamepad.current.rightStick.right;
            _right_stick_button = Gamepad.current.rightStickButton;
        }
    }
}