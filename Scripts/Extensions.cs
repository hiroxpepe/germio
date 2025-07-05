// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Collections.Generic;
using UnityEngine;

namespace Germio {
    /// <summary>
    /// Provides generic extension methods.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Extensions {
#nullable enable

        #region Type of Object

        /// <summary>
        /// Determines if the GameObject's name contains the specified string.
        /// </summary>
        public static bool Like(this GameObject self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// Determines if the Transform's name contains the specified string.
        /// </summary>
        public static bool Like(this Transform self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// Determines if the Collider's name contains the specified string.
        /// </summary>
        public static bool Like(this Collider self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// Determines if the Collision's GameObject name contains the specified string.
        /// </summary>
        public static bool Like(this Collision self, string type) {
            return self.gameObject.name.Contains(value: type);
        }

        #endregion

        #region get the component.

        /// <summary>
        /// Retrieves all components of type T from the GameObject's children.
        /// </summary>
        public static IEnumerable<T> GetInChildren<T>(this GameObject self) {
            return self.GetComponentsInChildren<T>();
        }

        /// <summary>
        /// Retrieves the component of type T from the GameObject.
        /// </summary>
        public static T Get<T>(this GameObject self) {
            return self.GetComponent<T>();
        }

        /// <summary>
        /// Retrieves the component of type T from the Transform.
        /// </summary>
        public static T Get<T>(this Transform self) {
            return self.GetComponent<T>();
        }

        /// <summary>
        /// Adds a component of type T to the GameObject.
        /// </summary>
        public static T Add<T>(this GameObject self) where T : Component {
            return self.AddComponent<T>();
        }

        /// <summary>
        /// Adds a component of type T to the Transform's GameObject.
        /// </summary>
        public static T Add<T>(this Transform self) where T : Component {
            return self.gameObject.AddComponent<T>();
        }

        #endregion

        #region for Material.

        /// <summary>
        /// Sets the Material's color to opaque.
        /// </summary>
        public static Material ToOpaque(this Material self, float time = 0) {
            Color color = self.color;
            color.a = 1.0f; // Set to opaque.
            self.color = color;
            return self;
        }

        /// <summary>
        /// Sets the Material's color to transparent.
        /// </summary>
        public static Material ToTransparent(this Material self, float time = 0) {
            Color color = self.color;
            color.a = 0.5f; // Set to transparent.
            self.color = color;
            return self;
        }

        #endregion
    }
}