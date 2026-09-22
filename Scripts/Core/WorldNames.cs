// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Germio.Core {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Holds a kind and an id string for every true thing in the world,
    /// keyed by its own GetInstanceID() — read once, at scene load, never
    /// on a tick. The same true shape as Modio.Core.INameSource
    /// (NameOf(int) returns a Kind and an ID), held here on its own type,
    /// with no line naming Modio at all.
    ///
    /// See TASK-067 in TASKLIST.md.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public sealed class WorldNames {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Dictionary<int, (string Kind, string ID)> _table = new();

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Builds the whole table, once, from what a real scene load found.
        /// A repeated instance_id (one GameObject, more than one Collider)
        /// never throws — the last given kind for that id holds.
        /// </summary>
        public void Fill(IReadOnlyList<(int InstanceId, string Kind)> found) {
            var table = new Dictionary<int, (string Kind, string ID)>(found.Count);
            for (int i = 0; i < found.Count; i++) {
                (int instance_id, string kind) = found[i];
                table[instance_id] = (kind, $"g_{instance_id}");
            }
            _table = table;
        }

        /// <summary>Reads a kind and an id string back. ("", "") where the id is not held.</summary>
        public (string Kind, string ID) NameOf(int instance_id) {
            return _table.TryGetValue(instance_id, out var found) ? found : ("", "");
        }
    }
}
