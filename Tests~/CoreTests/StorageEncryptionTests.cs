// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Tests verifying that Storage uses Vault for AES key material (no hardcoded keys).
    /// RED before Vault integration; GREEN after.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class StorageEncryptionTests {

        string _temp_dir = null!;

        // 48 bytes (32 key + 16 IV) — identical material used in VaultTests.
        static readonly string DUMMY_KEY_B64 = Convert.ToBase64String(
            new byte[] {
                 1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, 16,
                17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
                33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48
            });

        [SetUp]
        public void SetUp() {
            _temp_dir = Path.Combine(Path.GetTempPath(), "germio_enc_test");
            Directory.CreateDirectory(_temp_dir);
            foreach (var f in Directory.GetFiles(_temp_dir)) { File.Delete(f); }
            Environment.SetEnvironmentVariable("GERMIO_AES_KEY", null);
        }

        [TearDown]
        public void TearDown() {
            Environment.SetEnvironmentVariable("GERMIO_AES_KEY", null);
            if (Directory.Exists(_temp_dir)) { Directory.Delete(_temp_dir, true); }
        }

        /// <summary>
        /// RED test: SaveAsync with encrypt=true must throw InvalidOperationException
        /// when no key source is available (GERMIO_AES_KEY not set, no Unity env).
        /// Currently FAILS because Storage uses hardcoded keys.
        /// After Vault integration: PASSES.
        /// </summary>
        [Test]
        public void SaveAsync_Encrypted_ThrowsWhenNoVaultKey() {
            var data = make_minimal_root();
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await Storage.SaveAsync(data: data, encrypt: true, base_path: _temp_dir));
        }

        /// <summary>
        /// With GERMIO_AES_KEY set, SaveAsync with encrypt=true must succeed
        /// and write a .dat file.
        /// </summary>
        [Test]
        public async Task SaveAsync_Encrypted_SucceedsWithVaultKey() {
            Environment.SetEnvironmentVariable("GERMIO_AES_KEY", DUMMY_KEY_B64);
            var data = make_minimal_root();
            await Storage.SaveAsync(data: data, encrypt: true, base_path: _temp_dir);
            Assert.That(File.Exists(Path.Combine(_temp_dir, "germio.dat")), Is.True);
        }

        /// <summary>
        /// Full encrypted round-trip: SaveAsync (encrypt=true) then LoadAsync must
        /// restore the same Scenario when Vault key is available.
        /// </summary>
        [Test]
        public async Task SaveAndLoad_EncryptedDat_RoundTrip_WithVaultKey() {
            Environment.SetEnvironmentVariable("GERMIO_AES_KEY", DUMMY_KEY_B64);
            var data = make_minimal_root();
            await Storage.SaveAsync(data: data, encrypt: true, base_path: _temp_dir);

            // LoadAsync must pick up .dat (no .json present)
            var loaded = await Storage.LoadAsync(base_path: _temp_dir);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.root.children.Count,         Is.EqualTo(1));
            Assert.That(loaded.root.children[0].id,          Is.EqualTo("test_world"));
            Assert.That(loaded.root.children[0].children[0].id, Is.EqualTo("test_level"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods

        Scenario make_minimal_root() => new Scenario {
            initial_state = new State(),
            root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> {
                    new Node {
                        id     = "test_world",
                        name   = "Test World",
                        kind   = "world",
                        scene  = "",
                        children = new List<Node> {
                            new Node {
                                id     = "test_level",
                                name   = "Test Level",
                                kind   = "level",
                                scene  = "TestScene",
                                next   = new List<Next>(),
                                rules = new List<Rule>()
                            }
                        }
                    }
                }
            }
        };
    }
}