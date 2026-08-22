// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Germio.Model;

namespace Germio.Core {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Puts a found id in place of the $target mark.
    ///
    /// A rule as written names everything up front, but a deed cannot: what it
    /// finds is known only once it looks. So $target stands for whatever was
    /// found, and is put aside for the real id before the Evaluator or the
    /// Executor ever sees the line.
    ///
    /// See modio's own docs/modio_spec.md §7.7.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class TargetMark {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Const [nouns]

        /// <summary>The mark a deed writes where it cannot yet name what it will find.</summary>
        public const string MARK = "$target";

        /// <summary>
        /// The letter every written-out id takes in front. ExprLexer reads a
        /// value inside history.count(...) as an Identifier, and an Identifier is
        /// [a-zA-Z_][a-zA-Z0-9_-]* — it may not start with a number. Unity's own
        /// GetInstanceID() gives back a plain number, so this letter is what makes
        /// a written-out id hold.
        /// </summary>
        public const string ID_PREFIX = "g_";

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Writes out an instance id as text an expression may hold.
        /// </summary>
        /// <param name="instance_id">Unity's own GetInstanceID() value.</param>
        public static string WriteID(int instance_id) {
            return $"{ID_PREFIX}{instance_id}";
        }

        /// <summary>
        /// Gives back the text with every $target mark put aside for the id.
        ///
        /// The mark is a whole word: $targets and $targe are other words, and are
        /// left alone. Big and small letters count. What is put in is never looked
        /// at a second time, so an id holding the mark itself cannot run away with
        /// itself.
        /// </summary>
        /// <param name="text">The text to look through. Null comes back null.</param>
        /// <param name="id">The written-out id to put in place.</param>
        public static string? PutInPlace(string? text, string id) {
            if (text == null) { return null; }
            if (text.Length == 0) { return text; }
            if (!text.Contains(value: MARK)) { return text; }

            var built = new System.Text.StringBuilder(capacity: text.Length);
            int at = 0;
            while (at < text.Length) {
                int found = text.IndexOf(value: MARK, startIndex: at, comparisonType: System.StringComparison.Ordinal);
                if (found < 0) {
                    built.Append(value: text, startIndex: at, count: text.Length - at);
                    break;
                }

                built.Append(value: text, startIndex: at, count: found - at);

                // The mark is a whole word: a letter, a number, a low line or a
                // hyphen right after it makes another word, which is left alone.
                int after = found + MARK.Length;
                bool word_goes_on = after < text.Length
                    && (char.IsLetterOrDigit(c: text[after]) || text[after] == '_' || text[after] == '-');

                if (word_goes_on) {
                    built.Append(value: MARK);
                } else {
                    built.Append(value: id);
                }
                at = after;
            }
            return built.ToString();
        }

        /// <summary>
        /// Puts the id in place through every text field a Command holds, however
        /// deep. A deed's own held Command is reached this way, so a record_event
        /// naming $target comes out naming the thing the deed truly found.
        /// </summary>
        /// <param name="command">The Command to work through. Changed in place.</param>
        /// <param name="id">The written-out id to put in place.</param>
        public static void PutInPlaceOn(Command? command, string id) {
            if (command == null) { return; }

            if (command.set_flag != null) {
                command.set_flag.key = PutInPlace(text: command.set_flag.key, id: id) ?? string.Empty;
            }
            if (command.update_counter != null) {
                command.update_counter.key = PutInPlace(text: command.update_counter.key, id: id) ?? string.Empty;
            }
            if (command.update_inventory != null) {
                command.update_inventory.key = PutInPlace(text: command.update_inventory.key, id: id) ?? string.Empty;
            }
            if (command.update_need != null) {
                foreach (var need in command.update_need) {
                    need.key = PutInPlace(text: need.key, id: id) ?? string.Empty;
                }
            }
            if (command.record_event != null) {
                command.record_event.kind = PutInPlace(text: command.record_event.kind, id: id) ?? string.Empty;
                command.record_event.target_id = PutInPlace(text: command.record_event.target_id, id: id) ?? string.Empty;
            }
            if (command.request_notify != null) {
                command.request_notify = PutInPlace(text: command.request_notify, id: id);
            }
            if (command.request_transition != null) {
                command.request_transition = PutInPlace(text: command.request_transition, id: id);
            }
            // A request_deed inside a request_deed is not let through (V032), so
            // nothing goes deeper than this.
        }
    }
}
