// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using static System.Math;
using UnityEngine;

namespace Germio {
    /// <summary>
    /// Provides extension methods for the Human class.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    internal static class Human_Extensions {
#nullable enable

        /// <summary>
        /// Determines whether the player hits the side of the colliding object.
        /// </summary>
        /// <param name="self">The player GameObject.</param>
        /// <param name="target">The target GameObject to check collision with.</param>
        internal static bool isHitSide(this GameObject self, GameObject target) {
            const float ADJUST = 0.1f;
            float target_height = target.Get<Renderer>().bounds.size.y;
            float target_y = target.transform.position.y;
            float target_top = target_height + target_y;
            float position_y = self.transform.position.y;
            if (position_y < (target_top - ADJUST)) {
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Moves the player up when hitting a block.
        /// </summary>
        /// <param name="self">The player GameObject.</param>
        internal static void moveUp(this GameObject self) {
            const float MOVE_VALUE = 12.0f;
            Vector3 new_position = self.transform.position + Vector3.up * MOVE_VALUE * Time.deltaTime;
            self.transform.position = new_position;
        }

        /// <summary>
        /// Moves the player down when hitting a block.
        /// </summary>
        /// <param name="self">The player GameObject.</param>
        internal static void moveDown(this GameObject self) {
            const float MOVE_VALUE = 6.0f;
            Vector3 new_position = self.transform.position - Vector3.up * MOVE_VALUE * Time.deltaTime;
            self.transform.position = new_position;
        }

        /// <summary>
        /// Calculates the distance to the top of the block.
        /// </summary>
        /// <param name="self">The player GameObject.</param>
        /// <param name="target">The target GameObject to measure distance to.</param>
        internal static double getReach(this GameObject self, GameObject target) {
            //Debug.Log($"name: {target.name}");
            float distance_y = self.transform.position.y - target.transform.position.y;
            //Debug.Log($"distance_y: {distance_y}");
            //Debug.Log($"bounds.size.y: {target.Get<Renderer>().bounds.size.y}");
            float size_to_one = 1.0f * target.Get<Renderer>().bounds.size.y;
            //Debug.Log($"size_to_one: {size_to_one}");
            float rate_for_one = distance_y * size_to_one;
            //Debug.Log($"rate_for_one: {rate_for_one}");
            return Round(value: rate_for_one, digits: 2);
        }

        /// <summary>
        /// Moves the player left or right when hitting a block.
        /// </summary>
        /// <param name="self">The player GameObject.</param>
        /// <param name="direction">The player's direction.</param>
        internal static void moveLetfOrRight(this GameObject self, Direction direction) {
            const float MOVE_VALUE = 0.3f;
            Vector3 new_position = self.transform.position;
            float move_amount = MOVE_VALUE * Time.deltaTime;
            // Z-axis positive and negative.
            if (direction == Direction.PositiveZ || direction == Direction.NegativeZ) {
                float move_direction = (self.transform.forward.x < 0f) ? -1.0f : 1.0f;
                new_position += new Vector3(x: move_amount * move_direction, y: 0f, z: 0f);
            }
            // X-axis positive and negative.
            else if (direction == Direction.PositiveX || direction == Direction.NegativeX) {
                float move_direction = (self.transform.forward.z < 0f) ? -1.0f : 1.0f;
                new_position += new Vector3(x: 0f, y: 0f, z: move_amount * move_direction);
            }
            // Moves to a new position.
            self.transform.position = new_position;
        }
    }
}