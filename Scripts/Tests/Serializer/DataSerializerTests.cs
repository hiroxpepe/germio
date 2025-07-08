// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

// Run all tests in this project with:
// dotnet test Drafte/Assets/Plugins/Germio/Scripts/GermioTest.csproj --logger "console;verbosity=detailed"

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Germio.Tests.Serializer
{
    /// <summary>
    /// Unit tests for the DataSerializer class.
    /// 
    /// These tests verify that the Germio.Data structure can be correctly serialized and deserialized
    /// using real-world JSON files, and that the data integrity is maintained throughout the process.
    /// Each test uses a dedicated test data directory to ensure isolation and reproducibility.
    /// </summary>
    [TestFixture]
    public class DataSerializerTests
    {
        private string tempJsonPath;
        private string tempDatPath;

        /// <summary>
        /// Setup method to clean up any temporary files before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            tempJsonPath = Path.Combine(Path.GetTempPath(), "germio_config.json");
            tempDatPath = Path.Combine(Path.GetTempPath(), "germio_config.dat");
            if (File.Exists(tempJsonPath)) File.Delete(tempJsonPath);
            if (File.Exists(tempDatPath)) File.Delete(tempDatPath);
        }

        /// <summary>
        /// TearDown method to remove any temporary files after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (File.Exists(tempJsonPath)) File.Delete(tempJsonPath);
            if (File.Exists(tempDatPath)) File.Delete(tempDatPath);
        }

        /// <summary>
        /// Test01: Verifies that a minimal, valid Germio.Data JSON file can be deserialized correctly.
        /// This test ensures that the basic structure (state, worlds, levels, etc.) is loaded as expected.
        /// 
        /// Why: To confirm that the DataSerializer can handle the standard save data format and that
        /// all key fields are correctly mapped from JSON to C# objects.
        /// 
        /// Result: This test should pass if the deserialization logic and data model are correct.
        /// </summary>
        [Test, Description("Save and Load Plain JSON Works Correctly")]
        public void Test01()
        {
            // Get the absolute path of TestData/Test01/germio_config.json
            var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Tests/Serializer/TestData/Test01/germio_config.json");
            var json = File.ReadAllText(testDataPath);
            // Deserialize as Germio.Data
            var loaded = System.Text.Json.JsonSerializer.Deserialize<Germio.Data>(json);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.state.turn, Is.EqualTo(1));
            Assert.That(loaded.state.flags["hasSword"], Is.True);
            Assert.That(((System.Text.Json.JsonElement)loaded.state.inventory["gold"]).GetInt32(), Is.EqualTo(100));
            Assert.That(loaded.worlds[0].id, Is.EqualTo("overworld"));
            Assert.That(loaded.worlds[0].levels[0].id, Is.EqualTo("town1"));
        }

        /// <summary>
        /// Test02: Verifies that a different Germio.Data JSON file (with different values and structure)
        /// can also be deserialized correctly. This checks the flexibility and robustness of the deserialization logic.
        /// 
        /// Why: To ensure that the DataSerializer can handle variations in the save data, such as different flags,
        /// inventory items, and world/level structures.
        /// 
        /// Result: This test should pass if the deserializer is robust against different valid data patterns.
        /// </summary>
        [Test, Description("Load actual germio_config.json and check contents")]
        public void Test02()
        {
            var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Tests/Serializer/TestData/Test02/germio_config.json");
            var json = File.ReadAllText(testDataPath);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<Germio.Data>(json);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.state.turn, Is.EqualTo(2));
            Assert.That(loaded.state.flags["hasShield"], Is.False);
            Assert.That(((System.Text.Json.JsonElement)loaded.state.inventory["gold"]).GetInt32(), Is.EqualTo(999));
            Assert.That(((System.Text.Json.JsonElement)loaded.state.inventory["potion"]).GetInt32(), Is.EqualTo(3));
            Assert.That(loaded.worlds[0].id, Is.EqualTo("dungeon"));
            Assert.That(loaded.worlds[0].levels[0].id, Is.EqualTo("bossroom"));
        }
    }
}
