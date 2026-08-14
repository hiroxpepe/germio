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
    /// Roslyn gate: calls to our own types (Germio.*) must pass arguments by
    /// name, even a single argument. External APIs (.NET BCL, third-party
    /// libraries, NUnit) are exempt — we don't control their parameter names.
    /// This enforces call-site readability project-wide.
    ///
    /// Exemptions (an unnamed argument is allowed when):
    ///   - the callee is not one of our own symbols (external),
    ///   - the argument is part of a params expansion (variadic tail),
    ///   - the call is an operator / delegate invoke / property indexer,
    ///   - the argument already carries a name.
    /// </summary>
    [TestFixture]
    [Category("Convention")]
    public class NamedArgumentConventionTests
    {
        static bool is_own(ISymbol? s)
        {
            if (s == null) return false;
            var asm = s.ContainingAssembly?.Name;
            return asm == null || asm.StartsWith("Germio") || s.Locations.Any(l => l.IsInSource);
        }

        // does this argument map onto a params parameter (variadic tail)? then it's exempt.
        static bool is_params_arg(IMethodSymbol method, ArgumentSyntax arg, ArgumentListSyntax list)
        {
            if (method.Parameters.Length == 0) return false;
            var last = method.Parameters[^1];
            if (!last.IsParams) return false;
            int idx = list.Arguments.IndexOf(arg);
            return idx >= method.Parameters.Length - 1;
        }

        [Test]
        public void OwnCalls_UseNamedArguments()
        {
            var files = ConventionScan.source_files().ToList();
            var trees = files.ToDictionary(f => f, f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f));
            var refs = Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")
                .Where(d => { try { MetadataReference.CreateFromFile(d); return true; } catch { return false; } })
                .Select(d => (MetadataReference)MetadataReference.CreateFromFile(d));
            var comp = CSharpCompilation.Create("Germio.Scan", trees.Values, refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            var violations = new List<string>();

            foreach (var tree in trees.Values) {
                var model = comp.GetSemanticModel(tree);
                var unit = tree.GetCompilationUnitRoot();
                var label = Path.GetFileName(tree.FilePath);

                foreach (var inv in unit.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
                    if (inv.ArgumentList.Arguments.Count == 0) continue;
                    if (model.GetSymbolInfo(inv).Symbol is not IMethodSymbol sym) continue;
                    if (sym.MethodKind is not (MethodKind.Ordinary or MethodKind.LocalFunction)) continue;
                    if (!is_own(sym)) continue;
                    foreach (var a in inv.ArgumentList.Arguments) {
                        if (a.NameColon != null) continue;
                        if (is_params_arg(sym, a, inv.ArgumentList)) continue;
                        var line = tree.GetLineSpan(a.Span).StartLinePosition.Line + 1;
                        violations.Add($"{label}:{line}: {sym.Name}(...) arg '{a}' must be named");
                    }
                }
                foreach (var oc in unit.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()) {
                    if (oc.ArgumentList == null || oc.ArgumentList.Arguments.Count == 0) continue;
                    if (model.GetSymbolInfo(oc).Symbol is not IMethodSymbol sym) continue;
                    if (!is_own(sym)) continue;
                    foreach (var a in oc.ArgumentList.Arguments) {
                        if (a.NameColon != null) continue;
                        if (is_params_arg(sym, a, oc.ArgumentList)) continue;
                        var line = tree.GetLineSpan(a.Span).StartLinePosition.Line + 1;
                        violations.Add($"{label}:{line}: new {sym.ContainingType.Name}(...) arg '{a}' must be named");
                    }
                }
            }

            Assert.That(violations, Is.Empty,
                $"{violations.Count} unnamed argument(s) in our own calls (showing first 30):\n  "
                + string.Join("\n  ", violations.Take(30)));
        }
    }
}
