// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

using Germio;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for working out how big a spoken line should be drawn
    /// (germio TASK-043).
    ///
    /// A line over one character's head shows what it has in mind. With modio
    /// driving characters, a great deal will need checking that way, and a mind
    /// that cannot be seen cannot be checked by eye.
    ///
    /// Drawing the line takes Unity; **working out how big to draw it does not**.
    /// That part sits here, apart, and is checked with no Unity at all — the same
    /// road §3.6 of modio's own spec takes for seeking.
    ///
    /// The sums are carried over from super-nekokun's own Enemy.cs, which drew a
    /// line this way for years.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class SpeechSizeTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // How far off a reader stands

        [Test, Description("A line close by is drawn at its full size")]
        public void SizeAt_CloseBy_IsFullSize() {
            SpeechSize size = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 1.0f);

            Assert.That(size.Across, Is.EqualTo(200f));
            Assert.That(size.High, Is.EqualTo(100f));
        }

        [Test, Description("A line further off is drawn smaller")]
        public void SizeAt_FurtherOff_IsSmaller() {
            SpeechSize near = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 2.0f);
            SpeechSize far = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 10.0f);

            Assert.That(far.Across, Is.LessThan(near.Across));
            Assert.That(far.High, Is.LessThan(near.High));
        }

        [Test, Description("The font shrinks along with the box")]
        public void SizeAt_FurtherOff_FontShrinksToo() {
            SpeechSize near = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 1.0f);
            SpeechSize far = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 8.0f);

            Assert.That(far.Letter, Is.LessThan(near.Letter));
        }

        ///////////////////////////////////////////////////////////////////////
        // Nothing may ever come out at nothing

        [Test, Description("A line right on top of the reader is still drawn")]
        public void SizeAt_DistanceOfZero_IsStillDrawn() {
            SpeechSize size = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 0f);

            Assert.That(size.Across, Is.GreaterThan(0f));
            Assert.That(size.High, Is.GreaterThan(0f));
            Assert.That(size.Letter, Is.GreaterThan(0));
        }

        [Test, Description("A line a long way off is still drawn, however small")]
        public void SizeAt_VeryFarOff_IsStillDrawn() {
            SpeechSize size = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 1000f);

            Assert.That(size.Across, Is.GreaterThan(0f));
            Assert.That(size.High, Is.GreaterThan(0f));
            Assert.That(size.Letter, Is.GreaterThan(0), "A font of nothing draws nothing at all.");
        }

        [Test, Description("A distance below zero is taken as no distance")]
        public void SizeAt_DistanceBelowZero_IsTakenAsNone() {
            SpeechSize below = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: -5f);
            SpeechSize none = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 0f);

            Assert.That(below.Across, Is.EqualTo(none.Across));
            Assert.That(below.Letter, Is.EqualTo(none.Letter));
        }

        ///////////////////////////////////////////////////////////////////////
        // Same in, same out

        [Test, Description("The same distance always gives back the same size")]
        public void SizeAt_SameDistance_SameSize() {
            SpeechSize first = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 5.0f);
            SpeechSize second = SpeechSize.Work(base_across: 200f, base_high: 100f,
                base_letter: 60, distance: 5.0f);

            Assert.That(second.Across, Is.EqualTo(first.Across));
            Assert.That(second.High, Is.EqualTo(first.High));
            Assert.That(second.Letter, Is.EqualTo(first.Letter));
        }

        ///////////////////////////////////////////////////////////////////////
        // Whether a line is worth drawing at all

        [Test, Description("A line close enough is worth drawing")]
        public void WorthDrawing_CloseEnough_IsTrue() {
            Assert.That(SpeechSize.WorthDrawing(distance: 5.0f, reach: 30.0f), Is.True);
        }

        [Test, Description("A line too far off is not worth drawing")]
        public void WorthDrawing_TooFarOff_IsFalse() {
            Assert.That(SpeechSize.WorthDrawing(distance: 50.0f, reach: 30.0f), Is.False);
        }

        [Test, Description("A line right at the edge of reach is worth drawing")]
        public void WorthDrawing_RightAtReach_IsTrue() {
            Assert.That(SpeechSize.WorthDrawing(distance: 30.0f, reach: 30.0f), Is.True);
        }

        [Test, Description("With 64 characters running, most lines are not worth drawing")]
        public void WorthDrawing_ManyCharacters_MostAreNot() {
            // Master's own word: 64 characters, every one of them running, seen
            // or not. Drawing a line for each would cost for nothing.
            int worth = 0;
            for (int i = 0; i < 64; i++) {
                float distance = i * 2.0f;   // spread out across the field
                if (SpeechSize.WorthDrawing(distance: distance, reach: 30.0f)) { worth++; }
            }

            Assert.That(worth, Is.LessThan(64),
                "Working out which lines are worth drawing is why this sits apart from Unity.");
        }
    }
}
