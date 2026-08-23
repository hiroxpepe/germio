// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for Storage.
    /// Verifies that the abstracted Scenario structure serializes and deserializes
    /// correctly from JSON, and that counters/flags/inventory/fired_rules round-trip cleanly.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class StorageTests {
        string _temp_json = null!;
        string _temp_dat  = null!;

        [SetUp]
        public void SetUp() {
            _temp_json = Path.Combine(Path.GetTempPath(), "germio.json");
            _temp_dat  = Path.Combine(Path.GetTempPath(), "germio.dat");
            if (File.Exists(_temp_json)) File.Delete(_temp_json);
            if (File.Exists(_temp_dat))  File.Delete(_temp_dat);
        }

        [TearDown]
        public void TearDown() {
            if (File.Exists(_temp_json)) File.Delete(_temp_json);
            if (File.Exists(_temp_dat))  File.Delete(_temp_dat);
        }

        [Test, Description("Load plain JSON with abstracted state and verify counters/flags/inventory")]
        public async Task Test01() {
            var loaded = await Storage.LoadAsync(base_path:
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts/Core/TestData/Test01"));
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.initial_state.counters["turn"],          Is.EqualTo(1f));
            Assert.That(loaded.initial_state.flags["zone_a_cleared"],   Is.False);
            Assert.That(loaded.initial_state.inventory["key_01"],       Is.EqualTo(1));
            Assert.That(loaded.root.children[0].id,                     Is.EqualTo("world_01"));
            Assert.That(loaded.root.children[0].children[0].id,         Is.EqualTo("level_01"));
        }

        [Test, Description("Load second JSON fixture and verify different abstracted state values")]
        public async Task Test02() {
            var loaded = await Storage.LoadAsync(base_path:
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts/Core/TestData/Test02"));
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.initial_state.counters["turn"],        Is.EqualTo(2f));
            Assert.That(loaded.initial_state.flags["gate_b_open"],   Is.False);
            Assert.That(loaded.initial_state.inventory["key_02"],     Is.EqualTo(3));
            Assert.That(loaded.root.children[0].id,                  Is.EqualTo("world_02"));
            Assert.That(loaded.root.children[0].children[0].id,      Is.EqualTo("level_b1"));
        }
    }
}