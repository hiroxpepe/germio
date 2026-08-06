// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Germio.Tests.Convention;

/// <summary>
/// Applies the naming rules to the real sources. The rules live in ConventionRules
/// and are verified against mock code in ConventionRulesTests.
/// </summary>
[TestFixture]
[Category("Convention")]
public class NamingConventionTests
{
    [Test]
    public void Scan_FindsSourceFiles()
    {
        // Guards against a vacuous pass: if the roots resolve to nothing, the
        // convention tests would go green without inspecting anything.
        Assert.That(ConventionScan.source_files().Any(), Is.True,
            "no sources were scanned; ConventionScan.TARGET_DIRS does not resolve");
    }

    [Test]
    public void Sources_FollowNamingConventions()
    {
        var found = new List<string>();
        foreach (var path in ConventionScan.source_files())
            found.AddRange(ConventionRules.find_naming_violations(
                File.ReadAllText(path), Path.GetFileName(path)));

        found.Sort(StringComparer.Ordinal);
        Assert.That(found, Is.Empty,
            $"{found.Count} naming violation(s) (showing first 40):\n  "
            + string.Join("\n  ", found.Take(40)));
    }

    [Test]
    public void FileNames_FollowPrintForm()
    {
        var found = new List<string>();
        foreach (var path in ConventionScan.source_files())
            found.AddRange(ConventionRules.find_filename_violations(Path.GetFileName(path)));

        found.Sort(StringComparer.Ordinal);
        Assert.That(found, Is.Empty,
            $"{found.Count} file-name violation(s) (showing first 40):\n  "
            + string.Join("\n  ", found.Take(40)));
    }
}
