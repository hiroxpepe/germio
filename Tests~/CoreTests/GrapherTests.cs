// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for <see cref="Grapher"/>.
    /// Verifies Mermaid flowchart output: header, classDef styles, Node tree rendering
    /// (subgraphs for internal nodes, regular nodes for leaves), and transition edges.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class GrapherTests {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Helper builders

        static Scenario buildScenario(Node? root = null) {
            var scenario = new Scenario();
            scenario.root = root ?? new Node { id = "root", name = "Root" };
            return scenario;
        }

        static Node buildNode(string id, string name, bool is_leaf = false,
            List<Node>? children = null, List<Next>? next = null) {
            return new Node {
                id = id,
                name = name,
                children = is_leaf ? new List<Node>() : (children ?? new List<Node>()),
                next = next ?? new List<Next>()
            };
        }

        static Next buildNext(string target_id, string condition = "") =>
            new Next { id = target_id, condition = condition };

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Header and styles

        [Test, Description("Output always starts with 'graph TD'")]
        public void Export_AlwaysStartsWithGraphTD() {
            var scenario = buildScenario();
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.StartWith("graph TD"));
        }

        [Test, Description("Output always contains classDef default style")]
        public void Export_AlwaysHasClassDefDefault() {
            var output = Grapher.Export(scenario: buildScenario());
            Assert.That(output, Does.Contain("classDef default fill:#2B303A,stroke:#7D8597,color:#FFFFFF;"));
        }

        [Test, Description("Output always contains classDef start style")]
        public void Export_AlwaysHasClassDefStart() {
            var output = Grapher.Export(scenario: buildScenario());
            Assert.That(output, Does.Contain("classDef start fill:#1E88E5,stroke:#005CB2,color:#FFFFFF;"));
        }

        [Test, Description("Output always contains classDef endNode style")]
        public void Export_AlwaysHasClassDefEndNode() {
            var output = Grapher.Export(scenario: buildScenario());
            Assert.That(output, Does.Contain("classDef endNode fill:#D81159,stroke:#8F0031,color:#FFFFFF;"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Node rendering — internal vs leaf

        [Test, Description("Grapher_ExportsNodeAsSubgraph")]
        public void Grapher_ExportsNodeAsSubgraph() {
            var child = buildNode("child1", "Child", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { child });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("subgraph root"));
            Assert.That(output, Does.Contain("\"Root\""));
            Assert.That(output, Does.Contain("end"));
        }

        [Test, Description("Grapher_ExportsLeafAsNode")]
        public void Grapher_ExportsLeafAsNode() {
            var child = buildNode("child1", "Child Leaf", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { child });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("child1[\"Child Leaf\"]"));
            Assert.That(output, Does.Not.Contain("subgraph child1"));
        }

        [Test, Description("Leaf node with 'Title' in name uses start styling")]
        public void Export_LeafWithTitle_UsesStartClass() {
            var leaf = buildNode("title", "Title Screen", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("title([\"Title Screen\"]):::start"));
        }

        [Test, Description("Leaf node with 'Start' in name uses start styling")]
        public void Export_LeafWithStart_UsesStartClass() {
            var leaf = buildNode("start_screen", "Start Game", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("start_screen([\"Start Game\"]):::start"));
        }

        [Test, Description("Leaf node with 'End' in name uses endNode styling")]
        public void Export_LeafWithEnd_UsesEndNodeClass() {
            var leaf = buildNode("ending", "Game End", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("ending([\"Game End\"]):::endNode"));
        }

        [Test, Description("Leaf node with 'Over' in name uses endNode styling")]
        public void Export_LeafWithOver_UsesEndNodeClass() {
            var leaf = buildNode("gameover", "Game Over", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("gameover([\"Game Over\"]):::endNode"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Transitions (Next entries)

        [Test, Description("Grapher_ExportsNextAsArrow")]
        public void Grapher_ExportsNextAsArrow() {
            var leaf = buildNode("node1", "Node 1", is_leaf: true, next: new List<Next> { buildNext("node2") });
            var leaf2 = buildNode("node2", "Node 2", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf, leaf2 });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("node1 --> node2"));
        }

        [Test, Description("Grapher_ExportsCondition")]
        public void Grapher_ExportsCondition() {
            var leaf = buildNode("node1", "Node 1", is_leaf: true, 
                next: new List<Next> { buildNext("node2", "counters.score >= 100") });
            var leaf2 = buildNode("node2", "Node 2", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf, leaf2 });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("node1 -->|\"counters.score >= 100\"| node2"));
        }

        [Test, Description("Multiple transitions from one node all appear")]
        public void Export_MultipleTransitions_AllPresent() {
            var leaf = buildNode("n1", "N1", is_leaf: true, next: new List<Next> {
                buildNext("n2", "flags.a"),
                buildNext("n3", "flags.b")
            });
            var leaf2 = buildNode("n2", "N2", is_leaf: true);
            var leaf3 = buildNode("n3", "N3", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf, leaf2, leaf3 });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("n1 -->|\"flags.a\"| n2"));
            Assert.That(output, Does.Contain("n1 -->|\"flags.b\"| n3"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Sanitization

        [Test, Description("Node id with spaces is sanitized to underscores")]
        public void Export_NodeIdWithSpaces_SanitizedToUnderscores() {
            var leaf = buildNode("node 1", "Node", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("node_1[\"Node\"]"));
        }

        [Test, Description("Node id with hyphens is sanitized to underscores")]
        public void Export_NodeIdWithHyphens_SanitizedToUnderscores() {
            var leaf = buildNode("node-one", "Node", is_leaf: true);
            var root = buildNode("root", "Root", children: new List<Node> { leaf });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("node_one[\"Node\"]"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Complex hierarchies

        [Test, Description("Nested subgraphs (3-level hierarchy) all render correctly")]
        public void Export_NestedSubgraphs_AllRender() {
            var leaf = buildNode("leaf", "Leaf", is_leaf: true);
            var mid = buildNode("mid", "Middle", children: new List<Node> { leaf });
            var root = buildNode("root", "Root", children: new List<Node> { mid });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("subgraph root"));
            Assert.That(output, Does.Contain("subgraph mid"));
            Assert.That(output, Does.Contain("leaf[\"Leaf\"]"));
            Assert.That(output.Split(new[] { "end" }, System.StringSplitOptions.None).Length - 1, Is.GreaterThanOrEqualTo(2));
        }

        [Test, Description("Multiple sibling subgraphs within parent")]
        public void Export_MultipleSiblingSubgraphs_AllRender() {
            var leaf1 = buildNode("leaf1", "L1", is_leaf: true);
            var leaf2 = buildNode("leaf2", "L2", is_leaf: true);
            var sub1 = buildNode("sub1", "Sub1", children: new List<Node> { leaf1 });
            var sub2 = buildNode("sub2", "Sub2", children: new List<Node> { leaf2 });
            var root = buildNode("root", "Root", children: new List<Node> { sub1, sub2 });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("subgraph sub1"));
            Assert.That(output, Does.Contain("subgraph sub2"));
            Assert.That(output, Does.Contain("leaf1[\"L1\"]"));
            Assert.That(output, Does.Contain("leaf2[\"L2\"]"));
        }

        [Test, Description("Transitions crossing hierarchies (from leaf to sibling's leaf)")]
        public void Export_CrossHierarchyTransition_Renders() {
            var leaf1 = buildNode("leaf1", "L1", is_leaf: true, 
                next: new List<Next> { buildNext("leaf2") });
            var leaf2 = buildNode("leaf2", "L2", is_leaf: true);
            var sub1 = buildNode("sub1", "Sub1", children: new List<Node> { leaf1 });
            var sub2 = buildNode("sub2", "Sub2", children: new List<Node> { leaf2 });
            var root = buildNode("root", "Root", children: new List<Node> { sub1, sub2 });
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("leaf1 --> leaf2"));
        }

        [Test, Description("Empty scenario (null root) handles gracefully")]
        public void Export_NullRoot_HandlesGracefully() {
            var scenario = new Scenario { root = null };
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("graph TD"));
            Assert.That(output, Does.Contain("classDef default"));
        }

        [Test, Description("Root node with no children produces single node with no subgraph")]
        public void Export_RootOnlyNoChildren_SingleNode() {
            var root = buildNode("root", "Root", is_leaf: true);
            var scenario = buildScenario(root: root);
            var output = Grapher.Export(scenario: scenario);
            Assert.That(output, Does.Contain("root[\"Root\"]"));
            Assert.That(output, Does.Not.Contain("subgraph root"));
        }
    }
}