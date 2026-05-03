// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

#if UNITY_5_3_OR_NEWER
using UnityEngine;
using static UnityEngine.GameObject;

using static Germio.Env;
using Germio;

namespace Germio.Systems {
    /// <summary>
    /// Attaches to a trigger collider in the scene.
    /// When the player enters or exits, delegates to <see cref="Bus"/>.
    /// This component knows nothing about game logic — it only reports a zone ID.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Zone : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Inspector Fields

        /// <summary>
        /// The zone identifier reported to Bus, e.g. "vol_goal".
        /// </summary>
        [SerializeField] string _zone_id = string.Empty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Bus? _bus;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Unity lifecycle

        // Start is called before the first frame update (after all Awake calls).
        void Start() {
            /// <summary>
            /// Retrieves the Bus reference from GameSystem.
            /// Using Start (not Awake) ensures GameSystem.Awake() has already initialised the bus.
            /// </summary>
            _bus = Find(name: GAME_SYSTEM).Get<GameSystem>().bus;
        }

        void OnTriggerEnter(Collider other) {
            /// <summary>
            /// Notifies the bus when the player enters this volume.
            /// </summary>
            if (other.gameObject.CompareTag(tag: PLAYER_TYPE)) {
                _bus?.OnZoneEnter(_zone_id);
            }
        }

        void OnTriggerExit(Collider other) {
            /// <summary>
            /// Clears the G2 guard when the player exits this volume.
            /// </summary>
            if (other.gameObject.CompareTag(tag: PLAYER_TYPE)) {
                _bus?.OnZoneExit(_zone_id);
            }
        }
    }
}
#endif