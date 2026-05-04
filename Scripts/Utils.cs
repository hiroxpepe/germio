// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using System.Collections.Generic;

namespace Germio {
    /// <summary>
    /// A concise alias for Dictionary to reduce verbosity in code.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Map<K, V> : Dictionary<K, V> {
    }

    /// <summary>
    /// Represents event arguments with a name and optional value.
    /// </summary>
    public class EvtArgs : EventArgs {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EvtArgs"/> class with the specified event name.
        /// </summary>
        /// <param name="name">Name of the event.</param>
        public EvtArgs(string name) {
            Name = name;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, noun phrase, adjective]

        /// <summary>
        /// Gets the name of the event.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the optional value of the event.
        /// </summary>
        public string? Value { get; set; }
    }

    /// <summary>
    /// Represents a delegate for handling changed events.
    /// </summary>
    public delegate void Changed(object sender, EvtArgs e);
}
