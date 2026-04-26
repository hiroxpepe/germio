// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

#if UNITY_5_3_OR_NEWER
using UnityEngine;
using static UnityEngine.GameObject;

using static Germio.Env;

namespace Germio {
    /// <summary>
    /// Attaches to a trigger collider in the scene.
    /// When the player enters or exits, delegates to <see cref="UniversalTriggerSystem"/>.
    /// This component knows nothing about game logic — it only reports a trigger ID.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class VolumeTrigger : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Inspector Fields

        /// <summary>
        /// The trigger identifier reported to UniversalTriggerSystem, e.g. "vol_goal".
        /// </summary>
        [SerializeField] string _trigger_id = string.Empty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        UniversalTriggerSystem? _uts;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Unity lifecycle

        // Start is called before the first frame update (after all Awake calls).
        void Start() {
            /// <summary>
            /// Retrieves the UniversalTriggerSystem reference from GameSystem.
            /// Using Start (not Awake) ensures GameSystem.Awake() has already initialised UTS.
            /// </summary>
            _uts = Find(name: GAME_SYSTEM).Get<GameSystem>().universalTriggerSystem;
        }

        void OnTriggerEnter(Collider other) {
            /// <summary>
            /// Notifies the hub when the player enters this volume.
            /// </summary>
            if (other.gameObject.CompareTag(tag: PLAYER_TYPE)) {
                _uts?.OnAreaEnter(_trigger_id);
            }
        }

        void OnTriggerExit(Collider other) {
            /// <summary>
            /// Clears the G2 guard when the player exits this volume.
            /// </summary>
            if (other.gameObject.CompareTag(tag: PLAYER_TYPE)) {
                _uts?.OnAreaExit(_trigger_id);
            }
        }
    }
}
#endif
