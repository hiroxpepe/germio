// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using static System.Math;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GameObject;

using static Germio.Env;

namespace Germio {
    /// <summary>
    /// A dictionary renamed to Map for simplicity.
    /// </summary>
    public class Map<K, V> : Dictionary<K, V> {
    }

    /// <summary>
    /// Represents event arguments with a name and optional value.
    /// </summary>
    public class EvtArgs : EventArgs {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        public EvtArgs(string name) {
            Name = name;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Properties [noun, noun phrase, adjective]

        /// <summary>
        /// The name of the event.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The optional value of the event.
        /// </summary>
        public string? Value { get; set; }
    }

    /// <summary>
    /// A delegate for handling changed events.
    /// </summary>
    public delegate void Changed(object sender, EvtArgs e);

    /// <summary>
    /// Provides generic utility methods and properties.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Utils {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Static Fields [noun, adjectives] 

        /// <summary>
        /// Predefined colors.
        /// </summary>
        static Color _red, _orange, _yellow, _lime, _green, _cyan, _azure, _blue, _purple, _magenta, _white;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Static Constructor

        static Utils() {
            ColorUtility.TryParseHtmlString(htmlString: COLOR_RED, color: out _red);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_ORANGE, color: out _orange);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_YELLOW, color: out _yellow);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_LIME, color: out _lime);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_GREEN, color: out _green);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_CYAN, color: out _cyan);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_AZURE, color: out _azure);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_BLUE, color: out _blue);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_PURPLE, color: out _purple);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_MAGENTA, color: out _magenta);
            ColorUtility.TryParseHtmlString(htmlString: COLOR_WHITE, color: out _white);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Static Properties [noun, noun phrase, adjective]

        /// <summary>
        /// The red color.
        /// </summary>
        public static Color red { get => _red; }

        /// <summary>
        /// The orange color.
        /// </summary>
        public static Color orange { get => _orange; }

        /// <summary>
        /// The yellow color.
        /// </summary>
        public static Color yellow { get => _yellow; }

        /// <summary>
        /// The lime color.
        /// </summary>
        public static Color lime { get => _lime; }

        /// <summary>
        /// The green color.
        /// </summary>
        public static Color green { get => _green; }

        /// <summary>
        /// The cyan color.
        /// </summary>
        public static Color cyan { get => _cyan; }

        /// <summary>
        /// The azure color.
        /// </summary>
        public static Color azure { get => _azure; }

        /// <summary>
        /// The blue color.
        /// </summary>
        public static Color blue { get => _blue; }

        /// <summary>
        /// The purple color.
        /// </summary>
        public static Color purple { get => _purple; }

        /// <summary>
        /// The magenta color.
        /// </summary>
        public static Color magenta { get => _magenta; }

        /// <summary>
        /// The white color.
        /// </summary>
        public static Color white { get => _white; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Public Static Methods [verb]

        #region Has Component

        /// <summary>
        /// Checks if a level component exists.
        /// </summary>
        public static bool HasLevel() {
            GameObject game_object = Find(name: LEVEL_TYPE);
            return game_object is not null;
        }

        /// <summary>
        /// Checks if a player component exists.
        /// </summary>
        public static bool HasPlayer() {
            GameObject game_object = Find(name: PLAYER_TYPE);
            return game_object is not null;
        }

        /// <summary>
        /// Checks if a vehicle component exists.
        /// </summary>
        public static bool HasVehicle() {
            GameObject game_object = Find(name: VEHICLE_TYPE);
            return game_object is not null;
        }

        /// <summary>
        /// Checks if a home component exists.
        /// </summary>
        public static bool HasHome() {
            GameObject game_object = Find(name: HOME_TYPE);
            return game_object is not null;
        }

        #endregion

        #region Generic Static Methods

        /// <summary>
        /// Swaps the Y coordinate of the local position.
        /// </summary>
        public static Vector3 SwapLocalPositionY(Transform transform, float value) {
            return new Vector3(transform.localPosition.x, value, transform.localPosition.z);
        }

        /// <summary>
        /// Determines the player's direction based on the forward vector.
        /// </summary>
        public static Direction GetDirection(Vector3 forward_vector) {
            float forward_x = (float) Round(a: forward_vector.x);
            float forward_y = (float) Round(a: forward_vector.y);
            float forward_z = (float) Round(a: forward_vector.z);
            if (forward_x == 0 && forward_z == 1) { return Direction.PositiveZ; } // Z-axis positive.
            if (forward_x == 0 && forward_z == -1) { return Direction.NegativeZ; } // Z-axis negative.
            if (forward_x == 1 && forward_z == 0) { return Direction.PositiveX; } // X-axis positive.
            if (forward_x == -1 && forward_z == 0) { return Direction.NegativeX; } // X-axis negative.
            // Determine the difference between the two axes.
            float absolute_x = Abs(value: forward_vector.x);
            float absolute_z = Abs(value: forward_vector.z);
            if (absolute_x > absolute_z) {
                if (forward_x == 1) { return Direction.PositiveX; } // X-axis positive.
                if (forward_x == -1) { return Direction.NegativeX; } // X-axis negative.
            } else if (absolute_x < absolute_z) {
                if (forward_z == 1) { return Direction.PositiveZ; } // Z-axis positive.
                if (forward_z == -1) { return Direction.NegativeZ; } // Z-axis negative.
            }
            return Direction.None; // Unknown.
        }

        /// <summary>
        /// Sets the rendering mode of the material.
        /// </summary>
        public static void SetRenderingMode(Material material, RenderingMode rendering_mode) {
            switch (rendering_mode) {
                case RenderingMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case RenderingMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 2450;
                    break;
                case RenderingMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
                case RenderingMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// Provides functionality to vibrate an Android phone.
    /// </summary>
    /// <author>h.adachi</author>
    public static class AndroidVibrator {
#if UNITY_ANDROID && !UNITY_EDITOR
        public static AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        public static AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        public static AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#else
        public static AndroidJavaClass unityPlayer;
        public static AndroidJavaObject currentActivity;
        public static AndroidJavaObject vibrator;
#endif
        public static void Vibrate(long milliseconds) {
            if (isAndroid()) {
                vibrator.Call(methodName: "vibrate", args: milliseconds);
            } else {
                Handheld.Vibrate();
            }
        }

        static bool isAndroid() {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }
}