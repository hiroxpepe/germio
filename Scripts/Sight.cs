// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace Germio {
    /// <summary>
    /// Holds one character's own sight — how far, and how wide, it sees.
    /// A plain data holder alone: it turns no thing toward any other, and
    /// calls no ray, no sphere, itself. Modio's own Runtime reads these
    /// five values through a thin edge of its own (Modio's TASK-026),
    /// never this type directly.
    ///
    /// See TASKLIST.md TASK-069, and modio's own docs/sight_checklist.md
    /// §4.7, for the full turn that put this here, not in Modio.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Sight : MonoBehaviour {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Inspector Fields

        /// <summary>How far this character sees, in meters. Held true by this Inspector field alone.</summary>
        [SerializeField, Range(0.1f, 100f)] float _reach = 30f;

        /// <summary>Half the width of the sight wedge, side to side, in degrees.</summary>
        [SerializeField, Range(1f, 180f)] float _half_yaw = 90f;

        /// <summary>Half the height of the sight wedge, up and down, in degrees.</summary>
        [SerializeField, Range(1f, 180f)] float _half_pitch = 45f;

        /// <summary>How far up from the body's own root the eyes sit, in meters.</summary>
        [SerializeField] float _eye_height = 1.6f;

        /// <summary>
        /// Where the eyes themselves stand, and which way they face. Left
        /// empty, the body's own transform stands in for it.
        /// </summary>
        [SerializeField] Transform? _eyes = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Properties [noun, adjective]

        public float Reach => _reach;

        public float HalfYaw => _half_yaw;

        public float HalfPitch => _half_pitch;

        public float EyeHeight => _eye_height;

        /// <summary>The eyes' own transform — this body's own, where none is given.</summary>
        public Transform Eyes => _eyes != null ? _eyes : transform;
    }
}
#endif
