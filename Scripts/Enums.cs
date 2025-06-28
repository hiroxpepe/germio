// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// Specifies the direction.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public enum Direction {
        /// <summary>
        /// Positive Z-axis direction.
        /// </summary>
        PositiveZ,

        /// <summary>
        /// Negative Z-axis direction.
        /// </summary>
        NegativeZ,

        /// <summary>
        /// Positive X-axis direction.
        /// </summary>
        PositiveX,

        /// <summary>
        /// Negative X-axis direction.
        /// </summary>
        NegativeX,

        /// <summary>
        /// No direction specified.
        /// </summary>
        None
    };

    #region RenderingMode

    /// <summary>
    /// Specifies the rendering mode of the material.
    /// </summary>
    public enum RenderingMode {
        /// <summary>
        /// Opaque rendering mode.
        /// </summary>
        Opaque,

        /// <summary>
        /// Cutout rendering mode.
        /// </summary>
        Cutout,

        /// <summary>
        /// Fade rendering mode.
        /// </summary>
        Fade,

        /// <summary>
        /// Transparent rendering mode.
        /// </summary>
        Transparent,
    }

    #endregion
}