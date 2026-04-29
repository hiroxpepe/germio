// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

using Germio.Model;
using Germio.Core;
using ValidationLevel = Germio.Core.ValidationLevel;

namespace Germio.Editor {
    /// <summary>
    /// Germio Dashboard: LLM-Native game progression editor for Unity.
    /// Loads germio_config.json, runs the Validator to surface V001–V012
    /// errors/warnings in LLM-readable format, and copies the Mermaid
    /// transition graph to the clipboard via the Grapher.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Dashboard : EditorWindow {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Scenario?              _current_scenario;
        List<ValidationResult> _validation_results = new();
        Vector2                _scroll_position;
        string                 _status_message  = string.Empty;
        bool                   _is_config_loaded = false;

        static readonly string CONFIG_FILE = "germio_config.json";

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // MenuItem

        /// <summary>
        /// Opens the Germio Dashboard window from the Unity menu bar.
        /// </summary>
        [MenuItem("Germio/Dashboard")]
        public static void Open() {
            var window = GetWindow<Dashboard>("Germio Dashboard");
            window.minSize = new Vector2(400f, 300f);
            window.Show();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // EditorWindow lifecycle

        void OnGUI() {
            drawHeader();
            drawLoadButton();

            if (!_is_config_loaded) { return; }

            EditorGUILayout.Space(4);
            drawValidationSection();
            EditorGUILayout.Space(4);
            drawGrapherSection();
            EditorGUILayout.Space(4);
            drawExportButton();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// Renders the window title and optional status message.
        /// </summary>
        void drawHeader() {
            EditorGUILayout.LabelField("Germio Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (!string.IsNullOrEmpty(_status_message)) {
                EditorGUILayout.HelpBox(_status_message, MessageType.Info);
            }
        }

        /// <summary>
        /// Renders the "Load Config" button.
        /// Reads germio_config.json from StreamingAssets and deserializes it into Scenario.
        /// </summary>
        void drawLoadButton() {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Load Config", GUILayout.Height(28))) {
                loadConfig();
            }
        }

        /// <summary>
        /// Renders the Validator results section.
        /// Errors are drawn in red, Warnings in yellow, and a clean result in green.
        /// </summary>
        void drawValidationSection() {
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);

            float scroll_height = Mathf.Min(200f, 24f * Mathf.Max(1, _validation_results.Count));
            _scroll_position = EditorGUILayout.BeginScrollView(_scroll_position,
                GUILayout.Height(scroll_height));

            if (_validation_results.Count == 0) {
                var clean_style = new GUIStyle(EditorStyles.label);
                clean_style.normal.textColor = new Color(0.0f, 0.65f, 0.2f);
                EditorGUILayout.LabelField("✓ No errors found.", clean_style);
            }
            else {
                foreach (var result in _validation_results) {
                    var entry_style = new GUIStyle(EditorStyles.label);
                    entry_style.wordWrap = true;
                    entry_style.normal.textColor = result.severity == ValidationLevel.Error
                        ? new Color(0.9f, 0.2f, 0.2f)   // red
                        : new Color(0.9f, 0.75f, 0.0f);  // yellow
                    EditorGUILayout.LabelField(result.ToString(), entry_style);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Renders the "Copy Mermaid Graph" button.
        /// Exports the Mermaid LR flowchart string and copies it to the system clipboard.
        /// </summary>
        void drawGrapherSection() {
            EditorGUILayout.LabelField("Mermaid Graph", EditorStyles.boldLabel);
            if (GUILayout.Button("Copy Mermaid Graph", GUILayout.Height(28))) {
                copyMermaidGraph();
            }
        }

        /// <summary>
        /// Reads germio_config.json from StreamingAssets, parses it into Scenario,
        /// then runs the Validator and caches the results.
        /// </summary>
        void loadConfig() {
            string config_path = Path.Combine(Application.streamingAssetsPath, CONFIG_FILE);
            if (!File.Exists(config_path)) {
                _status_message  = $"Config not found: {config_path}";
                _is_config_loaded = false;
                Debug.LogWarning($"[Germio Dashboard] {_status_message}");
                Repaint();
                return;
            }

            try {
                string json = File.ReadAllText(config_path);
                _current_scenario = JsonConvert.DeserializeObject<Scenario>(json)
                    ?? throw new InvalidOperationException("Deserialized Scenario is null.");

                _validation_results = Validator.Validate(scenario: _current_scenario);
                _is_config_loaded   = true;
                _status_message     = $"Loaded: {config_path}";
                _scroll_position    = Vector2.zero;

                Debug.Log($"[Germio Dashboard] Config loaded. " +
                    $"{_validation_results.Count} finding(s).");
            }
            catch (Exception ex) {
                _status_message  = $"Load failed: {ex.Message}";
                _is_config_loaded = false;
                Debug.LogError($"[Germio Dashboard] {_status_message}");
            }

            Repaint();
        }

        /// <summary>
        /// Exports the Mermaid flowchart for the current Scenario and copies it to clipboard.
        /// </summary>
        void copyMermaidGraph() {
            if (_current_scenario == null) { return; }

            string mermaid_text = Grapher.Export(scenario: _current_scenario);
            EditorGUIUtility.systemCopyBuffer = mermaid_text;
            Debug.Log("[Germio Dashboard] Mermaid graph copied to clipboard.");
        }

        /// <summary>
        /// Renders the "Export Encrypted Config (.dat)" button.
        /// </summary>
        void drawExportButton() {
            EditorGUILayout.LabelField("Encrypted Export", EditorStyles.boldLabel);
            if (GUILayout.Button("Export Encrypted Config (.dat)", GUILayout.Height(28))) {
                _ = export_encrypted_config_async();
            }
        }

        /// <summary>
        /// Saves the current Scenario as an AES-encrypted .dat file to StreamingAssets.
        /// Uses Vault for key material; shows an error status if Vault has no key configured.
        /// </summary>
        async System.Threading.Tasks.Task export_encrypted_config_async() {
            if (_current_scenario == null) { return; }

            try {
                await Storage.SaveAsync(data: _current_scenario, encrypt: true, base_path: Application.streamingAssetsPath);
                _status_message = "germio_config.dat exported.";
                Debug.Log("[Germio Dashboard] germio_config.dat exported.");
            }
            catch (Exception ex) {
                _status_message = $"Export failed: {ex.Message}";
                Debug.LogError($"[Germio Dashboard] {_status_message}");
            }

            Repaint();
        }
    }
}
#endif
