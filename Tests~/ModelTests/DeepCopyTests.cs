// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.Collections.Generic;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for taking a copy that holds nothing in common (germio TASK-055).
    ///
    /// A request_deed holds a Command inside it, and that Command holds others
    /// again. Nothing in germio copied a Rule or a Command deeply before, so two
    /// rules built off one another would share what sits within, and a change to
    /// one would reach the other.
    ///
    /// animo's own Data.cs gives every model type a DeepCopy() for this reason.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class DeepCopyTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Command, held flat

        [Test, Description("A copied Command holds the same values")]
        public void Command_DeepCopy_HoldsSameValues() {
            var command = new Command {
                set_flag = new SetFlag { key = "gate", value = true },
                request_notify = "level_clear",
                reset_flags = true
            };

            Command copy = command.DeepCopy();

            Assert.That(copy.set_flag!.key, Is.EqualTo("gate"));
            Assert.That(copy.set_flag.value, Is.True);
            Assert.That(copy.request_notify, Is.EqualTo("level_clear"));
            Assert.That(copy.reset_flags, Is.True);
        }

        [Test, Description("A copied Command holds a set_flag of its own")]
        public void Command_DeepCopy_SetFlagIsItsOwn() {
            var command = new Command { set_flag = new SetFlag { key = "gate", value = true } };

            Command copy = command.DeepCopy();
            copy.set_flag!.key = "other";

            Assert.That(command.set_flag!.key, Is.EqualTo("gate"),
                "Changing the copy must never reach the one it was taken from.");
        }

        [Test, Description("A copied Command holds an update_need list of its own")]
        public void Command_DeepCopy_UpdateNeedListIsItsOwn() {
            var command = new Command {
                update_need = new List<UpdateNeed> {
                    new UpdateNeed { key = "curiosity", delta = -25f }
                }
            };

            Command copy = command.DeepCopy();
            copy.update_need![0].delta = -99f;
            copy.update_need.Add(item: new UpdateNeed { key = "fear", delta = 40f });

            Assert.That(command.update_need, Has.Count.EqualTo(1));
            Assert.That(command.update_need![0].delta, Is.EqualTo(-25f));
        }

        [Test, Description("A Command holding nothing copies without throwing")]
        public void Command_DeepCopy_EmptyCommand_DoesNotThrow() {
            var command = new Command();

            Assert.DoesNotThrow(() => command.DeepCopy());
        }

        ///////////////////////////////////////////////////////////////////////
        // RequestDeed, and the Command held within

        [Test, Description("A copied RequestDeed holds the same values")]
        public void RequestDeed_DeepCopy_HoldsSameValues() {
            var deed = new RequestDeed {
                target = new Target { kind = "Ground", reach = 15.0f, spread = 90.0f },
                condition = "history.count(kind=met, target_id=$target) == 0",
                motion = "walk",
                act = "show",
                until = new Until { near = 2.0f },
                command = new Command { set_flag = new SetFlag { key = "done", value = true } }
            };

            RequestDeed copy = deed.DeepCopy();

            Assert.That(copy.target!.kind, Is.EqualTo("Ground"));
            Assert.That(copy.condition, Does.Contain("history.count"));
            Assert.That(copy.motion, Is.EqualTo("walk"));
            Assert.That(copy.act, Is.EqualTo("show"));
            Assert.That(copy.until!.near, Is.EqualTo(2.0f));
            Assert.That(copy.command.set_flag!.key, Is.EqualTo("done"));
        }

        [Test, Description("A copied RequestDeed holds a target of its own")]
        public void RequestDeed_DeepCopy_TargetIsItsOwn() {
            var deed = new RequestDeed {
                target = new Target { kind = "Ground", reach = 15.0f, spread = 90.0f },
                until = new Until { near = 2.0f },
                command = new Command()
            };

            RequestDeed copy = deed.DeepCopy();
            copy.target!.kind = "Human";

            Assert.That(deed.target!.kind, Is.EqualTo("Ground"));
        }

        [Test, Description("A copied RequestDeed holds an until of its own")]
        public void RequestDeed_DeepCopy_UntilIsItsOwn() {
            var deed = new RequestDeed {
                until = new Until { near = 2.0f },
                command = new Command()
            };

            RequestDeed copy = deed.DeepCopy();
            copy.until!.near = 99.0f;

            Assert.That(deed.until!.near, Is.EqualTo(2.0f));
        }

        [Test, Description("A copied RequestDeed holds the Command within it as its own")]
        public void RequestDeed_DeepCopy_HeldCommandIsItsOwn() {
            var deed = new RequestDeed {
                until = new Until { near = 2.0f },
                command = new Command {
                    record_event = new RecordEvent { kind = "met", target_id = "$target" }
                }
            };

            RequestDeed copy = deed.DeepCopy();
            copy.command.record_event!.target_id = "g_1042";

            Assert.That(deed.command.record_event!.target_id, Is.EqualTo("$target"),
                "This is the whole point: putting an id in place on a copy must "
                + "never reach the rule the copy came from.");
        }

        [Test, Description("A RequestDeed with no target copies without throwing")]
        public void RequestDeed_DeepCopy_NoTarget_DoesNotThrow() {
            var deed = new RequestDeed {
                motion = "idle",
                until = new Until { elapsed = 4.0f },
                command = new Command()
            };

            RequestDeed copy = deed.DeepCopy();

            Assert.That(copy.target, Is.Null);
        }

        ///////////////////////////////////////////////////////////////////////
        // Rule, all the way down

        [Test, Description("A copied Rule holds the same values")]
        public void Rule_DeepCopy_HoldsSameValues() {
            var rule = new Rule {
                id = "rule_explore", trigger = "sig_behavior_explore",
                condition = "flags.ready == true", once = false, actor = "npc_01",
                command = new Command { set_flag = new SetFlag { key = "gate", value = true } }
            };

            Rule copy = rule.DeepCopy();

            Assert.That(copy.id, Is.EqualTo("rule_explore"));
            Assert.That(copy.trigger, Is.EqualTo("sig_behavior_explore"));
            Assert.That(copy.condition, Is.EqualTo("flags.ready == true"));
            Assert.That(copy.once, Is.False);
            Assert.That(copy.actor, Is.EqualTo("npc_01"));
            Assert.That(copy.command.set_flag!.key, Is.EqualTo("gate"));
        }

        [Test, Description("A copied Rule reaches all the way down to a deed's own held Command")]
        public void Rule_DeepCopy_ReachesAllTheWayDown() {
            var rule = new Rule {
                id = "rule_explore", trigger = "sig_behavior_explore", actor = "npc_01",
                command = new Command {
                    request_deed = new RequestDeed {
                        target = new Target { kind = "Ground", reach = 15.0f, spread = 90.0f },
                        motion = "walk",
                        until = new Until { meets = "$target" },
                        command = new Command {
                            update_need = new List<UpdateNeed> {
                                new UpdateNeed { key = "curiosity", delta = -25f }
                            },
                            record_event = new RecordEvent { kind = "met", target_id = "$target" }
                        }
                    }
                }
            };

            Rule copy = rule.DeepCopy();
            copy.command.request_deed!.command.record_event!.target_id = "g_1042";
            copy.command.request_deed.command.update_need![0].delta = -99f;
            copy.command.request_deed.until!.meets = "g_1042";
            copy.command.request_deed.target!.reach = 99.0f;

            var held = rule.command.request_deed!;
            Assert.That(held.command.record_event!.target_id, Is.EqualTo("$target"));
            Assert.That(held.command.update_need![0].delta, Is.EqualTo(-25f));
            Assert.That(held.until!.meets, Is.EqualTo("$target"));
            Assert.That(held.target!.reach, Is.EqualTo(15.0f));
        }
    }
}
