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
    /// Keeps a single open <see cref="StreamWriter"/> for the process lifetime instead
    /// of opening and closing the file on every call, so high-frequency logging does
    /// not degrade frame rate. AutoFlush keeps the file readable in real time while
    /// the game is running.
    ///
    /// Usage:
    ///   GermioLog.Write("[Germio] something happened");
    ///
    /// Disable by setting <see cref="Enabled"/> to false (or removing calls).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class GermioLog {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Fields

        /// <summary>Open writer held for the process lifetime; null until first Write.</summary>
        static StreamWriter? _writer;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Fields

        /// <summary>Enable / disable the logger globally.</summary>
        public static bool Enabled = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Writes a timestamped message to <c>game/germio.log</c> and Unity Console.
        /// First call clears any previous log file and opens it for the process lifetime.
        /// </summary>
        public static void Write(string message) {
            if (!Enabled) { return; }
            try {
                if (_writer == null) {
                    string path = Path.Combine(Application.dataPath, "..", "germio.log");
                    _writer = new StreamWriter(path: path, append: false) { AutoFlush = true };
                    _writer.WriteLine(value: $"=== Germio diagnostic log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                    Application.quitting += closeWriter;
                }
                _writer.WriteLine(value: $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            } catch (Exception ex) {
                Debug.LogError(message: $"[GermioLog] write failed: {ex.Message}");
            }
            Debug.Log(message: message);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Methods [verb]

        static void closeWriter() {
            _writer?.Close();
            _writer = null;
        }
    }
}