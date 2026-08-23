// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using NUnit.Framework;

using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for Vault (key management utility).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class VaultTests {

        [OneTimeSetUp]
        public void SetUpEnv() {
            // 48 bytes (32 key + 16 IV) encoded as Base64 — required by Vault.GetKey().
            byte[] material = new byte[48];
            for (int i = 0; i < 48; i++) { material[i] = (byte)(i + 1); }
            Environment.SetEnvironmentVariable("GERMIO_AES_KEY", Convert.ToBase64String(material));
        }

        [OneTimeTearDown]
        public void TearDownEnv() {
            Environment.SetEnvironmentVariable("GERMIO_AES_KEY", null);
        }

        [Test]
        public void GetKey_ReturnsValidKey() {
            var key = Vault.GetKey();
            Assert.That(key.key, Is.Not.Null);
            Assert.That(key.key, Has.Length.EqualTo(32));
            Assert.That(key.iv,  Has.Length.EqualTo(16));
        }

        [Test]
        public void GetKey_ReturnsSameKeyOnSubsequentCalls() {
            var key1 = Vault.GetKey();
            var key2 = Vault.GetKey();
            Assert.That(key1, Is.EqualTo(key2));
        }
    }
}