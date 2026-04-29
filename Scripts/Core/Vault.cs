// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
using System.IO;
#endif

namespace Germio.Core {
    /// <summary>
    /// Provides AES key material for Storage encryption.
    /// Key source priority (first available wins):
    ///   1. Environment variable GERMIO_AES_KEY (Base64, 48 bytes: 32 key + 16 IV) [testable]
    ///   2. StreamingAssets/germio_key.bin (Unity only)
    /// PlayerPrefs fallback removed in v2.2 (G6: no PlayerPrefs in framework layer).
    /// Throws InvalidOperationException if no source is available or material is too short.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Vault {
#nullable enable
        const string ENV_VAR     = "GERMIO_AES_KEY";
        const int    MATERIAL_LEN = 48; // 32 bytes key + 16 bytes IV
        const int    KEY_LEN      = 32;
        const int    IV_LEN       = 16;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Returns (key: 32 bytes, iv: 16 bytes) from the first available key source.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no key source is available or the material is shorter than 48 bytes.
        /// </exception>
        public static (byte[] key, byte[] iv) GetKey() {
            byte[]? material = null;

            // Priority 1: environment variable (testable in NUnit without Unity)
            string? env_value = Environment.GetEnvironmentVariable(ENV_VAR);
            if (!string.IsNullOrEmpty(env_value)) {
                material = Convert.FromBase64String(env_value);
            }

#if UNITY_5_3_OR_NEWER
            // Priority 2: StreamingAssets/germio_key.bin
            if (material == null) {
                string path = Path.Combine(Application.streamingAssetsPath, "germio_key.bin");
                if (File.Exists(path)) {
                    material = File.ReadAllBytes(path);
                }
            }
#endif

            if (material == null) {
                throw new InvalidOperationException(
                    $"No AES key source available. Set environment variable '{ENV_VAR}' " +
                    "or provide StreamingAssets/germio_key.bin.");
            }

            if (material.Length < MATERIAL_LEN) {
                throw new InvalidOperationException(
                    $"Key material must be at least {MATERIAL_LEN} bytes (got {material.Length}). " +
                    "Ensure the base64-encoded value encodes exactly 48 bytes.");
            }

            var key = new byte[KEY_LEN];
            var iv  = new byte[IV_LEN];
            Buffer.BlockCopy(material, 0,       key, 0, KEY_LEN);
            Buffer.BlockCopy(material, KEY_LEN, iv,  0, IV_LEN);

            return (key, iv);
        }
    }
}
