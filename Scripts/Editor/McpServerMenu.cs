// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Germio.Editor {

    /// <author>h.adachi (STUDIO MeowToon)</author>
    /// <summary>
    /// Unity Editor menu: Tools > Germio > MCP Server.
    /// Design stub for P5-T8 — full implementation targeted at Phase 7.
    /// Provides a launcher/stopper for the Germio MCP server console app,
    /// enabling LLM clients (Claude Desktop, Continue.dev) to call germio_validate,
    /// germio_export_mermaid, germio_get_schema, etc. via JSON-RPC over stdio.
    /// See docs/mcp_design.md for the full 6-tool specification.
    /// </summary>
    public static class McpServerMenu {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const string MENU_START = "Tools/Germio/MCP Server/Start MCP Server";
        const string MENU_STOP  = "Tools/Germio/MCP Server/Stop MCP Server";

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Starts the Germio MCP server process.
        /// TODO (Phase 7): build and launch the MCP console app via dotnet publish + Process.Start.
        /// </summary>
        [MenuItem(MENU_START)]
        public static void StartMcpServer() {
            // Phase 7 implementation will:
            //   1. Run `dotnet publish` on the MCP server project
            //   2. Launch the resulting binary as a child Process
            //   3. Print the stdio endpoint to the Unity Console
            UnityEngine.Debug.Log("[Germio] MCP Server: Start is not yet implemented (Phase 7). See docs/mcp_design.md.");
        }

        /// <summary>
        /// Stops the running Germio MCP server process.
        /// TODO (Phase 7): track and kill the child Process started by StartMcpServer.
        /// </summary>
        [MenuItem(MENU_STOP)]
        public static void StopMcpServer() {
            UnityEngine.Debug.Log("[Germio] MCP Server: Stop is not yet implemented (Phase 7). See docs/mcp_design.md.");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // MenuItem validation

        [MenuItem(MENU_START, validate = true)]
        static bool ValidateStart() => true;

        [MenuItem(MENU_STOP, validate = true)]
        static bool ValidateStop() => true;
    }
}
#endif