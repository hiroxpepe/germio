// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

using Germio.Model;
using Germio.Core;
using Germio.Systems;

namespace Germio.Tests.Systems {
    /// <summary>
    /// Unit tests for SceneLoader.
    /// Uses an injected Action&lt;string&gt; to intercept scene load calls — no Unity runtime needed.
    /// Verifies: scene-name lookup, current_scene update, dirty tracking, dispose, and edge cases.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class SceneLoaderTests {
#nullable enable
        Store _store = null!;
        SceneLoader _loader = null!;
        string? _loaded;

        // -----------------------------------------------------------------------------------------
        // Helpers

        static Scenario BuildRoot() {
            var root = new Scenario();
            root.initial_state.current_node = "level_01";
            root.root = new Node {
                id = "root",
                name = "Root",
                kind = "root",
                scene = "",
                children = new List<Node> {
                    new Node { id = "level_01", name = "Level 1", kind = "level", scene = "Scene_Level1" },
                    new Node { id = "level_02", name = "Level 2", kind = "level", scene = "Scene_Level2" },
                    new Node { id = "level_03", name = "Level 3", kind = "level", scene = "Scene_Level3" },
                    // Level whose id differs from scene name
                    new Node { id = "ending", name = "Ending", kind = "level", scene = "Scene_Ending" }
                }
            };
            return root;
        }

        [SetUp]
        public void SetUp() {
            _loaded  = null;
            _store   = new Store(BuildRoot());
            _loader  = new SceneLoader(store: _store, load_scene: name => _loaded = name);
        }

        [TearDown]
        public void TearDown() {
            _loader.Dispose();
        }

        // -----------------------------------------------------------------------------------------
        // Basic load behavior

        [Test]
        public void TransitionRequested_CallsLoadScene_WithSceneName() {
            _store.RequestTransition(target_id: "level_02");
            Assert.That(_loaded, Is.EqualTo("Scene_Level2"));
        }

        [Test]
        public void TransitionRequested_UsesSceneName_NotLevelId() {
            // node.id = "ending", node.scene = "Scene_Ending" — must use scene name
            _store.RequestTransition(target_id: "ending");
            Assert.That(_loaded, Is.EqualTo("Scene_Ending"),
                "SceneLoader must use node.scene, not node.id");
        }

        [Test]
        public void TransitionRequested_UpdatesCurrentScene() {
            _store.RequestTransition(target_id: "level_02");
            Assert.That(_store.Scenario.initial_state.current_node, Is.EqualTo("level_02"));
        }

        [Test]
        public void TransitionRequested_MarksDirty() {
            _store.RequestTransition(target_id: "level_02");
            Assert.That(_store.IsDirty, Is.True);
        }

        [Test]
        public void TransitionRequested_UnknownLevelId_DoesNotCallLoadScene() {
            _store.RequestTransition(target_id: "nonexistent_level");
            Assert.That(_loaded, Is.Null,
                "Unknown level ID must silently skip the load — no exception, no load");
        }

        [Test]
        public void TransitionRequested_UnknownLevelId_DoesNotUpdateCurrentScene() {
            _store.RequestTransition(target_id: "nonexistent_level");
            Assert.That(_store.Scenario.initial_state.current_node, Is.EqualTo("level_01"),
                "current_node must remain unchanged when target level is not found");
        }

        [Test]
        public void TransitionRequested_UnknownLevelId_DoesNotMarkDirty() {
            _store.RequestTransition(target_id: "nonexistent_level");
            Assert.That(_store.IsDirty, Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // Sequential transitions

        [Test]
        public void TransitionRequested_SequentialTransitions_EachLoadsCorrectScene() {
            _store.RequestTransition(target_id: "level_02");
            Assert.That(_loaded, Is.EqualTo("Scene_Level2"));
            Assert.That(_store.Scenario.initial_state.current_node, Is.EqualTo("level_02"));

            _store.RequestTransition(target_id: "level_03");
            Assert.That(_loaded, Is.EqualTo("Scene_Level3"));
            Assert.That(_store.Scenario.initial_state.current_node, Is.EqualTo("level_03"));
        }

        [Test]
        public void TransitionRequested_ReloadSameLevel_Works() {
            _store.RequestTransition(target_id: "level_01");
            Assert.That(_loaded, Is.EqualTo("Scene_Level1"));
            Assert.That(_store.Scenario.initial_state.current_node, Is.EqualTo("level_01"));
        }

        // -----------------------------------------------------------------------------------------
        // Via Executor → full event chain test

        [Test]
        public void Executor_RequestTransition_TriggersLoad() {
            Executor.Execute(command: new Command {
                request_transition = "level_03"
            }, store: _store);
            Assert.That(_loaded, Is.EqualTo("Scene_Level3"));
        }

        // -----------------------------------------------------------------------------------------
        // Dispose

        [Test]
        public void Dispose_PreventsFurtherLoads() {
            _loader.Dispose();
            _store.RequestTransition(target_id: "level_02");
            Assert.That(_loaded, Is.Null,
                "After Dispose, TransitionRequested must not trigger a load");
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow() {
            _loader.Dispose();
            Assert.DoesNotThrow(() => _loader.Dispose());
        }
    }
}