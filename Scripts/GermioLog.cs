// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.IO;
using UnityEngine;

namespace Germio {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Lightweight file-based logger for diagnostic purposes.
    /// Writes to <c>game/germio.log</c> (project root) so the maintainer can inspect
    /// the log file directly without scrolling through Unity Console.
    ///
    /// Each entry is timestamped (HH:mm:ss.fff) and appended with a newline.
    /// Also mirrors to Debug.Log so Unity Console keeps the message too.
    ///
    /// Usage:
    ///   GermioLog.Write("[Germio] something happened");
    ///
    /// Disable by setting <see cref="Enabled"/> to false (or removing calls).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class GermioLog {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Fields

        /// <summary>Enable / disable the logger globally.</summary>
        public static bool Enabled = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Fields

        /// <summary>Cached log file path (relative to project root: game/germio.log).</summary>
        static string? _path;

        /// <summary>True if the file has been cleared at app startup.</summary>
        static bool _initialized = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Writes a timestamped message to <c>game/germio.log</c> and Unity Console.
        /// First call clears any previous log file.
        /// </summary>
        public static void Write(string message) {
            if (!Enabled) { return; }
            try {
                if (_path == null) {
                    _path = Path.Combine(Application.dataPath, "..", "germio.log");
                }
                if (!_initialized) {
                    File.WriteAllText(path: _path, contents: $"=== Germio diagnostic log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
                    _initialized = true;
                }
                File.AppendAllText(path: _path, contents: $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            } catch (Exception ex) {
                Debug.LogError(message: $"[GermioLog] write failed: {ex.Message}");
            }
            Debug.Log(message: message);
        }
    }
}