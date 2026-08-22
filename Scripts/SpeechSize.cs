// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Germio {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// How big to draw a line spoken over a character's head.
    ///
    /// A line over one character's head shows what it has in mind. With modio
    /// driving characters, a great deal will need checking that way, and a mind
    /// that cannot be seen cannot be checked by eye.
    ///
    /// Drawing the line takes Unity; working out how big to draw it does not.
    /// That part sits here, apart, and is checked with no Unity at all — the same
    /// road modio's own docs/modio_spec.md 3.6 takes for seeking.
    ///
    /// The sums are carried over from super-nekokun's own Enemy.cs, which drew a
    /// line this way for years: the box and the letters both get smaller as the
    /// reader stands further off.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public readonly struct SpeechSize {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        SpeechSize(float across, float high, int letter) {
            Across = across;
            High = high;
            Letter = letter;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        /// <summary>How far across to draw the box.</summary>
        public float Across { get; }

        /// <summary>How high to draw the box.</summary>
        public float High { get; }

        /// <summary>How big to draw the letters. Never below one.</summary>
        public int Letter { get; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Works out how big a line should be drawn, given how far off the reader
        /// stands. Nothing ever comes out at nothing: a box of no size, or letters
        /// of no size, would draw nothing at all.
        /// </summary>
        /// <param name="base_across">How far across the box is, drawn close by.</param>
        /// <param name="base_high">How high the box is, drawn close by.</param>
        /// <param name="base_letter">How big the letters are, drawn close by.</param>
        /// <param name="distance">How far off the reader stands.</param>
        public static SpeechSize Work(float base_across, float base_high, int base_letter, float distance) {
            // super-nekokun's own sum: halve the distance, and never go below one.
            // A distance below zero is taken as none at all.
            float held = distance > 0f ? distance : 0f;
            int steps = held > 1f ? (int) (held / 2f) : 1;
            if (steps < 1) { steps = 1; }

            float across = base_across / steps;
            float high = base_high / steps;

            // The letters get smaller faster than the box, as they did before.
            int letter = (int) (base_letter / (steps * 1.25f));
            if (letter < 1) { letter = 1; }

            return new SpeechSize(across: across, high: high, letter: letter);
        }

        /// <summary>
        /// Tells whether a line is worth drawing at all.
        ///
        /// Master's own word: 64 characters run at once, every one of them, seen
        /// or not. Drawing a line for each would cost for nothing — so this is
        /// asked first, and asked away from Unity, where it is cheap.
        /// </summary>
        /// <param name="distance">How far off the reader stands.</param>
        /// <param name="reach">How far a line may be read from.</param>
        public static bool WorthDrawing(float distance, float reach) {
            return distance <= reach;
        }
    }
}
