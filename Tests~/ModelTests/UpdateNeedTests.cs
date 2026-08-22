// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for UpdateNeed (germio TASK-020).
    /// The one way anything reaches animo. Always a list, even holding one:
    /// see modio's own docs/modio_spec.md §7.2.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class UpdateNeedTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Fields

        [Test, Description("UpdateNeed holds a key and a delta")]
        public void UpdateNeed_Fields_SetCorrectly() {
            UpdateNeed need = new UpdateNeed();
            need.key = "loneliness";
            need.delta = -30f;

            Assert.That(need.key, Is.EqualTo("loneliness"));
            Assert.That(need.delta, Is.EqualTo(-30f));
        }

        [Test, Description("UpdateNeed defaults to an empty key and a delta of zero")]
        public void UpdateNeed_Defaults_AreEmptyAndZero() {
            UpdateNeed need = new UpdateNeed();

            Assert.That(need.key, Is.EqualTo(string.Empty));
            Assert.That(need.delta, Is.EqualTo(0f));
        }

        ///////////////////////////////////////////////////////////////////////
        // JSON, on its own

        [Test, Description("UpdateNeed serializes to JSON correctly")]
        public void UpdateNeed_Serialize_IncludesAllFields() {
            UpdateNeed need = new UpdateNeed();
            need.key = "curiosity";
            need.delta = -25f;

            string json = JsonConvert.SerializeObject(value: need);

            Assert.That(json, Does.Contain("\"key\":\"curiosity\""));
            Assert.That(json, Does.Contain("\"delta\":-25"));
        }

        [Test, Description("UpdateNeed deserializes from JSON correctly")]
        public void UpdateNeed_Deserialize_RestoresFields() {
            string json = @"{""key"":""curiosity"",""delta"":-25.0}";

            UpdateNeed? need = JsonConvert.DeserializeObject<UpdateNeed>(value: json);

            Assert.That(need, Is.Not.Null);
            Assert.That(need!.key, Is.EqualTo("curiosity"));
            Assert.That(need.delta, Is.EqualTo(-25f));
        }

        ///////////////////////////////////////////////////////////////////////
        // On a Command — always a list

        [Test, Description("Command holds update_need as a list, not one alone")]
        public void Command_UpdateNeed_IsAList() {
            Command command = new Command();
            command.update_need = new List<UpdateNeed> {
                new UpdateNeed { key = "loneliness", delta = -30f },
                new UpdateNeed { key = "separation", delta = -40f }
            };

            Assert.That(command.update_need, Has.Count.EqualTo(2));
            Assert.That(command.update_need![0].key, Is.EqualTo("loneliness"));
            Assert.That(command.update_need![1].key, Is.EqualTo("separation"));
        }

        [Test, Description("Command defaults to no update_need at all")]
        public void Command_UpdateNeed_DefaultsToNull() {
            Command command = new Command();

            Assert.That(command.update_need, Is.Null);
        }

        [Test, Description("Command deserializes a two-entry update_need as two")]
        public void Command_Deserialize_TwoEntries_GivesTwo() {
            string json = @"{""update_need"":[
                {""key"":""loneliness"",""delta"":-30.0},
                {""key"":""separation"",""delta"":-40.0}]}";

            Command? command = JsonConvert.DeserializeObject<Command>(value: json);

            Assert.That(command, Is.Not.Null);
            Assert.That(command!.update_need, Has.Count.EqualTo(2));
            Assert.That(command.update_need![0].delta, Is.EqualTo(-30f));
            Assert.That(command.update_need![1].delta, Is.EqualTo(-40f));
        }

        [Test, Description("Command deserializes a one-entry update_need as a list of one")]
        public void Command_Deserialize_OneEntry_GivesListOfOne() {
            string json = @"{""update_need"":[{""key"":""curiosity"",""delta"":-25.0}]}";

            Command? command = JsonConvert.DeserializeObject<Command>(value: json);

            Assert.That(command, Is.Not.Null);
            Assert.That(command!.update_need, Has.Count.EqualTo(1));
            Assert.That(command.update_need![0].key, Is.EqualTo("curiosity"));
        }
    }
}
