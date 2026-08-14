// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Germio.Tests.Convention {
    /// <summary>
    /// Gate: no duplicate using directives in a file (compilation-unit or namespace
    /// scope). A duplicate using is a CS0105 warning and pure noise, so this locks
    /// it out ahead of a build. Complements UsingOrderConventionTests, which checks
    /// order, not duplication.
    /// </summary>
    [TestFixture]
    [Category("Convention")]
    public class DuplicateUsingConventionTests
    {
        static string key(UsingDirectiveSyntax u) =>
            (u.Alias?.ToString() ?? "") + "|" +
            (u.StaticKeyword.IsKind(SyntaxKind.None) ? "" : "static") + "|" +
            (u.Name?.ToString() ?? "");

        [Test]
        public void Sources_HaveNoDuplicateUsings()
        {
            var violations = new List<string>();

            foreach (var path in ConventionScan.source_files()) {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
                var unit = tree.GetCompilationUnitRoot();
                var label = Path.GetFileName(path);

                void scan(SyntaxList<UsingDirectiveSyntax> usings)
                {
                    var seen = new HashSet<string>();
                    foreach (var u in usings) {
                        var k = key(u);
                        if (!seen.Add(k)) {
                            var line = tree.GetLineSpan(u.Span).StartLinePosition.Line + 1;
                            violations.Add($"{label}:{line}: duplicate using '{u.Name}'");
                        }
                    }
                }
                scan(unit.Usings);
                foreach (var ns in unit.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
                    scan(ns.Usings);
            }

            violations.Sort(System.StringComparer.Ordinal);
            Assert.That(violations, Is.Empty,
                $"{violations.Count} duplicate using(s) (showing first 40):\n  "
                + string.Join("\n  ", violations.Take(40)));
        }
    }
}
