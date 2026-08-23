// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;

namespace Germio.Tests.Core {
    /// <summary>
    /// Unit tests for ScenarioNavigator (Phase 5.8 v2 fix6 hotfix9).
    /// Verifies the Unity Scene → node id reverse lookup that drives the
    /// "Unity Scene is the single source of truth for current_node" principle.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class ScenarioNavigatorTests {
#nullable enable

        // -----------------------------------------------------------------------------------------
        // Helpers

        /// <summary>
        /// Builds the canonical Stemic 3-level scenario tree (matches StreamingAssets/germio.json
        /// structure). Used to verify both flat and deep lookups.
        /// </summary>
        static Node BuildStemicRoot() {
            return new Node {
                id = "main", name = "Main World", kind = "world", scene = "",
                children = new List<Node> {
                    new Node { id = "title",  name = "Title",  kind = "title",  scene = "Title"  },
                    new Node { id = "select", name = "Select", kind = "select", scene = "Select" },
                    new Node {
                        id = "levels", name = "Levels", kind = "world", scene = "",
                        children = new List<Node> {
                            new Node { id = "level_1", name = "Level 1", kind = "level", scene = "Level 1" },
                            new Node { id = "level_2", name = "Level 2", kind = "level", scene = "Level 2" },
                            new Node { id = "level_3", name = "Level 3", kind = "level", scene = "Level 3" },
                        }
                    },
                    new Node { id = "ending", name = "Ending", kind = "ending", scene = "Ending" },
                }
            };
        }

        // -----------------------------------------------------------------------------------------
        // Direct (flat) lookups

        [Test, Description("FindNodeIDBySceneName resolves a top-level scene name to its node id")]
        public void ScenarioNavigator_FindByScene_ReturnsTopLevelNodeId() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "Title");
            Assert.That(id, Is.EqualTo("title"));
        }

        [Test, Description("FindNodeIDBySceneName resolves the Select scene")]
        public void ScenarioNavigator_FindByScene_ResolvesSelect() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "Select");
            Assert.That(id, Is.EqualTo("select"));
        }

        [Test, Description("FindNodeIDBySceneName resolves the Ending scene")]
        public void ScenarioNavigator_FindByScene_ResolvesEnding() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "Ending");
            Assert.That(id, Is.EqualTo("ending"));
        }

        // -----------------------------------------------------------------------------------------
        // Deep lookups (under 'levels' parent)

        [Test, Description("FindNodeIDBySceneName resolves a deep scene name (with whitespace)")]
        public void ScenarioNavigator_FindByScene_ResolvesDeepLevel1() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "Level 1");
            Assert.That(id, Is.EqualTo("level_1"));
        }

        [Test, Description("FindNodeIDBySceneName resolves Level 2")]
        public void ScenarioNavigator_FindByScene_ResolvesLevel2() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "Level 2");
            Assert.That(id, Is.EqualTo("level_2"));
        }

        [Test, Description("FindNodeIDBySceneName resolves Level 3")]
        public void ScenarioNavigator_FindByScene_ResolvesLevel3() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "Level 3");
            Assert.That(id, Is.EqualTo("level_3"));
        }

        // -----------------------------------------------------------------------------------------
        // Edge cases

        [Test, Description("FindNodeIDBySceneName returns null for an unknown scene name")]
        public void ScenarioNavigator_FindByScene_UnknownReturnsNull() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "NotARealScene");
            Assert.That(id, Is.Null);
        }

        [Test, Description("FindNodeIDBySceneName returns null when root is null")]
        public void ScenarioNavigator_FindByScene_NullRootReturnsNull() {
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: null, scene_name: "Title");
            Assert.That(id, Is.Null);
        }

        [Test, Description("FindNodeIDBySceneName returns null when scene_name is null")]
        public void ScenarioNavigator_FindByScene_NullSceneReturnsNull() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: null);
            Assert.That(id, Is.Null);
        }

        [Test, Description("FindNodeIDBySceneName returns null when scene_name is empty")]
        public void ScenarioNavigator_FindByScene_EmptySceneReturnsNull() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "");
            Assert.That(id, Is.Null);
        }

        [Test, Description("FindNodeIDBySceneName is case-sensitive — 'level 1' (lowercase) does NOT match 'Level 1'")]
        public void ScenarioNavigator_FindByScene_IsCaseSensitive() {
            Node root = BuildStemicRoot();
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "level 1");
            Assert.That(id, Is.Null);
        }

        [Test, Description("FindNodeIDBySceneName matches against root's own scene field")]
        public void ScenarioNavigator_FindByScene_MatchesRootItself() {
            Node root = new Node {
                id = "only", name = "Only", kind = "level", scene = "OnlyScene"
            };
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "OnlyScene");
            Assert.That(id, Is.EqualTo("only"));
        }

        [Test, Description("FindNodeIDBySceneName works when children list is null")]
        public void ScenarioNavigator_FindByScene_NullChildrenIsSafe() {
            Node root = new Node {
                id = "root", name = "Root", kind = "world", scene = ""
                // children left null
            };
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "Title");
            Assert.That(id, Is.Null);
        }

        // -----------------------------------------------------------------------------------------
        // Determinism — first hit wins (depth-first pre-order)

        [Test, Description("FindNodeIDBySceneName returns the first match in depth-first pre-order when scene names collide")]
        public void ScenarioNavigator_FindByScene_FirstHitWins() {
            // Deliberately authored two nodes with the same scene name.
            // ScenarioNavigator should return the one encountered first
            // in depth-first pre-order.
            Node root = new Node {
                id = "root", name = "Root", kind = "world", scene = "",
                children = new List<Node> {
                    new Node { id = "first",  name = "First",  kind = "level", scene = "Duplicate" },
                    new Node { id = "second", name = "Second", kind = "level", scene = "Duplicate" },
                }
            };
            string? id = ScenarioNavigator.FindNodeIDBySceneName(root: root, scene_name: "Duplicate");
            Assert.That(id, Is.EqualTo("first"));
        }
    }
}