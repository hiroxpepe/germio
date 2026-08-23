// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Germio;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Integration tests for Storage (save/load round-trip).
    /// Verifies that the abstracted Scenario — including counters, inventory, fired_rules — survives
    /// a full async save/load cycle to the local file system.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class StorageIntegrationTests {
        string _test_path = null!;
        string _test_file = null!;
        string _test_dat  = null!;

        // 48 bytes (32 key + 16 IV) — same material as VaultTests.
        static readonly string DUMMY_KEY_B64 = Convert.ToBase64String(
            new byte[] {
                 1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, 16,
                17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
                33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48
            });

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            Environment.SetEnvironmentVariable("GERMIO_AES_KEY", DUMMY_KEY_B64);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            Environment.SetEnvironmentVariable("GERMIO_AES_KEY", null);
        }

        [SetUp]
        public void SetUp() {
            _test_path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Low", "StudioMeowToon", "Stemic");
            _test_file = Path.Combine(_test_path, "germio.json");
            _test_dat  = Path.Combine(_test_path, "germio.dat");
            Directory.CreateDirectory(_test_path);
            if (File.Exists(_test_dat)) { File.Delete(_test_dat); }
        }

        [Test]
        public async Task SaveAndLoad_GermioConfigJson_WorksCorrectly() {
            var data = new Scenario {
                initial_state = new State {
                    current_node = "level_1",
                    counters = new Map<string, float> {
                        ["score"] = 9999f,
                        ["depth"] = 3f
                    },
                    inventory = new Map<string, int> {
                        ["key_01"] = 2
                    }
                },
                root = new Node {
                    id = "root",
                    name = "Root",
                    kind = "root",
                    scene = "",
                    children = new System.Collections.Generic.List<Node> {
                        new Node {
                            id = "title",
                            name = "Title",
                            kind = "title",
                            scene = "Title"
                        },
                        new Node {
                            id = "select",
                            name = "Select",
                            kind = "select",
                            scene = "Select"
                        },
                        new Node {
                            id = "level_1",
                            name = "Level 1",
                            kind = "level",
                            scene = "Level1"
                        },
                        new Node {
                            id = "level_2",
                            name = "Level 2",
                            kind = "level",
                            scene = "Level2"
                        },
                        new Node {
                            id = "level_3",
                            name = "Level 3",
                            kind = "level",
                            scene = "Level3"
                        },
                        new Node {
                            id = "ending",
                            name = "Ending",
                            kind = "ending",
                            scene = "Ending"
                        }
                    }
                }
            };

            await Storage.SaveAsync(data: data, encrypt: false, base_path: _test_path);

            Assert.That(File.Exists(_test_file), Is.True, "germio.json should exist");

            var loaded = await Storage.LoadAsync(base_path: _test_path);
            Assert.That(loaded, Is.Not.Null,                       "Loaded data must not be null");
            Assert.That(loaded.root.children.Count, Is.EqualTo(6), "Must contain six level nodes");
            Assert.That(loaded.root.children[0].scene, Is.EqualTo("Title"),   "First node scene must be Title");
            Assert.That(loaded.root.children[5].scene, Is.EqualTo("Ending"),  "Last node scene must be Ending");

            // State round-trip assertions
            Assert.That(loaded.initial_state.counters["score"],     Is.EqualTo(9999f), "counter 'score' must survive save/load");
            Assert.That(loaded.initial_state.counters["depth"],     Is.EqualTo(3f),    "counter 'depth' must survive save/load");
            Assert.That(loaded.initial_state.inventory["key_01"],   Is.EqualTo(2),     "inventory 'key_01' must survive save/load");
        }
    }
}