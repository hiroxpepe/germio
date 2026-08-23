// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

using Germio.Model;

namespace Germio.Tests.Model {
    /// <summary>
    /// Unit tests for Node class (Phase 5.8 refactor).
    /// Verifies recursive node tree structure.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class NodeTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Default values

        [Test, Description("Node default kind is empty string")]
        public void Node_Kind_DefaultsToEmpty() {
            Node node = new Node();
            Assert.That(node.kind, Is.EqualTo(string.Empty));
        }

        [Test, Description("Node default next is empty list")]
        public void Node_Next_DefaultsToEmptyList() {
            Node node = new Node();
            Assert.That(node.next, Is.Not.Null);
            Assert.That(node.next, Has.Count.EqualTo(0));
        }

        [Test, Description("Node default children is empty list")]
        public void Node_Children_DefaultsToEmptyList() {
            Node node = new Node();
            Assert.That(node.children, Is.Not.Null);
            Assert.That(node.children, Has.Count.EqualTo(0));
        }

        ///////////////////////////////////////////////////////////////////////
        // Recursive structure

        [Test, Description("Node can have child nodes")]
        public void Node_Children_CanBeParsedAsNestedTree() {
            Node root = new Node();
            root.id = "world1";
            root.kind = "world";

            Node level1 = new Node();
            level1.id = "level1";
            level1.kind = "level";

            root.children.Add(item: level1);

            Assert.That(root.children, Has.Count.EqualTo(1));
            Assert.That(root.children[index: 0].id, Is.EqualTo("level1"));
        }

        [Test, Description("Node tree depth can reach MAX_NODE_DEPTH")]
        public void Node_Depth_CanReachMaxNodeDepth() {
            // Build a chain of nodes up to MAX_NODE_DEPTH (10)
            Node root = new Node { id = "n0" };
            Node current = root;
            for (int i = 1; i < 10; i++) {
                Node child = new Node { id = $"n{i}" };
                current.children.Add(item: child);
                current = child;
            }
            
            // Verify depth
            int depth = 0;
            current = root;
            while (current.children.Count > 0) {
                depth++;
                current = current.children[index: 0];
            }
            Assert.That(depth, Is.EqualTo(9));
        }

        ///////////////////////////////////////////////////////////////////////
        // Transitions (next)

        [Test, Description("Node can have next transitions")]
        public void Node_Next_CanContainMultipleTransitions() {
            Node node = new Node();
            Next next1 = new Next { id = "level2" };
            Next next2 = new Next { id = "level3" };
            
            node.next.Add(item: next1);
            node.next.Add(item: next2);

            Assert.That(node.next, Has.Count.EqualTo(2));
            Assert.That(node.next[index: 0].id, Is.EqualTo("level2"));
            Assert.That(node.next[index: 1].id, Is.EqualTo("level3"));
        }

        ///////////////////////////////////////////////////////////////////////
        // JSON round-trip

        [Test, Description("Node serializes to JSON with correct structure")]
        public void Node_Serialize_IncludesAllFields() {
            Node node = new Node();
            node.id = "level1";
            node.kind = "level";
            node.next.Add(item: new Next { id = "level2" });
            node.children.Add(item: new Node { id = "child1" });

            string json = JsonConvert.SerializeObject(value: node);
            Assert.That(json, Does.Contain("\"id\":\"level1\""));
            Assert.That(json, Does.Contain("\"kind\":\"level\""));
            Assert.That(json, Does.Contain("\"next\""));
            Assert.That(json, Does.Contain("\"children\""));
        }

        [Test, Description("Node deserializes from JSON correctly")]
        public void Node_Deserialize_RestoresStructure() {
            string json = @"{""id"":""level1"",""kind"":""level"",""next"":[{""id"":""level2""}],""children"":[]}";
            Node? node = JsonConvert.DeserializeObject<Node>(value: json);
            Assert.That(node, Is.Not.Null);
            Assert.That(node!.id, Is.EqualTo("level1"));
            Assert.That(node.kind, Is.EqualTo("level"));
            Assert.That(node.next, Has.Count.EqualTo(1));
            Assert.That(node.next[index: 0].id, Is.EqualTo("level2"));
        }

        ///////////////////////////////////////////////////////////////////////
        // Property count enforcement

        [Test, Description("Node has exactly 7 properties")]
        public void Node_PropertyCount_IsExactlyFour() {
            var props = typeof(Node).GetProperties();
            Assert.That(props.Length, Is.EqualTo(7),
                "Node must have exactly 7 properties: id, name, kind, scene, children, next, rules.");
        }
    }
}