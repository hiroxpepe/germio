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
/// Applies the member-order rule to the real sources. The rule lives in
/// ConventionRules and is verified against mock code in ConventionRulesTests.
/// </summary>
[TestFixture]
[Category("Convention")]
public class OrderConventionTests
{
    [Test]
    public void Sources_FollowStyleCopOrder()
    {
        var found = new List<string>();
        foreach (var path in ConventionScan.source_files())
            found.AddRange(ConventionRules.find_order_violations(
                File.ReadAllText(path), Path.GetFileName(path)));

        found.Sort(StringComparer.Ordinal);
        Assert.That(found, Is.Empty,
            $"{found.Count} member-order violation(s) (showing first 40):\n  "
            + string.Join("\n  ", found.Take(999)));
    }
}
