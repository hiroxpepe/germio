// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace Germio {
    /// <summary>
    /// Provides serialization and deserialization for game save data.
    /// Supports both plain JSON (for development) and AES-encrypted binary (for production).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class DataSerializer {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        /// <summary>
        /// File name for plain JSON save data.
        /// </summary>

        const string JSON_PATH = "germio_config.json";
        const string ENC_PATH = "germio_config.dat";

        /// <summary>
        /// AES encryption key (should be securely managed in production).
        /// </summary>
        static readonly byte[] AES_KEY = new byte[32] { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32 };

        /// <summary>
        /// AES initialization vector (should be securely managed in production).
        /// </summary>
        static readonly byte[] AES_IV = new byte[16] { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 };

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods

        /// <summary>
        /// Loads game save data from file.
        /// Tries to load plain JSON first, then encrypted binary if not found.
        /// </summary>
        /// <returns>Deserialized <see cref="Data"/> object, or null if not found.</returns>
        public static Data Load(string basePath = null) {
            string dir = basePath ?? Directory.GetCurrentDirectory();
            string pathJson = Path.Combine(dir, JSON_PATH);
            string pathEnc = Path.Combine(dir, ENC_PATH);

            if (File.Exists(pathJson)) {
                string json = File.ReadAllText(pathJson, Encoding.UTF8);
                return JsonSerializer.Deserialize<Data>(json)!;
            }
            else if (File.Exists(pathEnc)) {
                byte[] enc = File.ReadAllBytes(pathEnc);
                string json = DecryptAes(enc, AES_KEY, AES_IV);
                return JsonSerializer.Deserialize<Data>(json)!;
            }
            return null;
        }

        /// <summary>
        /// Saves game data to file.
        /// Writes as plain JSON in development, or as encrypted binary in production.
        /// </summary>
        /// <param name="data">The <see cref="Data"/> object to serialize and save.</param>
        /// <param name="encrypt">If true, saves as encrypted binary; otherwise, saves as plain JSON.</param>
        public static void Save(Data data, bool encrypt = false, string basePath = null) {
            string dir = basePath ?? Directory.GetCurrentDirectory();
            string pathJson = Path.Combine(dir, JSON_PATH);
            string pathEnc = Path.Combine(dir, ENC_PATH);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            if (encrypt) {
                byte[] enc = EncryptAes(json, AES_KEY, AES_IV);
                File.WriteAllBytes(pathEnc, enc);
            }
            else {
                File.WriteAllText(pathJson, json, Encoding.UTF8);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods

        /// <summary>
        /// Decrypts AES-encrypted binary data to a JSON string.
        /// </summary>
        static string DecryptAes(byte[] data, byte[] key, byte[] iv) {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        /// <summary>
        /// Encrypts a JSON string to AES-encrypted binary data.
        /// </summary>
        static byte[] EncryptAes(string plainText, byte[] key, byte[] iv) {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8)) {
                sw.Write(plainText);
            }
            return ms.ToArray();
        }
    }
}
