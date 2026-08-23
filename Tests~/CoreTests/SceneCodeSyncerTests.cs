// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Germio;
using Germio.Editor;
using Germio.Model;

namespace Germio.Tests.Editor {
    /// <summary>
    /// Unit tests for <see cref="SceneCodeSyncer"/> (Phase 5.19).
    /// 
    /// Covers all 32 test cases enumerated in
    /// docs/development_plan_phase_5_19_spec_JP.md §9.
    /// 
    /// Decision-table coverage:
    ///   - Axis A (JSON node state): A1..A6
    ///   - Axis B (C# file state): B1..B5
    ///   - Axis C (user edit presence): C1..C3
    ///   - Axis D (directory ops): D1..D3
    ///   - Axis E (line endings / BOM): E1..E3
    ///   - Axis F (modifier preservation): F1..F2
    ///   - Axis G (Unity meta): G1..G5
    ///   - Axis X (error detection): X1..X4
    ///   - Axis Z (idempotency): Z1..Z2
    /// 
    /// Per spec §10 (TDD strategy), this entire file is RED on first write.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    [TestFixture]
    public class SceneCodeSyncerTests {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        /// <summary>Temporary working directory created per test.</summary>
        string _temp_dir = string.Empty;

        /// <summary>Path to the simulated Scenes/ root inside <see cref="_temp_dir"/>.</summary>
        string _scenes_root = string.Empty;

        /// <summary>Path to the simulated germio.json file inside <see cref="_temp_dir"/>.</summary>
        string _germio_json_path = string.Empty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // setup / teardown

        [SetUp]
        public void SetUp() {
            _temp_dir = Path.Combine(
                path1: Path.GetTempPath(),
                path2: $"germio_phase5_19_{Guid.NewGuid():N}");
            Directory.CreateDirectory(path: _temp_dir);
            _scenes_root = Path.Combine(path1: _temp_dir, path2: "Scenes");
            _germio_json_path = Path.Combine(path1: _temp_dir, path2: "germio.json");
        }

        [TearDown]
        public void TearDown() {
            if (Directory.Exists(path: _temp_dir)) {
                Directory.Delete(path: _temp_dir, recursive: true);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.1 Unique behaviours T1..T10 (10 tests)

        /// <summary>#1 — T1: New node, directory and file both absent → both created.</summary>
        [Test]
        public void Sync_AssignsNewFile_OnNewNode() {
            // RED: SceneCodeSyncer not yet implemented.
            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // 2 files expected: World.cs (root) + World/Title.cs (child).
            Assert.That(actual: result.created_files.Count, expression: Is.EqualTo(expected: 2));
            string expected_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            Assert.That(actual: File.Exists(path: expected_path), expression: Is.True);
        }

        /// <summary>#2 — T2: New node, parent directory exists → only file created.</summary>
        [Test]
        public void Sync_AssignsNewFile_OnNewNodeInExistingDir() {
            Directory.CreateDirectory(path: Path.Combine(path1: _scenes_root, path2: "World"));
            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // World.cs (root, in scenes_root) + World/Title.cs (child).
            Assert.That(actual: result.created_files.Count, expression: Is.EqualTo(expected: 2));
            // The "World" sub-directory was already pre-created.
            Assert.That(actual: result.created_directories.Count, expression: Is.EqualTo(expected: 0));
        }

        /// <summary>#3 — T3: Existing node, file manually deleted → recreated.</summary>
        [Test]
        public void Sync_RecreatesFile_WhenManuallyDeleted() {
            // First sync to create file.
            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // User manually deletes the file.
            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            File.Delete(path: file_path);

            // Second sync should recreate.
            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            Assert.That(actual: File.Exists(path: file_path), expression: Is.True);
            Assert.That(actual: result.created_files.Count, expression: Is.GreaterThanOrEqualTo(expected: 1));
        }

        /// <summary>#4 — T4: Existing node unchanged → file not modified (mtime invariant).</summary>
        [Test]
        public void Sync_DoesNothing_OnUnchangedNode() {
            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            DateTime mtime_before = File.GetLastWriteTimeUtc(path: file_path);

            // Sleep a moment so any write would change mtime.
            System.Threading.Thread.Sleep(millisecondsTimeout: 50);
            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            DateTime mtime_after = File.GetLastWriteTimeUtc(path: file_path);
            Assert.That(actual: mtime_after, expression: Is.EqualTo(expected: mtime_before));
            Assert.That(actual: result.modified_files.Count, expression: Is.EqualTo(expected: 0));
        }

        /// <summary>#5 — T5: Existing node, file in unexpected directory → moved to expected dir.</summary>
        [Test]
        public void Sync_MovesFile_WhenInUnexpectedDir() {
            // Create file in wrong location.
            string wrong_dir = Path.Combine(path1: _scenes_root, path2: "Misplaced");
            Directory.CreateDirectory(path: wrong_dir);
            string wrong_path = Path.Combine(path1: wrong_dir, path2: "Title.cs");
            File.WriteAllText(path: wrong_path, contents: build_skeleton_text(class_name: "Title", parent_class: "World", node_id: "title", handler_name: "OnTitle"));

            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string expected_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            Assert.That(actual: File.Exists(path: expected_path), expression: Is.True);
            Assert.That(actual: File.Exists(path: wrong_path), expression: Is.False);
            Assert.That(actual: result.moved_files.Count, expression: Is.GreaterThanOrEqualTo(expected: 1));
        }

        /// <summary>#6 — T6: Same id attribute appears in multiple files → error reported.</summary>
        [Test]
        public void Sync_ReportsError_OnDuplicateIdInCSharp() {
            string dir = Path.Combine(path1: _scenes_root, path2: "World");
            Directory.CreateDirectory(path: dir);
            string text = build_skeleton_text(class_name: "Title", parent_class: "World", node_id: "title", handler_name: "OnTitle");
            File.WriteAllText(path: Path.Combine(path1: dir, path2: "Title.cs"), contents: text);
            File.WriteAllText(path: Path.Combine(path1: dir, path2: "TitleCopy.cs"), contents: text.Replace(oldValue: "class Title", newValue: "class TitleCopy"));

            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            Assert.That(actual: result.errors.Count, expression: Is.GreaterThanOrEqualTo(expected: 1));
            Assert.That(actual: result.errors.Any(predicate: e => e.Contains(value: "Duplicate")), expression: Is.True);
        }

        /// <summary>#7 — T7: Scene rename → file/class/method/attribute id all updated, body preserved.</summary>
        [Test]
        public void Sync_RenamesFileAndClass_OnSceneChange() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Inject user logic into the handler body.
            string old_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string content = File.ReadAllText(path: old_path);
            content = content.Replace(
                oldValue: "// Empty placeholder. Add Title-specific logic here.",
                newValue: "var x = 42; // user logic");
            File.WriteAllText(path: old_path, contents: content);

            // Rename scene.
            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            Assert.That(actual: File.Exists(path: new_path), expression: Is.True);
            Assert.That(actual: File.Exists(path: old_path), expression: Is.False);

            string new_content = File.ReadAllText(path: new_path);
            Assert.That(actual: new_content, expression: Does.Contain(expected: "class TitleScreen"));
            // Per spec §5.2.2, handler name follows the node id (unchanged here, still "title").
            Assert.That(actual: new_content, expression: Does.Contain(expected: "OnTitle"));
            Assert.That(actual: new_content, expression: Does.Contain(expected: "var x = 42; // user logic"),
                message: "User-edited handler body must be preserved.");
        }

        /// <summary>#8 — T7': Scene rename + file in unexpected dir → renamed and moved.</summary>
        [Test]
        public void Sync_RenamesAndMoves_OnSceneAndParentChange() {
            // Pre-existing file in wrong location with old class name.
            string wrong_dir = Path.Combine(path1: _scenes_root, path2: "Misplaced");
            Directory.CreateDirectory(path: wrong_dir);
            string wrong_path = Path.Combine(path1: wrong_dir, path2: "Title.cs");
            File.WriteAllText(path: wrong_path, contents: build_skeleton_text(class_name: "Title", parent_class: "World", node_id: "title", handler_name: "OnTitle"));

            // JSON has scene renamed.
            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string expected_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            Assert.That(actual: File.Exists(path: expected_path), expression: Is.True);
            Assert.That(actual: File.Exists(path: wrong_path), expression: Is.False);
        }

        /// <summary>#9 — T8: Parent change → directory moved, inheritance line updated, empty dir removed.</summary>
        [Test]
        public void Sync_MovesAndUpdatesInheritance_OnParentChange() {
            // Initial: level_1 under levels.
            var scenario1 = build_three_level_scenario();
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Move level_1 directly under root world (parent change).
            var scenario2 = build_scenario_with_level1_under_world();
            write_germio_json(scenario: scenario2);
            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Level1.cs");
            Assert.That(actual: File.Exists(path: new_path), expression: Is.True);
            string content = File.ReadAllText(path: new_path);
            Assert.That(actual: content, expression: Does.Contain(expected: "class Level1 : World"),
                message: "Inheritance must follow new parent.");
        }

        /// <summary>#10 — T9: name/kind/rules/next changes only → C# untouched.</summary>
        [Test]
        public void Sync_DoesNothing_OnNonIdentityChange() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);
            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            DateTime mtime_before = File.GetLastWriteTimeUtc(path: file_path);

            // Change only `name` field on the node.
            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            scenario2.root.children[0].name = "Renamed Display Name";
            write_germio_json(scenario: scenario2);

            System.Threading.Thread.Sleep(millisecondsTimeout: 50);
            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            DateTime mtime_after = File.GetLastWriteTimeUtc(path: file_path);
            Assert.That(actual: mtime_after, expression: Is.EqualTo(expected: mtime_before));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.2 Node deletion and orphan (1 test)

        /// <summary>#11 — T10: Node removed from JSON → orphan marker prepended, file kept.</summary>
        [Test]
        public void Sync_MarksOrphan_OnNodeRemoved() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Remove node.
            var scenario2 = new Scenario { schema_version = 1 };
            scenario2.root = new Node { id = "world", scene = "" };
            write_germio_json(scenario: scenario2);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            Assert.That(actual: File.Exists(path: file_path), expression: Is.True,
                message: "Orphan file must NOT be deleted.");
            string content = File.ReadAllText(path: file_path);
            Assert.That(actual: content, expression: Does.Contain(expected: "germio: orphan"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.3 User edit preservation C series (3 tests)

        /// <summary>#12 — T7 × C1: Handler body preserved through scene rename.</summary>
        [Test]
        public void Sync_PreservesHandlerBody_OnSceneRename() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string old_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string content = File.ReadAllText(path: old_path);
            content = content.Replace(
                oldValue: "// Empty placeholder. Add Title-specific logic here.",
                newValue: "Debug.Log(\"complex user logic\"); var z = 999;");
            File.WriteAllText(path: old_path, contents: content);

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            string new_content = File.ReadAllText(path: new_path);
            Assert.That(actual: new_content, expression: Does.Contain(expected: "Debug.Log(\"complex user logic\"); var z = 999;"));
        }

        /// <summary>#13 — T7/T8 × C2: Non-handler methods and fields preserved through any change.</summary>
        [Test]
        public void Sync_PreservesUserMethodsAndFields_OnAnyChange() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string old_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string content = File.ReadAllText(path: old_path);
            // Inject a user field and a separate method into the class.
            content = content.Replace(
                oldValue: "[GermioSceneHandler",
                newValue: "[SerializeField] private int _user_field = 42;\n        void user_method() { /* user code */ }\n\n        [GermioSceneHandler");
            File.WriteAllText(path: old_path, contents: content);

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            string new_content = File.ReadAllText(path: new_path);
            Assert.That(actual: new_content, expression: Does.Contain(expected: "_user_field = 42"));
            Assert.That(actual: new_content, expression: Does.Contain(expected: "void user_method()"));
        }

        /// <summary>#14 — T8 × C1: Handler body preserved through parent change.</summary>
        [Test]
        public void Sync_PreservesHandlerBody_OnParentChange() {
            var scenario1 = build_three_level_scenario();
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string old_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Levels", path4: "Level1.cs");
            string content = File.ReadAllText(path: old_path);
            content = content.Replace(
                oldValue: "// Empty placeholder. Add Level1-specific logic here.",
                newValue: "var stage_specific = \"hard mode\";");
            File.WriteAllText(path: old_path, contents: content);

            var scenario2 = build_scenario_with_level1_under_world();
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Level1.cs");
            string new_content = File.ReadAllText(path: new_path);
            Assert.That(actual: new_content, expression: Does.Contain(expected: "var stage_specific = \"hard mode\""));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.4 Directory operations D series (3 tests)

        /// <summary>#15 — D1: Multi-level directories created when missing.</summary>
        [Test]
        public void Sync_CreatesNestedDirectory_WhenMissing() {
            var scenario = build_three_level_scenario();
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string nested_dir = Path.Combine(path1: _scenes_root, path2: "World", path3: "Levels");
            Assert.That(actual: Directory.Exists(path: nested_dir), expression: Is.True);
        }

        /// <summary>#16 — D2: Empty directory removed after move.</summary>
        [Test]
        public void Sync_RemovesEmptyDirectory_AfterMove() {
            var scenario1 = build_three_level_scenario();
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Move all level_* under world; "Levels" dir becomes empty.
            var scenario2 = build_scenario_all_levels_under_world();
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string old_levels_dir = Path.Combine(path1: _scenes_root, path2: "World", path3: "Levels");
            Assert.That(actual: Directory.Exists(path: old_levels_dir), expression: Is.False);
        }

        /// <summary>#17 — D3: Non-empty directory preserved.</summary>
        [Test]
        public void Sync_KeepsNonEmptyDirectory_AfterMove() {
            var scenario1 = build_three_level_scenario();
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Add an unrelated file inside Levels/.
            string user_file = Path.Combine(path1: _scenes_root, path2: "World", path3: "Levels", path4: "UserFile.cs");
            File.WriteAllText(path: user_file, contents: "// user owned");

            // Move only level_1 out: remove from levels.children, add as sibling of levels.
            var scenario2 = build_three_level_scenario();
            // scenario2.root.children = [ levels{ children=[level_1, level_2, level_3] } ]
            scenario2.root.children[0].children.RemoveAt(index: 0);  // remove level_1 from levels
            scenario2.root.children.Add(item: new Node { id = "level_1", scene = "Level_1", kind = "level" });
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            Assert.That(actual: File.Exists(path: user_file), expression: Is.True);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.5 Line endings / BOM E series (3 tests)

        /// <summary>#18 — E1: CRLF line endings preserved.</summary>
        [Test]
        public void Sync_PreservesLineEndings_CRLF() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string content = File.ReadAllText(path: file_path).Replace(oldValue: "\r\n", newValue: "\n").Replace(oldValue: "\n", newValue: "\r\n");
            File.WriteAllText(path: file_path, contents: content);

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            byte[] bytes = File.ReadAllBytes(path: new_path);
            string text = System.Text.Encoding.UTF8.GetString(bytes: bytes);
            Assert.That(actual: text.Contains(value: "\r\n"), expression: Is.True);
            Assert.That(actual: text.Replace(oldValue: "\r\n", newValue: "").Contains(value: "\n"), expression: Is.False,
                message: "No bare LF should remain when source was CRLF.");
        }

        /// <summary>#19 — E2: LF line endings preserved.</summary>
        [Test]
        public void Sync_PreservesLineEndings_LF() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string content = File.ReadAllText(path: file_path).Replace(oldValue: "\r\n", newValue: "\n");
            File.WriteAllText(path: file_path, contents: content);

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            byte[] bytes = File.ReadAllBytes(path: new_path);
            string text = System.Text.Encoding.UTF8.GetString(bytes: bytes);
            Assert.That(actual: text.Contains(value: "\r\n"), expression: Is.False,
                message: "No CRLF should appear when source was LF.");
        }

        /// <summary>#20 — E3: UTF-8 BOM presence preserved.</summary>
        [Test]
        public void Sync_PreservesUtf8Bom_WhenPresent() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string content = File.ReadAllText(path: file_path);
            // Add BOM.
            File.WriteAllText(path: file_path, contents: content, encoding: new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            byte[] bytes = File.ReadAllBytes(path: new_path);
            Assert.That(actual: bytes.Length, expression: Is.GreaterThan(expected: 3));
            Assert.That(actual: bytes[0], expression: Is.EqualTo(expected: (byte)0xEF));
            Assert.That(actual: bytes[1], expression: Is.EqualTo(expected: (byte)0xBB));
            Assert.That(actual: bytes[2], expression: Is.EqualTo(expected: (byte)0xBF));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.6 Modifier preservation F series (2 tests)

        /// <summary>#21 — F1: `partial` (and similar) class modifiers preserved on rename.</summary>
        [Test]
        public void Sync_PreservesPartialModifier_OnRename() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string content = File.ReadAllText(path: file_path).Replace(
                oldValue: "public class Title : World {",
                newValue: "public partial class Title : World {");
            File.WriteAllText(path: file_path, contents: content);

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            string new_content = File.ReadAllText(path: new_path);
            Assert.That(actual: new_content, expression: Does.Contain(expected: "public partial class TitleScreen"));
        }

        /// <summary>#22 — F2: `using` statements preserved.</summary>
        [Test]
        public void Sync_PreservesUsingStatements() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string file_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string content = File.ReadAllText(path: file_path).Replace(
                oldValue: "using Germio;",
                newValue: "using Germio;\nusing UnityEngine;\nusing UniRx;");
            File.WriteAllText(path: file_path, contents: content);

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs");
            string new_content = File.ReadAllText(path: new_path);
            Assert.That(actual: new_content, expression: Does.Contain(expected: "using UnityEngine;"));
            Assert.That(actual: new_content, expression: Does.Contain(expected: "using UniRx;"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.7 Unity meta files G series (5 tests)

        /// <summary>#23 — G1: .cs.meta renamed alongside .cs on scene rename.</summary>
        [Test]
        public void Sync_RenamesMetaFile_OnSceneRename() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Simulate Unity-created .cs.meta.
            string old_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string old_meta = old_path + ".meta";
            File.WriteAllText(path: old_meta, contents: "fileFormatVersion: 2\nguid: abcdef0123456789\n");

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_meta = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs.meta");
            Assert.That(actual: File.Exists(path: new_meta), expression: Is.True);
            Assert.That(actual: File.Exists(path: old_meta), expression: Is.False);
        }

        /// <summary>#24 — G2: .cs.meta moved alongside .cs on parent change.</summary>
        [Test]
        public void Sync_MovesMetaFile_OnParentChange() {
            var scenario1 = build_three_level_scenario();
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string old_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Levels", path4: "Level1.cs");
            string old_meta = old_path + ".meta";
            File.WriteAllText(path: old_meta, contents: "fileFormatVersion: 2\nguid: 1111111122222222\n");

            var scenario2 = build_scenario_with_level1_under_world();
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_meta = Path.Combine(path1: _scenes_root, path2: "World", path3: "Level1.cs.meta");
            Assert.That(actual: File.Exists(path: new_meta), expression: Is.True);
            Assert.That(actual: File.Exists(path: old_meta), expression: Is.False);
        }

        /// <summary>#25 — G3: .cs.meta GUID content preserved through rename/move.</summary>
        [Test]
        public void Sync_PreservesMetaContent_OnRenameAndMove() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string old_meta = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs.meta");
            string original_content = "fileFormatVersion: 2\nguid: deadbeefcafebabe1234567890abcdef\n";
            File.WriteAllText(path: old_meta, contents: original_content);

            var scenario2 = build_minimal_scenario_with_node(node_id: "title", scene: "TitleScreen");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string new_meta = Path.Combine(path1: _scenes_root, path2: "World", path3: "TitleScreen.cs.meta");
            string new_content = File.ReadAllText(path: new_meta);
            Assert.That(actual: new_content, expression: Is.EqualTo(expected: original_content),
                message: "Meta GUID/content must NOT be altered by syncer.");
        }

        /// <summary>#26 — G4: Generator does NOT create directory.meta (Unity owns this).</summary>
        [Test]
        public void Sync_LeavesDirectoryMetaToUnity_OnNewDir() {
            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);

            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            string dir_meta = Path.Combine(path1: _scenes_root, path2: "World.meta");
            Assert.That(actual: File.Exists(path: dir_meta), expression: Is.False,
                message: "Generator must NOT create directory .meta files.");
        }

        /// <summary>#27 — G5: directory.meta deleted alongside empty directory.</summary>
        [Test]
        public void Sync_DeletesDirectoryMeta_OnEmptyDirRemoval() {
            var scenario1 = build_three_level_scenario();
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Simulate Unity-created Levels.meta on the dir we'll empty.
            string levels_dir = Path.Combine(path1: _scenes_root, path2: "World", path3: "Levels");
            string levels_meta = levels_dir + ".meta";
            File.WriteAllText(path: levels_meta, contents: "fileFormatVersion: 2\nguid: aaaaaaaabbbbbbbb\n");

            // Move all level_* out so Levels dir becomes empty.
            var scenario2 = build_scenario_all_levels_under_world();
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            Assert.That(actual: Directory.Exists(path: levels_dir), expression: Is.False);
            Assert.That(actual: File.Exists(path: levels_meta), expression: Is.False);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.8 Error detection X series (4 tests)

        /// <summary>#28 — X3: V004 violation halts sync entirely.</summary>
        [Test]
        public void Sync_StopsAndReports_OnV004Violation() {
            // Build a scenario with two nodes sharing the same id.
            var scenario = new Scenario { schema_version = 1 };
            scenario.root = new Node { id = "world", scene = "" };
            scenario.root.children = new List<Node> {
                new Node { id = "duplicate", scene = "Title" },
                new Node { id = "duplicate", scene = "Select" },
            };
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            Assert.That(actual: result.errors.Count, expression: Is.GreaterThanOrEqualTo(expected: 1));
            Assert.That(actual: result.errors.Any(predicate: e => e.Contains(value: "V004")), expression: Is.True);
            Assert.That(actual: result.created_files.Count, expression: Is.EqualTo(expected: 0),
                message: "No files should be created when V004 is violated.");
        }

        /// <summary>#29 — X2: Handler with id not in JSON → warning + orphan marker.</summary>
        [Test]
        public void Sync_ReportsWarning_OnHandlerWithUnknownId() {
            // Pre-existing C# file with id="ghost" not in JSON.
            string dir = Path.Combine(path1: _scenes_root, path2: "World");
            Directory.CreateDirectory(path: dir);
            string path = Path.Combine(path1: dir, path2: "Ghost.cs");
            File.WriteAllText(path: path, contents: build_skeleton_text(class_name: "Ghost", parent_class: "World", node_id: "ghost", handler_name: "OnGhost"));

            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            Assert.That(actual: result.warnings.Any(predicate: w => w.Contains(value: "ghost")), expression: Is.True);
            string content = File.ReadAllText(path: path);
            Assert.That(actual: content, expression: Does.Contain(expected: "germio: orphan"));
        }

        /// <summary>#30 — X1: Duplicate [GermioSceneHandler] attribute reported as error.</summary>
        [Test]
        public void Sync_ReportsError_OnDuplicateAttribute() {
            string dir = Path.Combine(path1: _scenes_root, path2: "World");
            Directory.CreateDirectory(path: dir);
            string text = build_skeleton_text(class_name: "Title", parent_class: "World", node_id: "title", handler_name: "OnTitle");
            File.WriteAllText(path: Path.Combine(path1: dir, path2: "TitleA.cs"), contents: text.Replace(oldValue: "class Title", newValue: "class TitleA"));
            File.WriteAllText(path: Path.Combine(path1: dir, path2: "TitleB.cs"), contents: text.Replace(oldValue: "class Title", newValue: "class TitleB"));

            var scenario = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario);

            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            Assert.That(actual: result.errors.Count, expression: Is.GreaterThanOrEqualTo(expected: 1));
        }

        /// <summary>#31 — X4: Id rename treated as add + orphan.</summary>
        [Test]
        public void Sync_TreatsIdRenameAsAddPlusOrphan() {
            var scenario1 = build_minimal_scenario_with_node(node_id: "title", scene: "Title");
            write_germio_json(scenario: scenario1);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // JSON renames id from "title" to "home", scene becomes "Home".
            var scenario2 = build_minimal_scenario_with_node(node_id: "home", scene: "Home");
            write_germio_json(scenario: scenario2);
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Old C# file remains as orphan (still id="title").
            string old_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Title.cs");
            string new_path = Path.Combine(path1: _scenes_root, path2: "World", path3: "Home.cs");
            Assert.That(actual: File.Exists(path: old_path), expression: Is.True);
            Assert.That(actual: File.Exists(path: new_path), expression: Is.True);
            string old_content = File.ReadAllText(path: old_path);
            Assert.That(actual: old_content, expression: Does.Contain(expected: "germio: orphan"));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // 9.9 Idempotency Z series (1 test)

        /// <summary>#32 — Z1+Z2: Run twice with no changes → no file modification.</summary>
        [Test]
        public void Sync_RunTwiceWithoutChanges_NoFileModified() {
            var scenario = build_three_level_scenario();
            write_germio_json(scenario: scenario);

            // First sync (creates).
            SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Snapshot all file mtimes.
            var snapshot = collect_file_mtimes(root: _scenes_root);

            // Second sync (should be no-op).
            System.Threading.Thread.Sleep(millisecondsTimeout: 50);
            var result = SceneCodeSyncer.Sync(germio_json_path: _germio_json_path, scenes_root: _scenes_root);

            // Verify no mtime changed.
            foreach (var kv in snapshot) {
                Assert.That(actual: File.GetLastWriteTimeUtc(path: kv.Key), expression: Is.EqualTo(expected: kv.Value),
                    message: $"File {kv.Key} mtime changed; sync is not idempotent.");
            }
            Assert.That(actual: result.modified_files.Count, expression: Is.EqualTo(expected: 0));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods (test helpers) [verb]

        /// <summary>Build a minimal scenario containing a single child node beneath the root world.</summary>
        Scenario build_minimal_scenario_with_node(string node_id, string scene) {
            var s = new Scenario { schema_version = 1 };
            s.root = new Node { id = "world", scene = "", kind = "world" };
            s.root.children = new List<Node> {
                new Node { id = node_id, scene = scene, kind = "title" },
            };
            return s;
        }

        /// <summary>Build a scenario with three levels under a "levels" intermediate node.</summary>
        Scenario build_three_level_scenario() {
            var s = new Scenario { schema_version = 1 };
            s.root = new Node { id = "world", scene = "", kind = "world" };
            var levels = new Node { id = "levels", scene = "", kind = "world" };
            levels.children = new List<Node> {
                new Node { id = "level_1", scene = "Level_1", kind = "level" },
                new Node { id = "level_2", scene = "Level_2", kind = "level" },
                new Node { id = "level_3", scene = "Level_3", kind = "level" },
            };
            s.root.children = new List<Node> { levels };
            return s;
        }

        /// <summary>Build a scenario where level_1 sits directly under world (not under levels).</summary>
        Scenario build_scenario_with_level1_under_world() {
            var s = new Scenario { schema_version = 1 };
            s.root = new Node { id = "world", scene = "", kind = "world" };
            var levels = new Node { id = "levels", scene = "", kind = "world" };
            levels.children = new List<Node> {
                new Node { id = "level_2", scene = "Level_2", kind = "level" },
                new Node { id = "level_3", scene = "Level_3", kind = "level" },
            };
            s.root.children = new List<Node> {
                levels,
                new Node { id = "level_1", scene = "Level_1", kind = "level" },
            };
            return s;
        }

        /// <summary>Build a scenario where all three levels sit directly under world.</summary>
        Scenario build_scenario_all_levels_under_world() {
            var s = new Scenario { schema_version = 1 };
            s.root = new Node { id = "world", scene = "", kind = "world" };
            s.root.children = new List<Node> {
                new Node { id = "level_1", scene = "Level_1", kind = "level" },
                new Node { id = "level_2", scene = "Level_2", kind = "level" },
                new Node { id = "level_3", scene = "Level_3", kind = "level" },
            };
            return s;
        }

        /// <summary>Serialize a scenario to <see cref="_germio_json_path"/>.</summary>
        void write_germio_json(Scenario scenario) {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(value: scenario, formatting: Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(path: _germio_json_path, contents: json);
        }

        /// <summary>Build a skeleton C# scene file matching the spec's template (§6.4).</summary>
        string build_skeleton_text(string class_name, string parent_class, string node_id, string handler_name) {
            return
                "// Copyright (c) STUDIO MeowToon. All rights reserved.\n" +
                "// Licensed under the GPL v2.0 license.\n" +
                "\n" +
                "using Germio;\n" +
                "\n" +
                "namespace GameDev {\n" +
                "    /// <summary>Test skeleton.</summary>\n" +
                "    public class " + class_name + " : " + parent_class + " {\n" +
                "        [GermioSceneHandler(id: \"" + node_id + "\")]\n" +
                "        protected void " + handler_name + "() {\n" +
                "            // Empty placeholder. Add " + class_name + "-specific logic here.\n" +
                "        }\n" +
                "    }\n" +
                "}\n";
        }

        /// <summary>Walk a directory and collect every file's last-write time.</summary>
        Dictionary<string, DateTime> collect_file_mtimes(string root) {
            var dict = new Dictionary<string, DateTime>();
            if (!Directory.Exists(path: root)) { return dict; }
            foreach (string p in Directory.EnumerateFiles(path: root, searchPattern: "*", searchOption: SearchOption.AllDirectories)) {
                dict[p] = File.GetLastWriteTimeUtc(path: p);
            }
            return dict;
        }
    }
}
