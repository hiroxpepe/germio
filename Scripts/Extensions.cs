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

        #region Type of Object

        /// Determines whether the GameObject's name contains the specified string.
        /// </summary>
        /// <param name="self">Target GameObject instance.</param>
        /// <param name="type">String to search for in the GameObject's name.</param>
        /// <returns>True if the name contains the specified string; otherwise, false.</returns>
        public static bool Like(this GameObject self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// Determines whether the Transform's name contains the specified string.
        /// </summary>
        /// <param name="self">Target Transform instance.</param>
        /// <param name="type">String to search for in the Transform's name.</param>
        /// <returns>True if the name contains the specified string; otherwise, false.</returns>
        public static bool Like(this Transform self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// Determines whether the Collider's name contains the specified string.
        /// </summary>
        /// <param name="self">Target Collider instance.</param>
        /// <param name="type">String to search for in the Collider's name.</param>
        /// <returns>True if the name contains the specified string; otherwise, false.</returns>
        public static bool Like(this Collider self, string type) {
            return self.name.Contains(value: type);
        }

        /// <summary>
        /// Determines whether the Collision's GameObject name contains the specified string.
        /// </summary>
        /// <param name="self">Target Collision instance.</param>
        /// <param name="type">String to search for in the GameObject's name.</param>
        /// <returns>True if the GameObject's name contains the specified string; otherwise, false.</returns>
        public static bool Like(this Collision self, string type) {
            return self.gameObject.name.Contains(value: type);
        }

        #endregion

        #region get the component.

        /// <summary>
        /// Gets all components of type T from the GameObject's children.
        /// </summary>
        /// <typeparam name="T">Type of component to retrieve.</typeparam>
        /// <param name="self">Target GameObject instance.</param>
        /// <returns>Enumerable collection of components of type T found in the children.</returns>
        public static IEnumerable<T> GetInChildren<T>(this GameObject self) {
            return self.GetComponentsInChildren<T>();
        }

        /// <summary>
        /// Gets the component of type T from the GameObject.
        /// </summary>
        /// <typeparam name="T">Type of component to retrieve.</typeparam>
        /// <param name="self">Target GameObject instance.</param>
        /// <returns>Component of type T if found; otherwise, null.</returns>
        public static T Get<T>(this GameObject self) {
            return self.GetComponent<T>();
        }

        /// <summary>
        /// Gets the component of type T from the Transform.
        /// </summary>
        /// <typeparam name="T">Type of component to retrieve.</typeparam>
        /// <param name="self">Target Transform instance.</param>
        /// <returns>Component of type T if found; otherwise, null.</returns>
        public static T Get<T>(this Transform self) {
            return self.GetComponent<T>();
        }

        /// <summary>
        /// Adds a component of type T to the GameObject.
        /// </summary>
        /// <typeparam name="T">Type of component to add.</typeparam>
        /// <param name="self">Target GameObject instance.</param>
        /// <returns>Newly added component of type T.</returns>
        public static T Add<T>(this GameObject self) where T : Component {
            return self.AddComponent<T>();
        }

        /// <summary>
        /// Adds a component of type T to the Transform's GameObject.
        /// </summary>
        /// <typeparam name="T">Type of component to add.</typeparam>
        /// <param name="self">Target Transform instance.</param>
        /// <returns>Newly added component of type T.</returns>
        public static T Add<T>(this Transform self) where T : Component {
            return self.gameObject.AddComponent<T>();
        }

        #endregion

        #region for Material.

        /// <summary>
        /// Sets the Material's color to fully opaque.
        /// </summary>
        /// <param name="self">Target Material instance.</param>
        /// <param name="time">Unused parameter for transition time.</param>
        /// <returns>Material with alpha set to 1.0 (opaque).</returns>
        public static Material ToOpaque(this Material self, float time = 0) {
            Color color = self.color;
            color.a = 1.0f; // Sets to opaque.
            self.color = color;
            return self;
        }

        /// <summary>
        /// Sets the Material's color to semi-transparent.
        /// </summary>
        /// <param name="self">Target Material instance.</param>
        /// <param name="time">Unused parameter for transition time.</param>
        /// <returns>Material with alpha set to 0.5 (semi-transparent).</returns>
        public static Material ToTransparent(this Material self, float time = 0) {
            Color color = self.color;
            color.a = 0.5f; // Sets to transparent.
            self.color = color;
            return self;
        }

        #endregion
    }
}