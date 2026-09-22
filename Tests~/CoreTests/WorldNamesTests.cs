// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for <see cref="WorldNames"/> (germio TASK-067,
    /// docs/modio_spec.md's own INameSource shape, held here with no
    /// tie to modio at all).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class WorldNamesTests {

        [Test, Description("Fill with three given entries; NameOf on each reads back its own true Kind and ID, whole")]
        public void Fill_ThreeGivenEntries_NameOfReadsEachBackWhole() {
            var table = new WorldNames();
            table.Fill(new List<(int, string)> {
                (1, "Ground"),
                (2, "Block"),
                (3, "Human")
            });

            Assert.That(table.NameOf(1), Is.EqualTo(("Ground", "g_1")));
            Assert.That(table.NameOf(2), Is.EqualTo(("Block", "g_2")));
            Assert.That(table.NameOf(3), Is.EqualTo(("Human", "g_3")));
        }

        [Test, Description("NameOf on an id never handed to Fill reads (\"\", \"\") — never thrown, never null")]
        public void NameOf_AnIdNeverGiven_ReadsEmptyKindAndID() {
            var table = new WorldNames();
            table.Fill(new List<(int, string)> { (1, "Ground") });

            Assert.That(table.NameOf(99), Is.EqualTo(("", "")));
        }

        [Test, Description("Calling Fill a second time throws away every row the first Fill held")]
        public void Fill_ASecondTime_ThrowsAwayTheFirstScenesOwnRows() {
            var table = new WorldNames();
            table.Fill(new List<(int, string)> { (1, "Ground") });

            table.Fill(new List<(int, string)> { (2, "Block") });

            Assert.That(table.NameOf(1), Is.EqualTo(("", "")), "The first scene's own id must not survive a second Fill.");
            Assert.That(table.NameOf(2), Is.EqualTo(("Block", "g_2")));
        }

        [Test, Description("Fill with the same InstanceId given twice never throws; the last given Kind holds")]
        public void Fill_TheSameInstanceIdTwice_NeverThrowsAndTheLastKindHolds() {
            var table = new WorldNames();

            Assert.DoesNotThrow(() => table.Fill(new List<(int, string)> {
                (1, "Ground"),
                (1, "Block")
            }));
            Assert.That(table.NameOf(1), Is.EqualTo(("Block", "g_1")));
        }

        [Test, Description("NameOf, called many times against a built table, makes no garbage")]
        public void NameOf_CalledManyTimes_MakesNoGarbage() {
            var table = new WorldNames();
            table.Fill(new List<(int, string)> { (1, "Ground"), (2, "Block"), (3, "Human") });

            // warm up (JIT)
            table.NameOf(1);

            long before = System.GC.GetTotalAllocatedBytes(precise: true);
            for (int i = 0; i < 10000; i++) {
                table.NameOf(1);
                table.NameOf(2);
                table.NameOf(99);
            }
            long after = System.GC.GetTotalAllocatedBytes(precise: true);

            Assert.That(after - before, Is.EqualTo(0),
                "NameOf is the true hot-path read; it must make nothing new at all.");
        }
    }
}
