// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

namespace Germio {
    /// <summary>
    /// Provides constants for the game environment.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Env {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        /// <summary>
        /// Frames per second.
        /// </summary>
        public const int FPS = 120; // 30fps

        /// <summary>
        /// Scene names.
        /// </summary>
        public const string SCENE_TITLE = "Title";
        public const string SCENE_SELECT = "Select";
        public const string SCENE_LEVEL_1 = "Level 1";
        public const string SCENE_LEVEL_2 = "Level 2";
        public const string SCENE_LEVEL_3 = "Level 3";
        public const string SCENE_ENDING = "Ending";

        /// <summary>
        /// Game modes.
        /// </summary>
        public const string MODE_EASY = "easy";
        public const string MODE_NORMAL = "normal";
        public const string MODE_HARD = "hard";

        /// <summary>
        /// System names.
        /// </summary>
        public const string CAMERA_SYSTEM = "CameraSystem";
        public const string GAME_SYSTEM = "GameSystem";
        public const string NOTICE_SYSTEM = "NoticeSystem";
        public const string SOUND_SYSTEM = "SoundSystem";

        /// <summary>
        /// Object types.
        /// </summary>
        public const string BLOCK_TYPE = "Block";
        public const string GROUND_TYPE = "Ground";
        public const string WALL_TYPE = "Wall";
        public const string ITEM_TYPE = "Item";
        public const string COIN_TYPE = "Coin";
        public const string BALLOON_TYPE = "Balloon";

        /// <summary>
        /// Name of target objects holder.
        /// </summary>
        public const string TARGETS_OBJECT = "Balloons";

        /// <summary>
        /// Player-related types.
        /// </summary>
        public const string PLAYER_TYPE = "Human";
        public const string VEHICLE_TYPE = "Vehicle";
        public const string HOME_TYPE = "Home";
        public const string LEVEL_TYPE = "Level";
        public const string DESPAWN_TYPE = "Despawn";

        /// <summary>
        /// Game messages.
        /// </summary>
        public const string MESSAGE_LEVEL_CLEAR = "Level Clear!";
        public const string MESSAGE_LEVEL_START = "Start!";
        public const string MESSAGE_GAME_OVER = "Game Over!";
        public const string MESSAGE_GAME_PAUSE = "Pause";
        public const string MESSAGE_STALL = "Stall"; // For a vehicle.

        /// <summary>
        /// Color codes.
        /// </summary>
        /// <remarks>
        /// https://www.color-sample.com/colorschemes/rule/dominant/
        /// </remarks>
        public const string COLOR_RED = "#FF0000";
        public const string COLOR_ORANGE = "#FF7F00";
        public const string COLOR_YELLOW = "#FFFF00";
        public const string COLOR_LIME = "#7FFF00";
        public const string COLOR_GREEN = "#00FF00";
        public const string COLOR_CYAN = "#00FFFF";
        public const string COLOR_AZURE = "#007FFF";
        public const string COLOR_BLUE = "#002AFF";
        public const string COLOR_PURPLE = "#D400FF";
        public const string COLOR_MAGENTA = "#FF007F";
        public const string COLOR_WHITE = "#FFFFFF";
    }
}