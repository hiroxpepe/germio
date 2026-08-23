// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using NUnit.Framework;

using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Tests that Vault.GetKey() uses only secure key sources (env var, germio_key.bin)
    /// with no PlayerPrefs fallback, conforming to G6 (secure key management).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class VaultSecureStoreTests {
#nullable enable
        const string ENV_VAR = "GERMIO_AES_KEY";

        [TearDown]
        public void TearDown() {
            Environment.SetEnvironmentVariable(ENV_VAR, null);
        }

        [Test]
        public void GetKey_WithValidEnvVar_ReturnsKeyAndIv() {
            var material = new byte[48];
            for (int i = 0; i < 48; i++) { material[i] = (byte)(i + 10); }
            Environment.SetEnvironmentVariable(ENV_VAR, Convert.ToBase64String(material));

            var (key, iv) = Vault.GetKey();

            Assert.That(key, Has.Length.EqualTo(32));
            Assert.That(iv, Has.Length.EqualTo(16));
        }

        [Test]
        public void GetKey_WithShortMaterial_ThrowsInvalidOperationException() {
            var material = new byte[16]; // too short (need 48)
            Environment.SetEnvironmentVariable(ENV_VAR, Convert.ToBase64String(material));

            Assert.Throws<InvalidOperationException>(() => Vault.GetKey());
        }

        [Test]
        public void GetKey_WithNoSource_ThrowsInvalidOperationException() {
            Environment.SetEnvironmentVariable(ENV_VAR, null);

            Assert.Throws<InvalidOperationException>(() => Vault.GetKey());
        }

        [Test]
        public void GetKey_WithNoSource_ErrorMessageMentionsEnvVar() {
            Environment.SetEnvironmentVariable(ENV_VAR, null);

            var ex = Assert.Throws<InvalidOperationException>(() => Vault.GetKey());

            Assert.That(ex!.Message, Does.Contain(ENV_VAR),
                "Error message must mention the GERMIO_AES_KEY env var as a key source.");
        }

        [Test]
        public void GetKey_WithNoSource_ErrorMessageDoesNotMentionPlayerPrefs() {
            Environment.SetEnvironmentVariable(ENV_VAR, null);

            var ex = Assert.Throws<InvalidOperationException>(() => Vault.GetKey());

            // G6: PlayerPrefs must not appear as a fallback key source in error output
            Assert.That(ex!.Message, Does.Not.Contain("PlayerPrefs"),
                "G6 violation: Vault error message must not reference PlayerPrefs.");
        }

        [Test]
        public void GetKey_KeyAndIv_AreSlicedCorrectly() {
            var material = new byte[48];
            for (int i = 0; i < 32; i++) { material[i] = (byte)(i + 1); }        // key: 1..32
            for (int i = 0; i < 16; i++) { material[32 + i] = (byte)(i + 100); } // iv: 100..115
            Environment.SetEnvironmentVariable(ENV_VAR, Convert.ToBase64String(material));

            var (key, iv) = Vault.GetKey();

            Assert.That(key[0],  Is.EqualTo(1));
            Assert.That(key[31], Is.EqualTo(32));
            Assert.That(iv[0],   Is.EqualTo(100));
            Assert.That(iv[15],  Is.EqualTo(115));
        }
    }
}