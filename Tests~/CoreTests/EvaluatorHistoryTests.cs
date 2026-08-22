// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for reading history inside && , || and ! (germio TASK-061).
    ///
    /// docs/dsl_spec.md §6 says a history.* call "does not work correctly when
    /// nested inside &&, || or !", and tells a writer to split such a rule in
    /// two. Measured 2026-08-22: **true && true gives back false**, so the limit
    /// is not a corner case at all — no history call works inside any of the
    /// three.
    ///
    /// The cause is plain: AndNode holds its two sides private, so the evaluator
    /// cannot reach them, and falls back to reading plain state alone. A history
    /// node throws there, and the throw is caught and turned into false.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class EvaluatorHistoryTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static History historyWith(params (string kind, string target_id, float at)[] rows) {
            var history = new History();
            foreach (var row in rows) {
                history.entries.Add(item: new HistoryEntry {
                    kind = row.kind, target_id = row.target_id, timestamp = row.at
                });
            }
            return history;
        }

        static State stateWith(params (string key, bool value)[] flags) {
            var state = new State();
            foreach (var flag in flags) { state.flags[flag.key] = flag.value; }
            return state;
        }

        ///////////////////////////////////////////////////////////////////////
        // AND

        [Test, Description("Two history calls, both true, give back true")]
        public void And_BothTrue_IsTrue() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.count(kind=met, target_id=g_1042) == 1 && "
                    + "history.count(kind=edge, target_id=g_1042) == 0",
                state: new State(), history: history);

            Assert.That(result, Is.True,
                "Explore asks two things at once: not met before, and not like a fall.");
        }

        [Test, Description("Two history calls, one false, give back false")]
        public void And_OneFalse_IsFalse() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.count(kind=met, target_id=g_1042) == 1 && "
                    + "history.count(kind=met, target_id=g_1042) == 0",
                state: new State(), history: history);

            Assert.That(result, Is.False);
        }

        [Test, Description("A history call beside a flag gives back true")]
        public void And_HistoryWithFlag_IsTrue() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.count(kind=met, target_id=g_1042) == 1 && flags.ready == true",
                state: stateWith(("ready", true)), history: history);

            Assert.That(result, Is.True);
        }

        [Test, Description("A flag before a history call works the same way round")]
        public void And_FlagWithHistory_IsTrue() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "flags.ready == true && history.count(kind=met, target_id=g_1042) == 1",
                state: stateWith(("ready", true)), history: history);

            Assert.That(result, Is.True);
        }

        [Test, Description("Three history calls in a row all hold")]
        public void And_ThreeInARow_IsTrue() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.count(kind=met, target_id=g_1042) == 1 && "
                    + "history.count(kind=edge, target_id=g_1042) == 0 && "
                    + "history.count(kind=gave, target_id=g_1042) == 0",
                state: new State(), history: history);

            Assert.That(result, Is.True);
        }

        ///////////////////////////////////////////////////////////////////////
        // OR

        [Test, Description("False or true gives back true")]
        public void Or_FalseThenTrue_IsTrue() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.count(kind=met, target_id=g_9999) == 1 || flags.ready == true",
                state: stateWith(("ready", true)), history: history);

            Assert.That(result, Is.True);
        }

        [Test, Description("False or false gives back false")]
        public void Or_BothFalse_IsFalse() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.count(kind=met, target_id=g_9999) == 1 || flags.ready == true",
                state: stateWith(("ready", false)), history: history);

            Assert.That(result, Is.False);
        }

        ///////////////////////////////////////////////////////////////////////
        // NOT

        [Test, Description("Not false gives back true")]
        public void Not_False_IsTrue() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "!(history.count(kind=met, target_id=g_1042) == 0)",
                state: new State(), history: history);

            Assert.That(result, Is.True);
        }

        [Test, Description("Not true gives back false")]
        public void Not_True_IsFalse() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "!(history.count(kind=met, target_id=g_1042) == 1)",
                state: new State(), history: history);

            Assert.That(result, Is.False);
        }

        ///////////////////////////////////////////////////////////////////////
        // Every history function, inside an AND

        [Test, Description("history.has works inside an AND")]
        public void And_HistoryHas_Works() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.has(kind=met, target_id=g_1042) && flags.ready == true",
                state: stateWith(("ready", true)), history: history);

            Assert.That(result, Is.True);
        }

        [Test, Description("history.time_since works inside an AND")]
        public void And_HistoryTimeSince_Works() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.time_since(kind=met, target_id=g_1042) == 10 && flags.ready == true",
                state: stateWith(("ready", true)), history: history);

            Assert.That(result, Is.True);
        }

        ///////////////////////////////////////////////////////////////////////
        // What stood before must go on standing

        [Test, Description("A history call standing alone still works")]
        public void Alone_StillWorks() {
            var history = historyWith(("met", "g_1042", 10f));

            object result = Evaluator.Evaluate(
                condition: "history.count(kind=met, target_id=g_1042) == 1",
                state: new State(), history: history);

            Assert.That(result, Is.True);
        }

        [Test, Description("A condition with no history at all still works")]
        public void NoHistory_StillWorks() {
            object result = Evaluator.Evaluate(
                condition: "flags.ready == true && counters.score >= 0",
                state: stateWith(("ready", true)), history: new History());

            Assert.That(result, Is.True);
        }
    }
}
