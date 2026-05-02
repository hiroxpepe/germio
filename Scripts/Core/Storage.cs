// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using Germio.Model;

namespace Germio.Core
{
    /// <summary>
    /// Provides serialization and deserialization for game save data.
    /// Supports both plain JSON (for development) and AES-encrypted binary (for production).
    /// All file I/O is async to avoid blocking the main thread on Android storage.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Storage {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const string SCENARIO_PATH = "germio.json";
        const string SCENARIO_ENC_PATH = "germio.dat";
        const string SNAPSHOT_PATH_TEMPLATE = "snapshot_{0}.json";
        const string SNAPSHOT_ENC_PATH_TEMPLATE = "snapshot_{0}.dat";

        static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() }
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods

        /// <summary>
        /// Asynchronously loads game save data from file.
        /// Tries to load plain JSON first, then encrypted binary if not found.
        /// </summary>
        /// <returns>Deserialized <see cref="Scenario"/> object, or null if not found.</returns>
        public static async Task<Scenario?> LoadAsync(string? base_path = null) {
            string dir        = base_path ?? Directory.GetCurrentDirectory();
            string path_json  = Path.Combine(dir, SCENARIO_PATH);
            string path_enc   = Path.Combine(dir, SCENARIO_ENC_PATH);
            if (File.Exists(path_json)) {
                string json = await File.ReadAllTextAsync(path_json, Encoding.UTF8);
                var raw     = JObject.Parse(json);
                return raw.ToObject<Scenario>(JsonSerializer.Create(_settings))!;
            }
            else if (File.Exists(path_enc)) {
                var (key, iv) = Vault.GetKey();
                byte[] enc    = await File.ReadAllBytesAsync(path_enc);
                string json   = await DecryptAesAsync(data: enc, key: key, iv: iv);
                var raw       = JObject.Parse(json);
                return raw.ToObject<Scenario>(JsonSerializer.Create(_settings))!;
            }
            return null;
        }

        /// <summary>
        /// Asynchronously saves game data to file.
        /// Writes as plain JSON in development, or as encrypted binary in production.
        /// </summary>
        /// <param name="data">The <see cref="Scenario"/> object to serialize and save.</param>
        /// <param name="encrypt">If true, saves as encrypted binary; otherwise, saves as plain JSON.</param>
        public static async Task SaveAsync(Scenario data, bool encrypt = false, string? base_path = null) {
            string dir        = base_path ?? Directory.GetCurrentDirectory();
            string path_json  = Path.Combine(dir, SCENARIO_PATH);
            string path_enc   = Path.Combine(dir, SCENARIO_ENC_PATH);
            string json       = JsonConvert.SerializeObject(data, _settings);
            if (encrypt) {
                var (key, iv) = Vault.GetKey();
                byte[] enc    = await EncryptAesAsync(plain_text: json, key: key, iv: iv);
                await File.WriteAllBytesAsync(path_enc, enc);
            }
            else {
                await File.WriteAllTextAsync(path_json, json, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Asynchronously loads a runtime snapshot from the specified slot.
        /// Tries plaintext snapshot_N.json first, then encrypted snapshot_N.dat.
        /// </summary>
        public static async Task<Snapshot?> LoadSnapshotAsync(int slot) {
            string plain_path = string.Format(SNAPSHOT_PATH_TEMPLATE, slot);
            string enc_path = string.Format(SNAPSHOT_ENC_PATH_TEMPLATE, slot);
            string base_dir = getStreamingAssetsPath();
            string plain_full = Path.Combine(base_dir, plain_path);
            string enc_full = Path.Combine(base_dir, enc_path);

            if (File.Exists(plain_full)) {
                string text = await File.ReadAllTextAsync(plain_full);
                return JsonConvert.DeserializeObject<Snapshot>(value: text, settings: _settings);
            }
            if (File.Exists(enc_full)) {
                byte[] enc_bytes = await File.ReadAllBytesAsync(enc_full);
                string text = await DecryptAesAsync(data: enc_bytes, key: Vault.GetKey().key, iv: Vault.GetKey().iv);
                return JsonConvert.DeserializeObject<Snapshot>(value: text, settings: _settings);
            }
            return null;
        }

        /// <summary>
        /// Asynchronously saves a runtime snapshot to the specified slot.
        /// Writes plaintext in development; encrypted in production.
        /// </summary>
        public static async Task SaveSnapshotAsync(Snapshot snapshot, int slot) {
            string base_dir = getStreamingAssetsPath();
            string text = JsonConvert.SerializeObject(value: snapshot, settings: _settings);
#if UNITY_EDITOR || DEBUG
            string plain_path = string.Format(SNAPSHOT_PATH_TEMPLATE, slot);
            string plain_full = Path.Combine(base_dir, plain_path);
            await File.WriteAllTextAsync(plain_full, text);
#else
            string enc_path = string.Format(SNAPSHOT_ENC_PATH_TEMPLATE, slot);
            string enc_full = Path.Combine(base_dir, enc_path);
            byte[] enc_bytes = await EncryptAesAsync(plain_text: text, key: Vault.GetKey().key, iv: Vault.GetKey().iv);
            await File.WriteAllBytesAsync(enc_full, enc_bytes);
#endif
        }

        /// <summary>
        /// Returns true if a snapshot exists for the specified slot (plaintext or encrypted).
        /// </summary>
        public static Task<bool> SnapshotExistsAsync(int slot) {
            string base_dir = getStreamingAssetsPath();
            string plain_full = Path.Combine(base_dir, string.Format(SNAPSHOT_PATH_TEMPLATE, slot));
            string enc_full = Path.Combine(base_dir, string.Format(SNAPSHOT_ENC_PATH_TEMPLATE, slot));
            bool exists = File.Exists(plain_full) || File.Exists(enc_full);
            return Task.FromResult(exists);
        }

        /// <summary>
        /// Deletes the snapshot at the specified slot (both plaintext and encrypted, if present).
        /// </summary>
        public static Task DeleteSnapshotAsync(int slot) {
            string base_dir = getStreamingAssetsPath();
            string plain_full = Path.Combine(base_dir, string.Format(SNAPSHOT_PATH_TEMPLATE, slot));
            string enc_full = Path.Combine(base_dir, string.Format(SNAPSHOT_ENC_PATH_TEMPLATE, slot));
            if (File.Exists(plain_full)) {
                File.Delete(plain_full);
            }
            if (File.Exists(enc_full)) {
                File.Delete(enc_full);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the StreamingAssets path appropriate for the current platform.
        /// </summary>
        private static string getStreamingAssetsPath() {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.Application.streamingAssetsPath;
#else
            return "StreamingAssets";
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods

        /// <summary>
        /// Asynchronously decrypts AES-encrypted binary data to a JSON string.
        /// </summary>
        static async Task<string> DecryptAesAsync(byte[] data, byte[] key, byte[] iv)        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return await sr.ReadToEndAsync();
        }

        /// <summary>
        /// Asynchronously encrypts a JSON string to AES-encrypted binary data.
        /// </summary>
        static async Task<byte[]> EncryptAesAsync(string plain_text, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var ms = new MemoryStream();
            await using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            await using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                await sw.WriteAsync(plain_text);
            }
            return ms.ToArray();
        }
    }
}
