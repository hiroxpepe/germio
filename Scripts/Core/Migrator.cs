// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using Newtonsoft.Json.Linq;

namespace Germio.Core
{
    /// <summary>
    /// Upgrades older JSON config files to the current schema version.
    /// V0 → V1: renames legacy property keys and stamps schema_version = 1.
    /// Idempotent: already-current documents pass through unchanged.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Migrator
    {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        /// <summary>The schema version produced by the current build.</summary>
        public const int CURRENT_VERSION = 1;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods

        /// <summary>
        /// Applies all pending migrations to <paramref name="raw"/> and returns the updated object.
        /// The input object may be mutated in-place.
        /// </summary>
        /// <param name="raw">Parsed JObject from a saved config file.</param>
        /// <returns>The same object after all applicable migrations have been applied.</returns>
        public static JObject Migrate(JObject raw)
        {
            int version = raw["schema_version"]?.Value<int>() ?? 0;

            if (version < 1)
            {
                raw = migrateV0ToV1(raw: raw);
            }

            return raw;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods

        /// <summary>
        /// V0 → V1 migration:
        ///   state.firedEvents  → state.fired_rules
        ///   level.events       → level.rules
        ///   schema_version     = 1 (stamped)
        /// </summary>
        static JObject migrateV0ToV1(JObject raw)
        {
            // Rename state.firedEvents → state.fired_rules
            var state = raw["state"] as JObject;
            if (state != null)
            {
                var fired_events = state["firedEvents"];
                if (fired_events != null)
                {
                    state["fired_rules"] = fired_events;
                    state.Remove("firedEvents");
                }
            }

            // Rename level.events → level.rules in every world/level
            // Also rename update_inventory.id → update_inventory.key
            var worlds = raw["worlds"] as JArray;
            if (worlds != null)
            {
                foreach (var world in worlds)
                {
                    var levels = world["levels"] as JArray;
                    if (levels == null) continue;
                    foreach (var level in levels)
                    {
                        var level_obj = level as JObject;
                        if (level_obj == null) continue;
                        var events = level_obj["events"];
                        if (events != null)
                        {
                            level_obj["rules"] = events;
                            level_obj.Remove("events");
                        }
                        // Rename update_inventory.id → update_inventory.key in rule commands
                        var rules = level_obj["rules"] as JArray;
                        if (rules == null) continue;
                        foreach (var rule in rules)
                        {
                            var cmd = rule["command"] as JObject;
                            if (cmd == null) continue;
                            var ui = cmd["update_inventory"] as JObject;
                            if (ui == null) continue;
                            var old_id = ui["id"];
                            if (old_id != null)
                            {
                                ui["key"] = old_id;
                                ui.Remove("id");
                            }
                        }
                    }
                }
            }

            raw["schema_version"] = CURRENT_VERSION;
            return raw;
        }
    }
}
