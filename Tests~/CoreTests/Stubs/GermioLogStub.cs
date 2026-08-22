// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// GermioLog stub for dotnet test.
// The real Germio.GermioLog (Scripts/GermioLog.cs) depends on UnityEngine.Application.dataPath
// and UnityEngine.Debug.Log, which cannot be resolved in a dotnet test environment without the Unity assembly.
// This stub satisfies only the GermioLog.Write(string) signature called by Bus.cs / Store.cs / Scene.cs / GameSystem.cs etc.
// and implements it as a no-op (log output is not needed during tests).
//
// The real GermioLog.cs is not included in CoreTests.csproj Compile Include.
// Including this stub resolves the Germio.GermioLog name referenced by Bus.cs / Store.cs.

namespace Germio {
    public static class GermioLog {
        public static bool enabled = false;
        public static void Write(string message) {
            // No-op in dotnet test environment.
            // Real Germio.GermioLog (with file I/O + Debug.Log) is used at Unity runtime.
        }
    }
}
