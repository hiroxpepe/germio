// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static UnityEngine.GameObject;
using UniRx;
using UniRx.Triggers;

namespace Germio {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Maps physical gamepad inputs.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class InputMapper : MonoBehaviour {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected static Fields

        /// <summary>
        /// Indicates whether the look functionality is active.
        /// </summary>
        protected static bool Look = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Fields

        /// <summary>
        /// Holds the virtual controller GameObject instance.
        /// </summary>
        protected GameObject VirtualControllerObject;

        /// <summary>
        /// Holds the main gamepad button controls.
        /// </summary>
        protected ButtonControl A_Button, B_Button, X_Button, Y_Button, Up_Button, Down_Button, Left_Button, Right_Button;

        /// <summary>
        /// Holds the shoulder and trigger button controls.
        /// </summary>
        protected ButtonControl Left1_Button, Right1_Button, Left2_Button, Right2_Button;

        /// <summary>
        /// Holds the right stick button controls.
        /// </summary>
        protected ButtonControl RightStick_Up_Button, RightStick_Down_Button, RightStick_Left_Button, RightStick_Right_Button, RightStick_Button;

        /// <summary>
        /// Holds the start and select button controls.
        /// </summary>
        protected ButtonControl Start_Button, Select_Button;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        /// <summary>
        /// Indicates whether vibration is enabled for the controller.
        /// </summary>
        bool _use_vibration = true;

        /// <summary>
        /// Indicates whether the virtual controller is currently used.
        /// </summary>
        bool _use_virtual_controller;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>
        /// Gets a value indicating whether the virtual controller is currently used.
        /// </summary>
        public bool UseVirtualController { get => _use_virtual_controller; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // protected Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        protected void Start() {
            /// <summary>
            /// Called by Unity before the first frame update. Initializes the virtual controller and sets up input subscriptions.
            /// </summary>
            // Gets virtual controller.
            VirtualControllerObject = Find(name: "VController");

            // Update is called once per frame.
            this.UpdateAsObservable()
                .Subscribe(onNext: _ => {
                    mapGamepad();
                }).AddTo(gameObjectComponent: this);

            #region mobile phone vibration.

            // Gets vibration on button press.
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    VirtualControllerObject && _use_vibration &&
                    (A_Button.wasPressedThisFrame || B_Button.wasPressedThisFrame || X_Button.wasPressedThisFrame || Y_Button.wasPressedThisFrame ||
                    Up_Button.wasPressedThisFrame || Down_Button.wasPressedThisFrame || Left_Button.wasPressedThisFrame || Right_Button.wasPressedThisFrame ||
                    Left1_Button.wasPressedThisFrame || Right1_Button.wasPressedThisFrame || 
                    Select_Button.wasPressedThisFrame || Start_Button.wasPressedThisFrame))
                .Subscribe(onNext: _ => {
                    AndroidVibrator.Vibrate(milliseconds: 50L);
                }).AddTo(gameObjectComponent: this);

            // Disables vibration if start + X are pressed together.
            this.UpdateAsObservable()
                .Where(predicate: _ => 
                    (X_Button.isPressed && Start_Button.wasPressedThisFrame) || 
                    (X_Button.wasPressedThisFrame && Start_Button.isPressed))
                .Subscribe(onNext: _ => {
                    _use_vibration = !_use_vibration;
                }).AddTo(gameObjectComponent: this);

            #endregion
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Maps physical and virtual gamepad inputs to button fields and sets up controller state.
        /// </summary>
        void mapGamepad() {
            // Checks if a physical gamepad is connected.
            string[] controller_names = Input.GetJoystickNames();
            if (controller_names.Length == 0 || controller_names[0] == "") {
                // Uses PC keyboard if no gamepad is connected.
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
                    VirtualControllerObject.SetActive(value: false);
                    _use_virtual_controller = false;
                    Up_Button = Keyboard.current.upArrowKey;
                    Down_Button = Keyboard.current.downArrowKey;
                    Left_Button = Keyboard.current.leftArrowKey;
                    Right_Button = Keyboard.current.rightArrowKey;
                    Y_Button = Keyboard.current.aKey;
                    B_Button = Keyboard.current.zKey;
                    X_Button = Keyboard.current.sKey;
                    A_Button = Keyboard.current.xKey;
                    Select_Button = Keyboard.current.cKey;
                    Start_Button = Keyboard.current.vKey;
                    Left1_Button = Keyboard.current.qKey; 
                    Left2_Button = Keyboard.current.wKey;
                    Right1_Button = Keyboard.current.eKey;  
                    Right2_Button = Keyboard.current.rKey;
                    RightStick_Up_Button = Keyboard.current.pageUpKey;
                    RightStick_Down_Button = Keyboard.current.pageDownKey;
                    RightStick_Left_Button = Keyboard.current.homeKey;
                    RightStick_Right_Button = Keyboard.current.endKey;
                    RightStick_Button = Keyboard.current.insertKey;
                    return;
                }
                VirtualControllerObject.SetActive(value: true);
                _use_virtual_controller = true;
            } else {
                VirtualControllerObject.SetActive(value: false);
                _use_virtual_controller = false;
            }
            // Identifies the OS and sets button mappings accordingly.
            Up_Button = Gamepad.current.dpad.up;
            Down_Button = Gamepad.current.dpad.down;
            Left_Button = Gamepad.current.dpad.left;
            Right_Button = Gamepad.current.dpad.right;
            Start_Button = Gamepad.current.startButton;
            Select_Button = Gamepad.current.selectButton;
            if (Application.platform == RuntimePlatform.Android) {
                // For Android OS.
                A_Button = Gamepad.current.aButton;
                B_Button = Gamepad.current.bButton;
                X_Button = Gamepad.current.xButton;
                Y_Button = Gamepad.current.yButton;
            } else if (Application.platform == RuntimePlatform.WindowsPlayer) {
                // For Windows OS.
                A_Button = Gamepad.current.bButton;
                B_Button = Gamepad.current.aButton;
                X_Button = Gamepad.current.yButton;
                Y_Button = Gamepad.current.xButton;
            } else {
                // For other platforms (e.g., during Unity development).
                // FIXME: Can't get correct mapping during Unity development.
                A_Button = Gamepad.current.bButton;
                B_Button = Gamepad.current.aButton;
                X_Button = Gamepad.current.yButton;
                Y_Button = Gamepad.current.xButton;
            }
            // Sets shoulder and trigger buttons for gamepad.
            Left1_Button = Gamepad.current.leftShoulder;
            Right1_Button = Gamepad.current.rightShoulder;
            Left2_Button = Gamepad.current.leftTrigger;
            Right2_Button = Gamepad.current.rightTrigger;
            // Sets right stick direction buttons.
            RightStick_Up_Button = Gamepad.current.rightStick.up;
            RightStick_Down_Button = Gamepad.current.rightStick.down;
            RightStick_Left_Button = Gamepad.current.rightStick.left;
            RightStick_Right_Button = Gamepad.current.rightStick.right;
            RightStick_Button = Gamepad.current.rightStickButton;
        }
    }
}