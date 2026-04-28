// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Germio.Model;

namespace Germio.Core
{
    /// <summary>
    /// Provides serialization and deserialization for game save data.
    /// Supports both plain JSON (for development) and AES-encrypted binary (for production).
    /// All file I/O is async to avoid blocking the main thread on Android storage.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Storage
    {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const string JSON_PATH = "germio_config.json";
        const string ENC_PATH = "germio_config.dat";

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
        public static async Task<Scenario?> LoadAsync(string? base_path = null)
        {
            string dir       = base_path ?? Directory.GetCurrentDirectory();
            string path_json = Path.Combine(dir, JSON_PATH);
            string path_enc  = Path.Combine(dir, ENC_PATH);
            if (File.Exists(path_json))
            {
                string json = await File.ReadAllTextAsync(path_json, Encoding.UTF8);
                return JsonConvert.DeserializeObject<Scenario>(json, _settings)!;
            }
            else if (File.Exists(path_enc))
            {
                var (key, iv) = Vault.GetKey();
                byte[] enc    = await File.ReadAllBytesAsync(path_enc);
                string json   = await DecryptAesAsync(data: enc, key: key, iv: iv);
                return JsonConvert.DeserializeObject<Scenario>(json, _settings)!;
            }
            return null;
        }

        /// <summary>
        /// Asynchronously saves game data to file.
        /// Writes as plain JSON in development, or as encrypted binary in production.
        /// </summary>
        /// <param name="data">The <see cref="Scenario"/> object to serialize and save.</param>
        /// <param name="encrypt">If true, saves as encrypted binary; otherwise, saves as plain JSON.</param>
        public static async Task SaveAsync(Scenario data, bool encrypt = false, string? base_path = null)
        {
            string dir       = base_path ?? Directory.GetCurrentDirectory();
            string path_json = Path.Combine(dir, JSON_PATH);
            string path_enc  = Path.Combine(dir, ENC_PATH);
            string json      = JsonConvert.SerializeObject(data, _settings);
            if (encrypt)
            {
                var (key, iv) = Vault.GetKey();
                byte[] enc    = await EncryptAesAsync(plain_text: json, key: key, iv: iv);
                await File.WriteAllBytesAsync(path_enc, enc);
            }
            else
            {
                await File.WriteAllTextAsync(path_json, json, Encoding.UTF8);
            }
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
