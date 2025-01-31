// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// Defines the direction.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public enum Direction {
        PositiveZ,
        NegativeZ,
        PositiveX,
        NegativeX,
        None
    };

    #region RenderingMode

    /// <summary>
    /// Defines the render mode of the material.
    /// </summary>
    public enum RenderingMode {
        Opaque,
        Cutout,
        Fade,
        Transparent,
    }

    #endregion
}