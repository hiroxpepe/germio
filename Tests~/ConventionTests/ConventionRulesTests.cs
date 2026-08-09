// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable
using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Germio.Tests.Convention {
    /// <summary>
    /// Verifies the rules themselves against mock code. Without this, a green
    /// convention run only proves the scan ran, not that it can catch anything.
    /// Every rule gets a dirty case (must be caught) and a clean case (must pass).
    /// </summary>
    [TestFixture]
    [Category("Convention")]
    public class ConventionRulesTests
    {
        [Test]
        public void Catches_MissingNullableInHeader()
        {
            var code = "// Copyright (c) STUDIO MeowToon. All rights reserved.\n"
                + "// Licensed under the MIT License. See LICENSE in the project root for license information.\n"
                + "\n"
                + "using System;\n";
            var found = ConventionRules.find_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("must be '#nullable enable'")), Is.True);
        }

        [Test]
        public void Passes_TheStandardHeader()
        {
            var code = "// Copyright (c) STUDIO MeowToon. All rights reserved.\n"
                + "// Licensed under the MIT License. See LICENSE in the project root for license information.\n"
                + "\n"
                + "#nullable enable\n"
                + "\n"
                + "using System;\n";
            var found = ConventionRules.find_header_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_TwoBlankLinesInARow()
        {
            var code = "class Mock {\n\n\n    void run() {}\n}";
            var found = ConventionRules.find_blank_line_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("keep only one")), Is.True);
        }

        [Test]
        public void Passes_OneBlankLine()
        {
            var code = "class Mock {\n\n    void run() {}\n}";
            var found = ConventionRules.find_blank_line_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_FreeFormLabelDoesNotExemptTheKindRequirement()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Step 2: EffectiveNeeds\n"
                + "\n"
                + "        void step2EffectiveNeeds() {}\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("need a section header") && v.Contains("Methods [verb]")), Is.True);
        }

        [Test]
        public void Passes_WhenAKindDividerSitsAboveAFreeFormOne()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Methods [verb]\n"
                + "\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Step 2: EffectiveNeeds\n"
                + "\n"
                + "        void step2EffectiveNeeds() {}\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("need a section header")), Is.False);
        }

        [Test]
        public void Catches_DividerMissingBlankLineAbove()
        {
            var code = "class Mock {\n"
                + "        int x;\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Methods [verb]\n"
                + "\n"
                + "        void run() {}\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("needs a blank line above it")), Is.True);
        }

        [Test]
        public void Passes_DividerRightAfterAnOpeningBraceNeedsNoBlankLine()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Fields\n"
                + "\n"
                + "        int x;\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("needs a blank line above it")), Is.False);
        }

        [Test]
        public void Passes_PublicInnerClassesIsRecognizedAsAKindMatch()
        {
            var code = "class Outer {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // public inner Classes\n"
                + "\n"
                + "        public class Inner {}\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_ConceptEventNameShapedMethodByDefault()
        {
            // The concept_EventName shape (Button_Click) is not exempt on its
            // own — only an exact entry in naming_exceptions.md is. A method
            // must be explicitly reviewed and named, never waved through by shape.
            var code = "class Mock { public void abilities_OnAwake() {} }";
            Assert.That(caught(code, "must be PascalCase"), Is.True);
        }

        [Test]
        public void Catches_EventNameNotEndingInPastOrPresentParticiple()
        {
            // An event name's last word must read as a single, completed
            // happening (past participle) or an ongoing state (present
            // participle) — never the bare, command-shaped verb.
            var code = "class Mock { public event Action? RequestTransition; }";
            Assert.That(caught(code, "past or present participle"), Is.True);
        }

        [Test]
        public void Allows_EventNameEndingInEd()
        {
            var code = "class Mock { public event Action<string>? TransitionRequested; }";
            Assert.That(caught(code, "past or present participle"), Is.False);
        }

        [Test]
        public void Allows_EventNameEndingInIng()
        {
            var code = "class Mock { public event Action? Playbacking; }";
            Assert.That(caught(code, "past or present participle"), Is.False);
        }

        [Test]
        public void Catches_ExplicitAccessorEventNotEndingInParticiple()
        {
            // EventDeclarationSyntax (an event with explicit add/remove
            // accessors) must be held to the same participle rule as the
            // plain EventFieldDeclarationSyntax form.
            var code = "class Mock {\n"
                + "    event Action? _backing;\n"
                + "    public event Action? RequestTransition {\n"
                + "        add { _backing += value; }\n"
                + "        remove { _backing -= value; }\n"
                + "    }\n"
                + "}";
            Assert.That(caught(code, "past or present participle"), Is.True);
        }

        [Test]
        public void Allows_ExplicitAccessorEventEndingInParticiple()
        {
            var code = "class Mock {\n"
                + "    event Action? _backing;\n"
                + "    public event Action? TransitionRequested {\n"
                + "        add { _backing += value; }\n"
                + "        remove { _backing -= value; }\n"
                + "    }\n"
                + "}";
            Assert.That(caught(code, "past or present participle"), Is.False);
        }

        [Test]
        public void Allows_AcronymPrefixedEventEndingInParticiple()
        {
            // word_parts keeps a run of caps together as one acronym token
            // (JSON, HTTP); the participle check must still read the LAST
            // word part, not the acronym, so a name like OnHTTPRequested
            // is judged on "Requested", not on "HTTP".
            var code = "class Mock { public event Action? HTTPRequested; }";
            Assert.That(caught(code, "past or present participle"), Is.False);
        }

        [Test]
        public void Catches_AcronymPrefixedEventNotEndingInParticiple()
        {
            var code = "class Mock { public event Action? HTTPRequest; }";
            Assert.That(caught(code, "past or present participle"), Is.True);
        }

        [Test]
        public void Catches_InterfaceEventNotEndingInParticiple()
        {
            // An event declared inside an interface body is still a public
            // surface every implementer and caller sees; the participle
            // rule applies to it the same as a class-level event.
            var code = "interface IMock { event Action? RequestTransition; }";
            Assert.That(caught(code, "past or present participle"), Is.True);
        }

        [Test]
        public void Allows_InterfaceEventEndingInParticiple()
        {
            var code = "interface IMock { event Action? TransitionRequested; }";
            Assert.That(caught(code, "past or present participle"), Is.False);
        }

        [Test]
        public void Catches_PlainCamelCaseMethodStillFails()
        {
            var code = "class Mock { public void getPushedDirection() {} }";
            Assert.That(caught(code, "must be PascalCase"), Is.True);
        }

        [Test]
        public void Catches_ProtectedFieldStillNeedsPascalCaseByDefault()
        {
            var code = "class Mock { protected int do_update; }";
            Assert.That(caught(code, "must be PascalCase"), Is.True);
        }

        [Test]
        public void Catches_PublicFieldStillNeedsPascalCase()
        {
            var code = "class Mock { public int _do_update; }";
            Assert.That(caught(code, "must be PascalCase"), Is.True);
        }

        [Test]
        public void Probe_AllRemaining()
        {
            foreach (var path in ConventionScan.source_files()) {
                var code = System.IO.File.ReadAllText(path);
                var found = ConventionRules.find_naming_violations(code, System.IO.Path.GetFileName(path));
                foreach (var f in found) System.Console.WriteLine("PROBE " + f);
            }
            Assert.Pass();
        }

        [Test]
        public void Catches_BlankLineAfterBlockScopedNamespace()
        {
            var code = "namespace Germio.Core {\n\n    class Mock {}\n}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found, Is.Not.Empty);
        }

        [Test]
        public void Catches_BlankLineAfterFileScopedNamespace()
        {
            var code = "namespace Webio.Core;\n\nclass Mock {}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found, Is.Not.Empty);
        }

        [Test]
        public void Passes_NoBlankLineAfterNamespace()
        {
            var code = "namespace Germio.Core {\n    class Mock {}\n}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_NamespaceRightAfterUsingWithNoBlankLine()
        {
            var code = "using Germio.Systems;\nnamespace Germio.Players {\n    class Mock {}\n}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("blank line above it")), Is.True);
        }

        [Test]
        public void Passes_NamespaceWithBlankLineAfterUsing()
        {
            var code = "using Germio.Systems;\n\nnamespace Germio.Players {\n    class Mock {}\n}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("blank line above it")), Is.False);
        }

        [Test]
        public void Catches_BlankLineAfterTypeDeclaration()
        {
            var code = "class Mock {\n\n    int x;\n}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("type")), Is.True);
        }

        [Test]
        public void Catches_BlankLineAfterInnerClassDeclaration()
        {
            var code = "class Human {\n    protected class Acceleration {\n\n        int x;\n    }\n}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("type")), Is.True);
        }

        [Test]
        public void Passes_NoBlankLineAfterTypeDeclaration()
        {
            var code = "class Mock {\n    int x;\n}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("type")), Is.False);
        }

        [Test]
        public void Passes_OneLineEnumFollowedByBlankLine()
        {
            var code = "class Mock {\n    public enum Level { Error, Warning }\n\n    int x;\n}";
            var found = ConventionRules.find_namespace_gap_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("type")), Is.False);
        }

        [Test]
        public void Passes_PrivateFieldBeforeProtectedField()
        {
            var code = "class Mock {\n"
                + "    bool _flag;\n"
                + "    protected bool IsGrounded;\n"
                + "}";
            var found = ConventionRules.find_order_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_ProtectedFieldBeforePrivateField()
        {
            var code = "class Mock {\n"
                + "    protected bool IsGrounded;\n"
                + "    bool _flag;\n"
                + "}";
            var found = ConventionRules.find_order_violations(code, "mock.cs");
            Assert.That(found, Is.Not.Empty);
        }

        [Test]
        public void Passes_InstanceFieldBeforeStaticField()
        {
            var code = "class Mock {\n"
                + "    bool _flag;\n"
                + "    static bool _cache;\n"
                + "}";
            var found = ConventionRules.find_order_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Passes_PublicMethodBeforeProtectedMethod()
        {
            var code = "class Mock {\n"
                + "    public void Run() {}\n"
                + "    protected void step() {}\n"
                + "}";
            var found = ConventionRules.find_order_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Passes_StaticMethodBeforeInstanceMethod()
        {
            var code = "class Mock {\n"
                + "    public static void Create() {}\n"
                + "    public void Run() {}\n"
                + "}";
            var found = ConventionRules.find_order_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Passes_BareFieldsPackedTogether()
        {
            var code = "class Mock {\n"
                + "    int _a;\n"
                + "    int _b;\n"
                + "}";
            var found = ConventionRules.find_member_spacing_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_DocumentedFieldWithNoBlankLineAbove()
        {
            var code = "class Mock {\n"
                + "    int _a;\n"
                + "    /// <summary>b</summary>\n"
                + "    int _b;\n"
                + "}";
            var found = ConventionRules.find_member_spacing_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("above")), Is.True);
        }

        [Test]
        public void Catches_DocumentedFieldWithNoBlankLineBelow()
        {
            var code = "class Mock {\n"
                + "    /// <summary>a</summary>\n"
                + "    int _a;\n"
                + "    int _b;\n"
                + "}";
            var found = ConventionRules.find_member_spacing_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("below")), Is.True);
        }

        [Test]
        public void Passes_DocumentedFieldWithBlankLinesAround()
        {
            var code = "class Mock {\n"
                + "    int _a;\n"
                + "\n"
                + "    /// <summary>b</summary>\n"
                + "    int _b;\n"
                + "\n"
                + "    int _c;\n"
                + "}";
            var found = ConventionRules.find_member_spacing_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Passes_BareFieldRightAfterSectionHeaderWithBlank()
        {
            var code = "class Mock {\n"
                + "    /////////////////////////////\n"
                + "    // Fields\n"
                + "\n"
                + "    int _a;\n"
                + "}";
            var found = ConventionRules.find_member_spacing_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        static bool caught(string code, string needle) =>
            ConventionRules.find_naming_violations(code, "mock.cs").Any(v => v.Contains(needle));

        static int naming_count(string code) =>
            ConventionRules.find_naming_violations(code, "mock.cs").Count;

        static int order_count(string code) =>
            ConventionRules.find_order_violations(code, "mock.cs").Count;

        // ---- naming: dirty cases must be caught ------------------------------

        [Test]
        public void Catches_PrivateFieldNotSnakeCase()
        {
            Assert.That(caught("class Mock { int badField; }", "must be _snake_case"), Is.True);
        }

        [Test]
        public void Catches_OwnNamespaceUsingBeforeThirdParty()
        {
            var code = "namespace Briko.Editor {\n"
                + "    using System;\n"
                + "    using Briko.Editor.Internal;\n"
                + "    using UnityEngine;\n"
                + "    class Mock {}\n"
                + "}";
            var found = ConventionRules.find_using_order_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("out of group order")), Is.True);
        }

        [Test]
        public void Passes_UsingsInSystemThenThirdPartyThenOwnOrder()
        {
            var code = "namespace Briko.Editor {\n"
                + "    using System;\n"
                + "    using UnityEngine;\n"
                + "    using Briko.Editor.Internal;\n"
                + "    class Mock {}\n"
                + "}";
            var found = ConventionRules.find_using_order_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("out of group order")), Is.False);
        }

        [Test]
        public void Catches_BlankLineBetweenUsings()
        {
            var code = "namespace Briko.Editor {\n"
                + "    using System;\n"
                + "\n"
                + "    using UnityEngine;\n"
                + "    using Briko.Editor.Internal;\n"
                + "    class Mock {}\n"
                + "}";
            var found = ConventionRules.find_using_order_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("blank line")), Is.True);
        }

        [Test]
        public void Passes_UsingsWithNoBlankLineBetweenThem()
        {
            var code = "namespace Briko.Editor {\n"
                + "    using System;\n"
                + "    using UnityEngine;\n"
                + "    using Briko.Editor.Internal;\n"
                + "    class Mock {}\n"
                + "}";
            var found = ConventionRules.find_using_order_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("blank line")), Is.False);
        }

        [Test]
        public void Passes_UsingsSeparatedByCommentOrIfdefWithNoBlankLine()
        {
            var code = "using System;\n"
                + "// explanatory comment\n"
                + "#if !UNITY_5_3_OR_NEWER\n"
                + "using NJsonSchema;\n"
                + "#endif\n"
                + "namespace Briko.Editor { class Mock {} }";
            var found = ConventionRules.find_using_order_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("blank line")), Is.False);
        }

        [Test]
        public void Catches_OpeningBraceOnItsOwnLine()
        {
            var code = "class Mock\n{\n    void run() {}\n}";
            var found = ConventionRules.find_brace_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("opening brace must join the line above")), Is.True);
        }

        [Test]
        public void Passes_OpeningBraceOnTheSameLine()
        {
            var code = "class Mock {\n    void run() {}\n}";
            var found = ConventionRules.find_brace_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("opening brace must join the line above")), Is.False);
        }

        [Test]
        public void Passes_DestructorSectionWithBareLabel()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Destructor\n"
                + "\n"
                + "        ~Mock() {}\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_EnumSectionMissingAccessLevel()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Enums [noun]\n"
                + "\n"
                + "        public enum Level { Low, High }\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("must be 'public Enums [noun]'")), Is.True);
        }

        [Test]
        public void Passes_InterfaceSectionBareForPrivateInstance()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Interfaces\n"
                + "\n"
                + "        interface Helper {}\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_IndexerSectionMissingAccessLevel()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Indexers [noun, adjective]\n"
                + "\n"
                + "        public int this[int i] => i;\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("must be 'public Indexers [noun, adjective]'")), Is.True);
        }

        [Test]
        public void Catches_DividerNotOnColumn103()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////\n"
                + "        // Fields\n"
                + "\n"
                + "        int x;\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("column 103")), Is.True);
        }

        [Test]
        public void Passes_DividerOnColumn103()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Fields\n"
                + "\n"
                + "        int x;\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found, Is.Empty);
        }

        [Test]
        public void Catches_WordingDriftOnAKindLabel()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Private methods [verb, verb phrase]\n"
                + "\n"
                + "        void run() {}\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            Assert.That(found.Any(v => v.Contains("must be 'private Methods [verb]'")), Is.True);
        }

        [Test]
        public void Passes_FreeFormLabelWordingIsNeverRewrittenButKindIsStillRequired()
        {
            var code = "class Mock {\n"
                + "        ///////////////////////////////////////////////////////////////////////////////////////////////\n"
                + "        // Persona own-field merge (persona wins)\n"
                + "\n"
                + "        void run() {}\n"
                + "}";
            var found = ConventionRules.find_section_header_violations(code, "mock.cs");
            // The free-form text itself is never rewritten or flagged as a
            // wording problem — but the member still needs its own Kind
            // divider somewhere, so the "missing header" violation fires.
            Assert.That(found.Any(v => v.Contains("must be")), Is.False);
            Assert.That(found.Any(v => v.Contains("need a section header")), Is.True);
        }

        [Test]
        public void Catches_ExplicitPrivateKeyword()
        {
            Assert.That(caught("class Mock { private void run() {} }", "must omit the redundant 'private' keyword"), Is.True);
        }

        [Test]
        public void Passes_ImplicitPrivateWithNoKeyword()
        {
            Assert.That(caught("class Mock { void run() {} }", "must omit the redundant 'private' keyword"), Is.False);
        }

        [Test]
        public void Catches_ConstNotUpperSnake()
        {
            Assert.That(caught("class Mock { const int maxSize = 1; }", "must be UPPER_SNAKE"), Is.True);
        }

        [Test]
        public void Catches_LocalNotSnakeCase()
        {
            Assert.That(caught("class Mock { void run() { var itemCount = 1; } }", "local 'itemCount'"), Is.True);
        }

        [Test]
        public void Catches_ForEachVarNotSnakeCase()
        {
            Assert.That(caught("class Mock { void run() { foreach (var eachItem in x) {} } }", "foreach var 'eachItem'"), Is.True);
        }

        [Test]
        public void Catches_ParameterNotSnakeCase()
        {
            Assert.That(caught("class Mock { void run(int tabId) {} }", "parameter 'tabId'"), Is.True);
        }

        [Test]
        public void Catches_PublicMethodNotPascalCase()
        {
            Assert.That(caught("class Mock { public void doWork() {} }", "must be PascalCase"), Is.True);
        }

        [Test]
        public void Catches_PrivateMethodNotCamelCase()
        {
            Assert.That(caught("class Mock { private void DoWork() {} }", "must be camelCase"), Is.True);
        }

        [Test]
        public void Catches_EnumMemberNotPascalCase()
        {
            Assert.That(caught("enum M { first_value }", "must be PascalCase"), Is.True);
        }

        [Test]
        public void Catches_AbbreviationNotExpanded()
        {
            Assert.That(caught("class Mock { public void SendMsgNow() {} }", "unknown word part 'Msg'"), Is.True);
        }

        [Test]
        public void Catches_AcronymNotUpperCased()
        {
            Assert.That(caught("class Mock { public void ReadDomTree() {} }", "letter word 'Dom', use 'DOM'"), Is.True);
        }

        // ---- naming: clean cases must pass -----------------------------------

        [Test]
        public void Passes_CleanNaming()
        {
            var code = @"
    enum TabState { Idle, Stopped }

    class Watcher
    {
        const int MAX_TABS = 8;
        static readonly string DEFAULT_URL = ""x"";
        int _tab_count;

        public void Start(int tab_index)
        {
            var next_state = TabState.Idle;
            foreach (var each_tab in tabs) { }
        }

        void reset() { }
    }";
            Assert.That(naming_count(code), Is.Zero,
                string.Join("\n  ", ConventionRules.find_naming_violations(code, "mock.cs")));
        }

        [Test]
        public void Skips_OverrideMemberParameters()
        {
            // An override signature comes from outside, so its parameter names are exempt.
            Assert.That(naming_count("class Mock { public override void OnCreate(int savedState) {} }"), Is.Zero);
        }

        [Test]
        public void Allows_AcronymAlreadyUpperCased()
        {
            Assert.That(naming_count("class Mock { public void ReadDOMTree() {} }"), Is.Zero);
        }

        [Test]
        public void Allows_WordThatMerelyContainsAcronymLetters()
        {
            // 'Region' contains 'io' but not as a hump, so it must not be flagged.
            Assert.That(naming_count("class Mock { public void FindRegion() {} }"), Is.Zero);
        }


        [Test]
        public void Ignores_ExternalApiNamesWhenSpelling()
        {
            // Calling an SDK member named LoadUrl is not ours to rename.
            Assert.That(naming_count("class Mock { void run() { view.LoadUrl(site); } }"), Is.Zero);
        }

        [Test]
        public void Ignores_ExternalPropertyNamesWhenSpelling()
        {
            Assert.That(naming_count("class Mock { void run() { settings.DomStorageEnabled = true; } }"), Is.Zero);
        }

        [Test]
        public void Ignores_ExternDeclarations()
        {
            // The name of an imported function is fixed by the platform. It cannot be
            // renamed, so holding it to our casing would only force it to be silenced.
            var code = "class Mock { static extern int DwmSetWindowAttribute(int window); }";
            Assert.That(naming_count(code), Is.Zero);
        }

        [Test]
        public void Catches_AbbreviationInDeclaredTypeName()
        {
            Assert.That(caught("class MsgBox { }", "unknown word part 'Msg'"), Is.True);
        }

        // ---- order -----------------------------------------------------------

        [Test]
        public void Catches_MethodBeforeField()
        {
            var code = "class Mock { public void Run() {} int _count; }";
            Assert.That(order_count(code), Is.GreaterThan(0));
        }

        [Test]
        public void Catches_PublicMethodAfterPrivateMethod()
        {
            var code = "class Mock { void helper() {} public void Run() {} }";
            Assert.That(order_count(code), Is.GreaterThan(0));
        }

        [Test]
        public void Catches_InstanceFieldBeforeConst()
        {
            var code = "class Mock { int _count; const int MAX = 1; }";
            Assert.That(order_count(code), Is.GreaterThan(0));
        }

        [Test]
        public void Passes_CleanOrder()
        {
            var code = @"
    class Watcher
    {
        const int MAX_TABS = 8;
        int _tab_count;
        static int _shared_count;

        public Watcher() { }

        public int TabCount { get; }

        public void Start() { }

        void reset() { }
    }";
            Assert.That(order_count(code), Is.Zero,
                string.Join("\n  ", ConventionRules.find_order_violations(code, "mock.cs")));
        }

        [Test]
        public void Ignores_InterfaceDeclarations()
        {
            Assert.That(order_count("interface I { void Run(); int Count { get; } }"), Is.Zero);
        }

        // ---- letter words: snake keeps lower, Pascal wants all caps ----------

        [Test]
        public void Allows_LowerCaseLetterWordInSnakeName()
        {
            // A snake_case name is all lower case, so 'id' in 'item_id' is fine.
            Assert.That(naming_count("class Mock { void run(int item_id) {} }"), Is.Zero);
        }

        [Test]
        public void Catches_LetterWordOnlyCapitalized()
        {
            // 'Id' in a PascalCase name must be the all-caps print form 'ID'.
            Assert.That(caught("class Mock { public int NodeId() => 0; }",
                "'NodeId' has the letter word 'Id', use 'ID'"), Is.True);
        }

        [Test]
        public void Allows_LetterWordAllCaps()
        {
            // 'ID' is already the print form, so it passes.
            Assert.That(naming_count("class Mock { public int NodeID() => 0; }"), Is.Zero);
        }

        [Test]
        public void Allows_NormalWordStartingUpper()
        {
            // 'Node' is a plain word, not a letter word, so PascalCase is fine.
            Assert.That(naming_count("class Mock { public int NodeName() => 0; }"), Is.Zero);
        }

        [Test]
        public void Allows_PluralLetterWord()
        {
            // URLs is the plural of the letter word URL: it splits as URL + s,
            // not UR + Ls, so it passes as an all-caps letter word.
            Assert.That(naming_count("class Mock { public int NodeURLs() => 0; }"), Is.Zero);
        }

        // ---- single letters: only the habitual ones pass ---------------------

        [Test]
        public void Allows_HabitualSingleLetter()
        {
            // 'i' is a long-standing loop name, so it passes.
            Assert.That(naming_count("class Mock { void run() { for (var i = 0; i < 3; i++) {} } }"), Is.Zero);
        }

        [Test]
        public void Catches_UnlistedSingleLetter()
        {
            // 'g' is not in the habitual set, so a one-letter 'g' is too short.
            Assert.That(caught("class Mock { void run() { for (var g = 0; g < 3; g++) {} } }",
                "the one-letter name 'g'"), Is.True);
        }
    }
}
