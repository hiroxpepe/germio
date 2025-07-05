// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// Specifies the direction.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public enum Direction {
#nullable enable

        /// <summary>
        /// Represents the positive Z-axis direction.
        /// </summary>
        PositiveZ,

        /// <summary>
        /// Represents the negative Z-axis direction.
        /// </summary>
        NegativeZ,

        /// <summary>
        /// Represents the positive X-axis direction.
        /// </summary>
        PositiveX,

        /// <summary>
        /// Represents the negative X-axis direction.
        /// </summary>
        NegativeX,

        /// <summary>
        /// Represents no direction specified.
        /// </summary>
        None
    };

    #region RenderingMode

    /// <summary>
    /// Specifies the rendering mode of the material.
    /// </summary>
    public enum RenderingMode {
        /// <summary>
        /// Represents the opaque rendering mode.
        /// </summary>
        Opaque,

        /// <summary>
        /// Represents the cutout rendering mode.
        /// </summary>
        Cutout,

        /// <summary>
        /// Represents the fade rendering mode.
        /// </summary>
        Fade,

        /// <summary>
        /// Represents the transparent rendering mode.
        /// </summary>
        Transparent,
    }

    #endregion
}