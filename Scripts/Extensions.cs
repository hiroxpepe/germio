// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Collections.Generic;
using UnityEngine;

namespace Germio {
    /// <summary>
    /// generic extension method
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Extensions {
#nullable enable

        #region type of object.

        /// <summary>
        /// whether the GameObject's name contains the argument string.
        /// </summary>
        public static bool Like(this GameObject self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// whether the Transform's name contains the argument string.
        /// </summary>
        public static bool Like(this Transform self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// whether the Collider's name contains the argument string.
        /// </summary>
        public static bool Like(this Collider self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// whether the Collision's name contains the argument string.
        /// </summary>
        public static bool Like(this Collision self, string type) {
            return self.gameObject.name.Contains(value: type);
        }

        #endregion

        #region get the component.

        /// <summary>
        /// gets T component from children.
        /// </summary>
        public static IEnumerable<T> GetInChildren<T>(this GameObject self) {
            return self.GetComponentsInChildren<T>();
        }

        /// <summary>
        /// gets T component.
        /// </summary>
        public static T Get<T>(this GameObject self) {
            return self.GetComponent<T>();
        }

        /// <summary>
        /// gets T component.
        /// </summary>
        public static T Get<T>(this Transform self) {
            return self.GetComponent<T>();
        }

        /// <summary>
        /// adds T component.
        /// </summary>
        public static T Add<T>(this GameObject self) where T : Component {
            return self.AddComponent<T>();
        }

        /// <summary>
        /// adds T component.
        /// </summary>
        public static T Add<T>(this Transform self) where T : Component {
            return self.gameObject.AddComponent<T>();
        }

        #endregion

        #region for Material.

        /// <summary>
        /// sets Material color to opaque.
        /// </summary>
        public static Material ToOpaque(this Material self, float time = 0) {
            Color color = self.color;
            color.a = 1.0f; // to opaque.
            self.color = color;
            return self;
        }

        /// <summary>
        /// sets Material color to transparent.
        /// </summary>
        public static Material ToTransparent(this Material self, float time = 0) {
            Color color = self.color;
            color.a = 0.5f; // to transparent.
            self.color = color;
            return self;
        }

        #endregion
    }
}