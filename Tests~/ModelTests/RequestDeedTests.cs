// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for RequestDeed (germio TASK-024 to TASK-026, TASK-037).
    /// Work that takes time, and may fail part way. Sits beside
    /// request_transition and request_notify, all three asking for what does not
    /// finish on the spot. See modio's own docs/modio_spec.md §7.3.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class RequestDeedTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Target

        [Test, Description("Target holds a kind, a reach and a spread")]
        public void Target_Fields_SetCorrectly() {
            Target target = new Target();
            target.kind = "Ground";
            target.reach = 15.0f;
            target.spread = 90.0f;

            Assert.That(target.kind, Is.EqualTo("Ground"));
            Assert.That(target.reach, Is.EqualTo(15.0f));
            Assert.That(target.spread, Is.EqualTo(90.0f));
        }

        [Test, Description("Target deserializes from JSON correctly")]
        public void Target_Deserialize_RestoresFields() {
            string json = @"{""kind"":""Human"",""reach"":30.0,""spread"":120.0}";

            Target? target = JsonConvert.DeserializeObject<Target>(value: json);

            Assert.That(target, Is.Not.Null);
            Assert.That(target!.kind, Is.EqualTo("Human"));
            Assert.That(target.reach, Is.EqualTo(30.0f));
            Assert.That(target.spread, Is.EqualTo(120.0f));
        }

        [Test, Description("Target holds the four questions a deed puts to its own past")]
        public void Target_Questions_SetCorrectly() {
            Target target = new Target();
            target.not_in_memory = "met";
            target.not_given_to = "gave";
            target.keep_from = "edge";
            target.new_again_after = 60.0f;

            Assert.That(target.not_in_memory, Is.EqualTo("met"));
            Assert.That(target.not_given_to, Is.EqualTo("gave"));
            Assert.That(target.keep_from, Is.EqualTo("edge"));
            Assert.That(target.new_again_after, Is.EqualTo(60.0f));
        }

        [Test, Description("Target defaults to asking nothing of the past")]
        public void Target_Questions_DefaultToAskingNothing() {
            Target target = new Target();

            Assert.That(target.not_in_memory, Is.EqualTo(string.Empty));
            Assert.That(target.not_given_to, Is.EqualTo(string.Empty));
            Assert.That(target.keep_from, Is.EqualTo(string.Empty));
            Assert.That(target.new_again_after, Is.LessThan(0f),
                "Below zero says never new again, which no true count of seconds can say.");
        }

        [Test, Description("Target reads the four questions from JSON")]
        public void Target_Questions_ReadFromJson() {
            string json = "{KIND,QA,QB,QC}"
                .Replace("KIND", "\"kind\":\"Ground\"")
                .Replace("QA", "\"not_in_memory\":\"met\"")
                .Replace("QB", "\"keep_from\":\"edge\"")
                .Replace("QC", "\"new_again_after\":60.0");

            Target? target = JsonConvert.DeserializeObject<Target>(value: json);

            Assert.That(target, Is.Not.Null);
            Assert.That(target!.not_in_memory, Is.EqualTo("met"));
            Assert.That(target.keep_from, Is.EqualTo("edge"));
            Assert.That(target.new_again_after, Is.EqualTo(60.0f));
        }

        ///////////////////////////////////////////////////////////////////////
        // Until

        [Test, Description("Until holds one key alone, and the rest stay empty")]
        public void Until_Near_SetCorrectly() {
            Until until = new Until();
            until.near = 2.0f;

            Assert.That(until.near, Is.EqualTo(2.0f));
            Assert.That(until.meets, Is.Null);
            Assert.That(until.elapsed, Is.Null);
            Assert.That(until.@while, Is.Null);
        }

        [Test, Description("Until deserializes meets from JSON correctly")]
        public void Until_Deserialize_Meets() {
            string json = @"{""meets"":""$target""}";

            Until? until = JsonConvert.DeserializeObject<Until>(value: json);

            Assert.That(until, Is.Not.Null);
            Assert.That(until!.meets, Is.EqualTo("$target"));
            Assert.That(until.near, Is.Null);
        }

        [Test, Description("Until deserializes elapsed from JSON correctly")]
        public void Until_Deserialize_Elapsed() {
            string json = @"{""elapsed"":4.0}";

            Until? until = JsonConvert.DeserializeObject<Until>(value: json);

            Assert.That(until, Is.Not.Null);
            Assert.That(until!.elapsed, Is.EqualTo(4.0f));
        }

        [Test, Description("Until deserializes while from JSON correctly")]
        public void Until_Deserialize_While() {
            string json = @"{""while"":""other_near""}";

            Until? until = JsonConvert.DeserializeObject<Until>(value: json);

            Assert.That(until, Is.Not.Null);
            Assert.That(until!.@while, Is.EqualTo("other_near"));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-024: all five parts

        [Test, Description("RequestDeed reads all five of its parts from JSON")]
        public void RequestDeed_Deserialize_RestoresAllParts() {
            string json = @"{
                ""target"": { ""kind"": ""Ground"", ""reach"": 15.0, ""spread"": 90.0 },
                ""condition"": ""history.time_since(kind=met, target_id=$target) > 60"",
                ""motion"": ""walk"",
                ""until"": { ""meets"": ""$target"" },
                ""command"": { ""update_need"": [ { ""key"": ""curiosity"", ""delta"": -25.0 } ] }
            }";

            RequestDeed? deed = JsonConvert.DeserializeObject<RequestDeed>(value: json);

            Assert.That(deed, Is.Not.Null);
            Assert.That(deed!.target, Is.Not.Null);
            Assert.That(deed.target!.kind, Is.EqualTo("Ground"));
            Assert.That(deed.condition, Does.Contain("history.time_since"));
            Assert.That(deed.motion, Is.EqualTo("walk"));
            Assert.That(deed.until, Is.Not.Null);
            Assert.That(deed.until!.meets, Is.EqualTo("$target"));
            Assert.That(deed.command, Is.Not.Null);
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-025: the Command held inside

        [Test, Description("RequestDeed reads a held Command with two commands at once")]
        public void RequestDeed_HeldCommand_ReadsBoth() {
            string json = @"{
                ""motion"": ""walk"",
                ""until"": { ""near"": 1.5 },
                ""command"": {
                    ""update_need"": [ { ""key"": ""togetherness"", ""delta"": -30.0 } ],
                    ""record_event"": { ""kind"": ""gave"", ""target_id"": ""$target"" }
                }
            }";

            RequestDeed? deed = JsonConvert.DeserializeObject<RequestDeed>(value: json);

            Assert.That(deed, Is.Not.Null);
            Assert.That(deed!.command.update_need, Has.Count.EqualTo(1));
            Assert.That(deed.command.record_event, Is.Not.Null);
            Assert.That(deed.command.record_event!.kind, Is.EqualTo("gave"));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-026: a deed that seeks nothing

        [Test, Description("RequestDeed stands with no target at all")]
        public void RequestDeed_NoTarget_ReadsWithTargetNull() {
            string json = @"{
                ""motion"": ""idle"",
                ""until"": { ""elapsed"": 4.0 },
                ""command"": { ""update_need"": [ { ""key"": ""fatigue"", ""delta"": -40.0 } ] }
            }";

            RequestDeed? deed = JsonConvert.DeserializeObject<RequestDeed>(value: json);

            Assert.That(deed, Is.Not.Null);
            Assert.That(deed!.target, Is.Null);
            Assert.That(deed.motion, Is.EqualTo("idle"));
        }

        [Test, Description("RequestDeed defaults to an empty condition")]
        public void RequestDeed_NoCondition_DefaultsToEmpty() {
            string json = @"{ ""motion"": ""idle"", ""until"": { ""elapsed"": 4.0 } }";

            RequestDeed? deed = JsonConvert.DeserializeObject<RequestDeed>(value: json);

            Assert.That(deed, Is.Not.Null);
            Assert.That(deed!.condition, Is.EqualTo(string.Empty));
        }

        ///////////////////////////////////////////////////////////////////////
        // TASK-037: act, which most deeds do without

        [Test, Description("RequestDeed reads an act when one is given")]
        public void RequestDeed_Act_ReadsWhenGiven() {
            string json = @"{
                ""motion"": ""walk"",
                ""act"": ""hand_over"",
                ""until"": { ""near"": 1.5 },
                ""command"": { }
            }";

            RequestDeed? deed = JsonConvert.DeserializeObject<RequestDeed>(value: json);

            Assert.That(deed, Is.Not.Null);
            Assert.That(deed!.act, Is.EqualTo("hand_over"));
        }

        [Test, Description("RequestDeed leaves act empty when none is given")]
        public void RequestDeed_NoAct_DefaultsToEmpty() {
            string json = @"{ ""motion"": ""walk"", ""until"": { ""near"": 2.0 } }";

            RequestDeed? deed = JsonConvert.DeserializeObject<RequestDeed>(value: json);

            Assert.That(deed, Is.Not.Null);
            Assert.That(deed!.act, Is.EqualTo(string.Empty));
        }

        ///////////////////////////////////////////////////////////////////////
        // On a Command

        [Test, Description("Command holds a request_deed")]
        public void Command_RequestDeed_ReadsFromJson() {
            string json = @"{
                ""request_deed"": {
                    ""motion"": ""walk"",
                    ""until"": { ""near"": 2.0 },
                    ""command"": { }
                }
            }";

            Command? command = JsonConvert.DeserializeObject<Command>(value: json);

            Assert.That(command, Is.Not.Null);
            Assert.That(command!.request_deed, Is.Not.Null);
            Assert.That(command.request_deed!.motion, Is.EqualTo("walk"));
        }

        [Test, Description("Command defaults to no request_deed at all")]
        public void Command_RequestDeed_DefaultsToNull() {
            Command command = new Command();

            Assert.That(command.request_deed, Is.Null);
        }
    }
}
