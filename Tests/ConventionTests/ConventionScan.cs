// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable
using System.Collections.Generic;
using System.IO;

namespace Germio.Tests.Convention;

/// <summary>
/// Locates the sources to police. This is the only file that knows about paths;
/// the rules in ConventionRules are pure and path free.
///
/// PORTING: change TARGET_DIRS and this works in any C# repo.
/// </summary>
static class ConventionScan
{
    internal static readonly string[] TARGET_DIRS = {
        "Scripts",
    };

    // Resolve a directory by walking up from the test binary location.
    internal static string find_dir(string relative_path)
    {
        var dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 10; i++) {
            var candidate = Path.GetFullPath(Path.Combine(dir, relative_path));
            if (Directory.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return "";
    }

    // All .cs files under the target roots, excluding build output.
    internal static IEnumerable<string> source_files()
    {
        foreach (var rel in TARGET_DIRS) {
            var root = find_dir(rel);
            if (root.Length == 0) continue;
            foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)) {
                var norm = path.Replace(Path.DirectorySeparatorChar, '/');
                if (norm.Contains("/bin/") || norm.Contains("/obj/")) continue;
                yield return path;
            }
        }
    }
}
