// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// A Human controller
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Human : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region Inner Classes

        /// <summary>
        /// The Class for the Update() method.
        /// </summary>
        protected class DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            bool _grounded, _climbing, _pushing, _holding, _faceing, _virtualControllerMode;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool grounded { get => _grounded; set => _grounded = value; }

            public bool climbing { get => _climbing; set => _climbing = value; }

            public bool pushing { get => _pushing; set => _pushing = value; }

            public bool holding { get => _holding; set => _holding = value; }

            public bool faceing { get => _faceing; set => _faceing = value; }

            public bool virtualControllerMode { get => _virtualControllerMode; set => _virtualControllerMode = value; }

            //public bool readyOnRun { 
            //    get {
            //        return !_look ? true : false;
            //    }
            //}

            public bool readyForAnyGround { 
                get {
                    return !_look && !_climbing && !_pushing && !_faceing ? true : false;
                }
            }

            public bool ready { 
                get {
                    return !_look && _grounded && !_climbing && !_pushing && !_faceing ? true : false;
                }
            }

            public bool readyWithoutHold { 
                get {
                    return !_look && _grounded && !_climbing && !_pushing && !_holding && !_faceing ? true : false;
                }
            }

            public bool readyWithHold { 
                get {
                    return !_look && !_climbing && !_pushing && _holding && !_faceing ? true : false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// Returns an initialized instance.
            /// </summary>
            public static DoUpdate GetInstance() {
                DoUpdate instance = new();
                instance.ResetState();
                return instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Public Methods [verb]

            public void ResetState() {
                _grounded = _climbing = _pushing = _holding = _faceing = _virtualControllerMode = false;
            }
        }

        #endregion
    }
}