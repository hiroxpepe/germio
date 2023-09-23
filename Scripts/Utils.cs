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
    /// name of Dictionary is too long, it be named Map.
    /// </summary>
    public class Map<K, V> : Dictionary<K, V> {
    }

    /// <summary>
    /// changed event args.
    /// </summary>
    public class EvtArgs : EventArgs {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        public EvtArgs(string name) {
            Name = name;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, noun phrase, adjective]

        public string Name { get; }
        public string? Value { get; set; }
    }

    /// <summary>
    /// changed event handler.
    /// </summary>
    public delegate void Changed(object sender, EvtArgs e);

    /// <summary>
    /// generic utility class
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Utils {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // static Fields [noun, adjectives] 

        /// <summary>
        /// color.
        /// </summary>
        static Color _red, _orange, _yellow, _lime, _green, _cyan, _azure, _blue, _purple, _magenta, _white;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // static Constructor

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
        // public static Properties [noun, noun phrase, adjective]

        public static Color red { get => _red; }
        public static Color orange { get => _orange; }
        public static Color yellow { get => _yellow; }
        public static Color lime { get => _lime; }
        public static Color green { get => _green; }
        public static Color cyan { get => _cyan; }
        public static Color azure { get => _azure; }
        public static Color blue { get => _blue; }
        public static Color purple { get => _purple; }
        public static Color magenta { get => _magenta; }
        public static Color white { get => _white; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        #region has the component.

        /// <summary>
        /// has level.
        /// </summary>
        public static bool HasLevel() {
            GameObject game_object = Find(name: LEVEL_TYPE);
            return game_object is not null;
        }

        /// <summary>
        /// has player.
        /// </summary>
        public static bool HasPlayer() {
            GameObject game_object = Find(name: PLAYER_TYPE);
            return game_object is not null;
        }

        /// <summary>
        /// has home.
        /// </summary>
        public static bool HasHome() {
            GameObject game_object = Find(name: HOME_TYPE);
            return game_object is not null;
        }

        #endregion

        #region generic static methods.

        /// <summary>
        /// swap only the localPosition Y coordinate.
        /// </summary>
        public static Vector3 ReplaceLocalPositionY(Transform t, float value) {
            return new Vector3(t.localPosition.x, value, t.localPosition.z);
        }

        /// <summary>
        /// returns an enum of the player's direction.
        /// </summary>
        public static Direction GetDirection(Vector3 forward_vector) {
            float forward_x = (float) Round(a: forward_vector.x);
            float forward_y = (float) Round(a: forward_vector.y);
            float forward_z = (float) Round(a: forward_vector.z);
            if (forward_x == 0 && forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
            if (forward_x == 0 && forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            if (forward_x == 1 && forward_z == 0) { return Direction.PositiveX; } // x-axis positive.
            if (forward_x == -1 && forward_z == 0) { return Direction.NegativeX; } // x-axis negative.
            // determine the difference between the two axes.
            float absolute_x = Abs(value: forward_vector.x);
            float absolute_z = Abs(value: forward_vector.z);
            if (absolute_x > absolute_z) {
                if (forward_x == 1) { return Direction.PositiveX; } // x-axis positive.
                if (forward_x == -1) { return Direction.NegativeX; } // x-axis negative.
            } else if (absolute_x < absolute_z) {
                if (forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
                if (forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            }
            return Direction.None; // unknown.
        }

        #endregion
    }

    /// <summary>
    /// class for vibrate an Android phone.
    /// @author h.adachi
    /// </summary>
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