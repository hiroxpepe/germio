// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Tests MermaidParser.Parse() and TryParse() — bidirectional Mermaid conversion (P5-T6, G11).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class MermaidParserTests {
#nullable enable

        [Test]
        public void Parse_SimpleScenario_ReturnsScenarioWithRoot() {
            string mermaid = "flowchart LR\n" +
                "    classDef default fill:#2B303A;\n" +
                "    subgraph w1 [\"World 1\"]\n" +
                "        level_1[\"Level 1\"]\n" +
                "        level_2[\"Level 2\"]\n" +
                "    end\n" +
                "    level_1 --> level_2";

            var scenario = MermaidParser.Parse(mermaid: mermaid);

            Assert.That(scenario.root.children, Has.Count.EqualTo(1));
            Assert.That(scenario.root.children[0].id,   Is.EqualTo("w1"));
            Assert.That(scenario.root.children[0].name, Is.EqualTo("World 1"));
        }

        [Test]
        public void Parse_Levels_AreExtractedFromSubgraph() {
            string mermaid = "flowchart LR\n" +
                "    classDef default fill:#000;\n" +
                "    subgraph world1 [\"Adventure\"]\n" +
                "        start_level([\"Title\"]):::start\n" +
                "        mid_level[\"Mid\"]\n" +
                "        end_level([\"The End\"]):::endNode\n" +
                "    end";

            var scenario = MermaidParser.Parse(mermaid: mermaid);
            var levels   = scenario.root.children[0].children;

            Assert.That(levels, Has.Count.EqualTo(3));
            var ids = levels.Select(l => l.id).ToList();
            Assert.That(ids, Is.EquivalentTo(new[] { "start_level", "mid_level", "end_level" }));
            Assert.That(levels.First(l => l.id == "start_level").name, Is.EqualTo("Title"));
            Assert.That(levels.First(l => l.id == "end_level").name,   Is.EqualTo("The End"));
        }

        [Test]
        public void Parse_UnlabeledEdge_CreatesNextWithEmptyCondition() {
            string mermaid = "flowchart LR\n" +
                "    subgraph w1 [\"W\"]\n" +
                "        a[\"A\"]\n" +
                "        b[\"B\"]\n" +
                "    end\n" +
                "    a --> b";

            var scenario = MermaidParser.Parse(mermaid: mermaid);
            var level_a  = scenario.root.children[0].children.First(l => l.id == "a");

            Assert.That(level_a.next,              Has.Count.EqualTo(1));
            Assert.That(level_a.next[0].id,        Is.EqualTo("b"));
            Assert.That(level_a.next[0].condition, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Parse_LabeledEdge_CreatesNextWithCondition() {
            string mermaid = "flowchart LR\n" +
                "    subgraph w1 [\"W\"]\n" +
                "        a[\"A\"]\n" +
                "        b[\"B\"]\n" +
                "    end\n" +
                "    a -->|\"counters.score >= 100\"| b";

            var scenario = MermaidParser.Parse(mermaid: mermaid);
            var level_a  = scenario.root.children[0].children.First(l => l.id == "a");

            Assert.That(level_a.next[0].condition, Is.EqualTo("counters.score >= 100"));
        }

        [Test]
        public void TryParse_ValidMermaid_ReturnsSuccess() {
            string mermaid = "flowchart LR\n" +
                "    subgraph w1 [\"W\"]\n" +
                "        a[\"A\"]\n" +
                "    end";

            var result = MermaidParser.TryParse(mermaid: mermaid);

            Assert.That(result.Success,  Is.True);
            Assert.That(result.Scenario, Is.Not.Null);
            Assert.That(result.Errors,   Is.Empty);
        }

        [Test]
        public void TryParse_InvalidMermaid_ReturnsFailed() {
            string mermaid = "this is not valid mermaid content";

            var result = MermaidParser.TryParse(mermaid: mermaid);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors,  Is.Not.Empty);
        }

        [Test]
        public void TryParse_EmptyString_ReturnsFailed() {
            var result = MermaidParser.TryParse(mermaid: string.Empty);

            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void ParseResult_HasScenarioAndErrors() {
            var result = new ParseResult {
                Success  = true,
                Scenario = new Scenario(),
                Errors   = new List<ParseError>()
            };

            Assert.That(result.Scenario, Is.Not.Null);
            Assert.That(result.Errors,   Is.Not.Null);
        }

        [Test]
        public void ParseError_HasLineNumberAndMessage() {
            var err = new ParseError { LineNumber = 5, Message = "unexpected token" };

            Assert.That(err.LineNumber, Is.EqualTo(5));
            Assert.That(err.Message,     Is.EqualTo("unexpected token"));
        }

        [Test]
        public void Roundtrip_SimpleScenario_IsSemanticallEquivalent() {
            var original = new Scenario();
            var world    = new Node { id = "world1", name = "Adventure", kind = "world" };
            var lv1      = new Node { id = "level1", name = "Start", kind = "level", scene = "SceneStart" };
            var lv2      = new Node { id = "level2", name = "Level Two", kind = "level", scene = "SceneTwo" };
            lv1.next.Add(new Next { id = "level2", condition = "flags.ready" });
            world.children.Add(lv1);
            world.children.Add(lv2);
            original.root.children.Add(world);

            string mermaid = Grapher.Export(scenario: original);
            var    restored = MermaidParser.Parse(mermaid: mermaid);

            Assert.That(restored.root.children.Count, Is.EqualTo(1));
            Assert.That(restored.root.children[0].id,   Is.EqualTo("world1"));
            Assert.That(restored.root.children[0].name, Is.EqualTo("Adventure"));

            var r_lv1 = restored.root.children[0].children.FirstOrDefault(l => l.id == "level1");
            Assert.That(r_lv1, Is.Not.Null);
            Assert.That(r_lv1!.next.Count,         Is.EqualTo(1));
            Assert.That(r_lv1.next[0].id,          Is.EqualTo("level2"));
            Assert.That(r_lv1.next[0].condition,   Is.EqualTo("flags.ready"));
        }

        [Test]
        public void Parse_MultipleWorlds_ExtractsAll() {
            string mermaid = "flowchart LR\n" +
                "    classDef default fill:#000;\n" +
                "    subgraph w1 [\"World One\"]\n" +
                "        l1[\"Level 1\"]\n" +
                "    end\n" +
                "    subgraph w2 [\"World Two\"]\n" +
                "        l2[\"Level 2\"]\n" +
                "    end\n" +
                "    l1 --> l2";

            var scenario = MermaidParser.Parse(mermaid: mermaid);

            Assert.That(scenario.root.children.Count,   Is.EqualTo(2));
            Assert.That(scenario.root.children[0].name, Is.EqualTo("World One"));
            Assert.That(scenario.root.children[1].name, Is.EqualTo("World Two"));
        }

        [Test]
        public void Parse_ThrowsFormatException_WhenInvalid() {
            Assert.Throws<FormatException>(() =>
                MermaidParser.Parse(mermaid: "invalid content with no flowchart"));
        }
    }
}