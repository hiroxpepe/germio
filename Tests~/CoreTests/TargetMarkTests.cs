// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for putting a found id in place of the $target mark
    /// (germio TASK-032 to TASK-034, TASK-046 to TASK-050).
    /// A deed cannot name up front what it has not yet found, so $target stands
    /// for it until the deed looks. See modio's own docs/modio_spec.md §7.7.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class TargetMarkTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // TASK-049: writing out an id

        [Test, Description("An id is written out with a letter in front of it")]
        public void WriteID_PutsALetterInFront() {
            string written = TargetMark.WriteID(instance_id: 1042);

            Assert.That(written, Is.EqualTo("g_1042"));
        }

        [Test, Description("A written-out id holds no number at its front")]
        public void WriteID_NeverStartsWithANumber() {
            foreach (int id in new[] { 1, 42, 1042, -7, int.MaxValue }) {
                string written = TargetMark.WriteID(instance_id: id);

                Assert.That(char.IsDigit(written[0]), Is.False,
                    $"'{written}' must not start with a number: ExprLexer reads an Identifier as [a-zA-Z_][a-zA-Z0-9_-]*");
            }
        }

        [Test, Description("A written-out id runs through the Evaluator with nothing thrown")]
        public void WriteID_ParsesInsideAHistoryCall() {
            string written = TargetMark.WriteID(instance_id: 1042);
            string line = $"history.count(kind=met, target_id={written}) == 0";

            var state = new State();
            var history = new History();

            Assert.DoesNotThrow(() => Evaluator.Evaluate(
                condition: line, state: state, history: history));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-032: putting the id in place

        [Test, Description("The mark gives way to the id it was given")]
        public void PutInPlace_SwapsTheMark() {
            string got = TargetMark.PutInPlace(text: "target_id=$target", id: "g_1042");

            Assert.That(got, Is.EqualTo("target_id=g_1042"));
        }

        [Test, Description("A whole line of a condition comes back with the id in place")]
        public void PutInPlace_WholeCondition() {
            string got = TargetMark.PutInPlace(
                text: "history.time_since(kind=met, target_id=$target) > 60", id: "g_1042");

            Assert.That(got, Is.EqualTo("history.time_since(kind=met, target_id=g_1042) > 60"));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-033: text holding no mark

        [Test, Description("Text holding no mark comes back just as it was")]
        public void PutInPlace_NoMark_ComesBackUnchanged() {
            string got = TargetMark.PutInPlace(text: "flags.gate == true", id: "g_1042");

            Assert.That(got, Is.EqualTo("flags.gate == true"));
        }

        [Test, Description("Empty text comes back empty")]
        public void PutInPlace_EmptyText_ComesBackEmpty() {
            string got = TargetMark.PutInPlace(text: string.Empty, id: "g_1042");

            Assert.That(got, Is.EqualTo(string.Empty));
        }

        [Test, Description("Text that is not there at all comes back as it was")]
        public void PutInPlace_NullText_ComesBackNull() {
            string? got = TargetMark.PutInPlace(text: null, id: "g_1042");

            Assert.That(got, Is.Null);
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-034: every mark, not the first alone

        [Test, Description("Every mark in a line gives way, not the first alone")]
        public void PutInPlace_EveryMark_GivesWay() {
            string got = TargetMark.PutInPlace(
                text: "like=$target, target_id=$target", id: "g_1042");

            Assert.That(got, Is.EqualTo("like=g_1042, target_id=g_1042"));
        }

        [Test, Description("Two marks side by side both give way")]
        public void PutInPlace_MarksSideBySide_BothGiveWay() {
            string got = TargetMark.PutInPlace(text: "$target$target", id: "g_1042");

            Assert.That(got, Is.EqualTo("g_1042g_1042"));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-046: the mark is a whole word, and letters count

        [Test, Description("$targets is another word, and is left alone")]
        public void PutInPlace_LongerWord_LeftAlone() {
            string got = TargetMark.PutInPlace(text: "like=$targets", id: "g_1042");

            Assert.That(got, Is.EqualTo("like=$targets"));
        }

        [Test, Description("$targe is not the mark, and is left alone")]
        public void PutInPlace_ShorterWord_LeftAlone() {
            string got = TargetMark.PutInPlace(text: "like=$targe", id: "g_1042");

            Assert.That(got, Is.EqualTo("like=$targe"));
        }

        [Test, Description("$TARGET is not the mark, since letters count")]
        public void PutInPlace_BigLetters_LeftAlone() {
            string got = TargetMark.PutInPlace(text: "like=$TARGET", id: "g_1042");

            Assert.That(got, Is.EqualTo("like=$TARGET"));
        }

        [Test, Description("A mark with something before it still gives way")]
        public void PutInPlace_MarkAfterOtherText_GivesWay() {
            string got = TargetMark.PutInPlace(text: "id:$target", id: "g_1042");

            Assert.That(got, Is.EqualTo("id:g_1042"));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-047: once, and never again

        [Test, Description("An id holding the mark itself is put in once, and not looked at again")]
        public void PutInPlace_IdHoldingTheMark_StopsAfterOnce() {
            string got = TargetMark.PutInPlace(text: "target_id=$target", id: "$target");

            Assert.That(got, Is.EqualTo("target_id=$target"),
                "What is put in must never be looked at a second time.");
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-048: reaching every text field inside a held Command

        [Test, Description("A record_event inside a deed's own Command takes the id")]
        public void PutInPlaceOn_Command_ReachesRecordEvent() {
            var command = new Command {
                record_event = new RecordEvent { kind = "met", target_id = "$target" }
            };

            TargetMark.PutInPlaceOn(command: command, id: "g_1042");

            Assert.That(command.record_event!.target_id, Is.EqualTo("g_1042"));
        }

        [Test, Description("A set_flag key inside a deed's own Command takes the id")]
        public void PutInPlaceOn_Command_ReachesSetFlagKey() {
            var command = new Command {
                set_flag = new SetFlag { key = "seen_$target", value = true }
            };

            TargetMark.PutInPlaceOn(command: command, id: "g_1042");

            Assert.That(command.set_flag!.key, Is.EqualTo("seen_g_1042"));
        }

        [Test, Description("An update_need key inside a deed's own Command takes the id")]
        public void PutInPlaceOn_Command_ReachesUpdateNeed() {
            var command = new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "curiosity", delta = -25f },
                    new UpdateNeed { key = "seen_$target", delta = -5f }
                }
            };

            TargetMark.PutInPlaceOn(command: command, id: "g_1042");

            Assert.That(command.update_need![0].key, Is.EqualTo("curiosity"));
            Assert.That(command.update_need![1].key, Is.EqualTo("seen_g_1042"));
        }

        [Test, Description("A Command holding no mark anywhere comes through unchanged")]
        public void PutInPlaceOn_Command_NoMark_Unchanged() {
            var command = new Command {
                set_flag = new SetFlag { key = "gate_open", value = true },
                request_notify = "level_clear"
            };

            TargetMark.PutInPlaceOn(command: command, id: "g_1042");

            Assert.That(command.set_flag!.key, Is.EqualTo("gate_open"));
            Assert.That(command.request_notify, Is.EqualTo("level_clear"));
        }

        [Test, Description("An empty Command throws nothing")]
        public void PutInPlaceOn_EmptyCommand_DoesNotThrow() {
            var command = new Command();

            Assert.DoesNotThrow(() => TargetMark.PutInPlaceOn(command: command, id: "g_1042"));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-050: run through the Evaluator, once put in place

        [Test, Description("A put-in-place condition runs through the Evaluator")]
        public void PutInPlace_ThenEvaluate_GivesBackTrueOrFalse() {
            string line = TargetMark.PutInPlace(
                text: "history.count(kind=met, target_id=$target) == 0", id: "g_1042")!;

            var state = new State();
            var history = new History();

            object result = Evaluator.Evaluate(condition: line, state: state, history: history);

            Assert.That(result, Is.TypeOf<bool>());
            Assert.That((bool)result, Is.True, "nothing has been met yet, so the count is zero");
        }

        [Test, Description("A put-in-place condition finds a row that was truly written")]
        public void PutInPlace_ThenEvaluate_FindsAWrittenRow() {
            string line = TargetMark.PutInPlace(
                text: "history.count(kind=met, target_id=$target) == 1", id: "g_1042")!;

            var state = new State();
            var history = new History();
            history.entries.Add(item: new HistoryEntry {
                kind = "met", target_id = "g_1042", timestamp = 12.4f
            });

            object result = Evaluator.Evaluate(condition: line, state: state, history: history);

            Assert.That((bool)result, Is.True);
        }
    }
}
