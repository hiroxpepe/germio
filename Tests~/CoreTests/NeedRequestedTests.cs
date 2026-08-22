// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for firing a Need out of the Store (germio TASK-021, TASK-040).
    /// germio knows nothing of animo, so the Executor calls no engine: it fires an
    /// event, the same road request_notify already takes, and modio is what hears
    /// it. See modio's own docs/modio_spec.md §7.11.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class NeedRequestedTests {
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

        ///////////////////////////////////////////////////////////////////////
        // One entry

        [Test, Description("One update_need entry fires the event once")]
        public void Execute_OneUpdateNeed_FiresOnce() {
            var store = buildStore();
            int fired = 0;
            store.NeedRequested += (key, delta) => { fired++; };

            Executor.Execute(command: new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "curiosity", delta = -25f }
                }
            }, store: store);

            Assert.That(fired, Is.EqualTo(1));
        }

        [Test, Description("The event carries the key and the delta it was given")]
        public void Execute_OneUpdateNeed_CarriesKeyAndDelta() {
            var store = buildStore();
            string? got_key = null;
            float got_delta = 0f;
            store.NeedRequested += (key, delta) => { got_key = key; got_delta = delta; };

            Executor.Execute(command: new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "curiosity", delta = -25f }
                }
            }, store: store);

            Assert.That(got_key, Is.EqualTo("curiosity"));
            Assert.That(got_delta, Is.EqualTo(-25f));
        }

        ///////////////////////////////////////////////////////////////////////
        // Two entries — the whole point of a list

        [Test, Description("Two update_need entries fire the event twice")]
        public void Execute_TwoUpdateNeeds_FiresTwice() {
            var store = buildStore();
            int fired = 0;
            store.NeedRequested += (key, delta) => { fired++; };

            Executor.Execute(command: new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "loneliness", delta = -30f },
                    new UpdateNeed { key = "separation", delta = -40f }
                }
            }, store: store);

            Assert.That(fired, Is.EqualTo(2));
        }

        [Test, Description("Each of two entries carries its own key and delta, in order")]
        public void Execute_TwoUpdateNeeds_EachCarriesItsOwn() {
            var store = buildStore();
            var keys = new List<string>();
            var deltas = new List<float>();
            store.NeedRequested += (key, delta) => { keys.Add(item: key); deltas.Add(item: delta); };

            Executor.Execute(command: new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "loneliness", delta = -30f },
                    new UpdateNeed { key = "separation", delta = -40f }
                }
            }, store: store);

            Assert.That(keys, Is.EqualTo(new List<string> { "loneliness", "separation" }));
            Assert.That(deltas, Is.EqualTo(new List<float> { -30f, -40f }));
        }

        ///////////////////////////////////////////////////////////////////////
        // Nothing to fire

        [Test, Description("A command with no update_need fires nothing")]
        public void Execute_NoUpdateNeed_FiresNothing() {
            var store = buildStore();
            int fired = 0;
            store.NeedRequested += (key, delta) => { fired++; };

            Executor.Execute(command: new Command {
                set_flag = new SetFlag { key = "gate_open", value = true }
            }, store: store);

            Assert.That(fired, Is.EqualTo(0));
        }

        [Test, Description("An empty update_need list fires nothing")]
        public void Execute_EmptyUpdateNeedList_FiresNothing() {
            var store = buildStore();
            int fired = 0;
            store.NeedRequested += (key, delta) => { fired++; };

            Executor.Execute(command: new Command {
                update_need = new List<UpdateNeed>()
            }, store: store);

            Assert.That(fired, Is.EqualTo(0));
        }

        [Test, Description("Firing with no one listening throws nothing")]
        public void Execute_NoListener_DoesNotThrow() {
            var store = buildStore();

            Assert.DoesNotThrow(() => Executor.Execute(command: new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "curiosity", delta = -25f }
                }
            }, store: store));
        }
    }
}
