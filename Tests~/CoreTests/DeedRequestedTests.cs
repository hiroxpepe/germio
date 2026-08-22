// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for firing a deed out of the Store (germio TASK-027, TASK-041).
    /// germio starts a deed and hears no more of it: modio carries it out.
    /// See modio's own docs/modio_spec.md §7.11.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class DeedRequestedTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Helpers

        static Store buildStore() {
            var node = new Node();
            node.id = "lv_1";

            var scenario = new Scenario();
            scenario.initial_state = new State();
            scenario.initial_state.current_node = "lv_1";
            scenario.root = node;

            return new Store(scenario: scenario);
        }

        static RequestDeed buildDeed() {
            return new RequestDeed {
                target = new Target { kind = "Ground", reach = 15.0f, spread = 90.0f },
                condition = "history.time_since(kind=met, target_id=$target) > 60",
                motion = "walk",
                until = new Until { meets = "$target" },
                command = new Command {
                    update_need = new List<UpdateNeed> {
                        new UpdateNeed { key = "curiosity", delta = -25f }
                    }
                }
            };
        }

        ///////////////////////////////////////////////////////////////////////
        // Firing

        [Test, Description("One request_deed fires the event once")]
        public void Execute_RequestDeed_FiresOnce() {
            var store = buildStore();
            int fired = 0;
            store.DeedRequested += (deed) => { fired++; };

            Executor.Execute(command: new Command { request_deed = buildDeed() }, store: store);

            Assert.That(fired, Is.EqualTo(1));
        }

        [Test, Description("The event carries every one of the deed's own parts")]
        public void Execute_RequestDeed_CarriesEveryPart() {
            var store = buildStore();
            RequestDeed? got = null;
            store.DeedRequested += (deed) => { got = deed; };

            Executor.Execute(command: new Command { request_deed = buildDeed() }, store: store);

            Assert.That(got, Is.Not.Null);
            Assert.That(got!.target, Is.Not.Null);
            Assert.That(got.target!.kind, Is.EqualTo("Ground"));
            Assert.That(got.condition, Does.Contain("history.time_since"));
            Assert.That(got.motion, Is.EqualTo("walk"));
            Assert.That(got.until, Is.Not.Null);
            Assert.That(got.until!.meets, Is.EqualTo("$target"));
            Assert.That(got.command.update_need, Has.Count.EqualTo(1));
        }

        [Test, Description("A deed with no target fires just the same")]
        public void Execute_RequestDeed_NoTarget_FiresAnyway() {
            var store = buildStore();
            RequestDeed? got = null;
            store.DeedRequested += (deed) => { got = deed; };

            Executor.Execute(command: new Command {
                request_deed = new RequestDeed {
                    motion = "idle",
                    until = new Until { elapsed = 4.0f },
                    command = new Command()
                }
            }, store: store);

            Assert.That(got, Is.Not.Null);
            Assert.That(got!.target, Is.Null);
            Assert.That(got.motion, Is.EqualTo("idle"));
        }

        ///////////////////////////////////////////////////////////////////////
        // The held Command is not run here

        [Test, Description("The Command held inside a deed does not run when the deed is asked for")]
        public void Execute_RequestDeed_HeldCommandDoesNotRunYet() {
            var store = buildStore();
            int needs_fired = 0;
            store.NeedRequested += (key, delta) => { needs_fired++; };

            Executor.Execute(command: new Command { request_deed = buildDeed() }, store: store);

            Assert.That(needs_fired, Is.EqualTo(0),
                "A deed's own held Command runs only where the deed truly lands, and modio decides that.");
        }

        ///////////////////////////////////////////////////////////////////////
        // Nothing to fire

        [Test, Description("A command with no request_deed fires nothing")]
        public void Execute_NoRequestDeed_FiresNothing() {
            var store = buildStore();
            int fired = 0;
            store.DeedRequested += (deed) => { fired++; };

            Executor.Execute(command: new Command {
                set_flag = new SetFlag { key = "gate_open", value = true }
            }, store: store);

            Assert.That(fired, Is.EqualTo(0));
        }

        [Test, Description("Firing with no one listening throws nothing")]
        public void Execute_RequestDeed_NoListener_DoesNotThrow() {
            var store = buildStore();

            Assert.DoesNotThrow(() => Executor.Execute(
                command: new Command { request_deed = buildDeed() }, store: store));
        }

        ///////////////////////////////////////////////////////////////////////
        // Beside other commands

        [Test, Description("A request_deed fires beside a set_flag in the same command")]
        public void Execute_RequestDeedWithSetFlag_BothHappen() {
            var store = buildStore();
            int fired = 0;
            store.DeedRequested += (deed) => { fired++; };

            Executor.Execute(command: new Command {
                set_flag = new SetFlag { key = "gate_open", value = true },
                request_deed = buildDeed()
            }, store: store);

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(store.Scenario.initial_state.flags["gate_open"], Is.True);
        }

        ///////////////////////////////////////////////////////////////////////
        // Both new commands, held in one command

        [Test, Description("A command holding both an update_need and a request_deed fires both")]
        public void Execute_UpdateNeedAndRequestDeed_BothFire() {
            var store = buildStore();
            int needs_fired = 0;
            int deeds_fired = 0;
            store.NeedRequested += (key, delta) => { needs_fired++; };
            store.DeedRequested += (deed) => { deeds_fired++; };

            Executor.Execute(command: new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "curiosity", delta = -25f }
                },
                request_deed = buildDeed()
            }, store: store);

            Assert.That(needs_fired, Is.EqualTo(1));
            Assert.That(deeds_fired, Is.EqualTo(1),
                "The Executor runs a plain row of ifs, not a switch: both are taken.");
        }

        [Test, Description("A need fires before a deed, so an arrival is felt before the next is asked for")]
        public void Execute_UpdateNeedAndRequestDeed_NeedComesFirst() {
            var store = buildStore();
            var order = new List<string>();
            store.NeedRequested += (key, delta) => { order.Add(item: "need"); };
            store.DeedRequested += (deed) => { order.Add(item: "deed"); };

            Executor.Execute(command: new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "curiosity", delta = -25f }
                },
                request_deed = buildDeed()
            }, store: store);

            Assert.That(order, Is.EqualTo(new List<string> { "need", "deed" }));
        }

        [Test, Description("A whole command holding every kind at once takes each of them")]
        public void Execute_EveryKindAtOnce_TakesEach() {
            var store = buildStore();
            int needs_fired = 0;
            int deeds_fired = 0;
            int notifies_fired = 0;
            store.NeedRequested += (key, delta) => { needs_fired++; };
            store.DeedRequested += (deed) => { deeds_fired++; };
            store.NotifyRequested += (id) => { notifies_fired++; };

            Executor.Execute(command: new Command {
                set_flag = new SetFlag { key = "gate_open", value = true },
                record_event = new RecordEvent { kind = "met", target_id = "g_1042" },
                request_notify = "level_clear",
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "curiosity", delta = -25f },
                    new UpdateNeed { key = "fatigue", delta = -40f }
                },
                request_deed = buildDeed()
            }, store: store);

            Assert.That(store.Scenario.initial_state.flags["gate_open"], Is.True);
            Assert.That(needs_fired, Is.EqualTo(2));
            Assert.That(deeds_fired, Is.EqualTo(1));
            Assert.That(notifies_fired, Is.EqualTo(1));
        }
    }
}
