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
        /// Gets the frames per second for the game.
        /// </summary>
        public const int FPS = 120; // 30fps

        /// <summary>
        /// Gets the scene name for the title screen.
        /// </summary>
        public const string SCENE_TITLE = "Title";
        /// <summary>
        /// Gets the scene name for the select screen.
        /// </summary>
        public const string SCENE_SELECT = "Select";
        /// <summary>
        /// Gets the scene name for level 1.
        /// </summary>
        public const string SCENE_LEVEL_1 = "Level 1";
        /// <summary>
        /// Gets the scene name for level 2.
        /// </summary>
        public const string SCENE_LEVEL_2 = "Level 2";
        /// <summary>
        /// Gets the scene name for level 3.
        /// </summary>
        public const string SCENE_LEVEL_3 = "Level 3";
        /// <summary>
        /// Gets the scene name for the ending screen.
        /// </summary>
        public const string SCENE_ENDING = "Ending";

        /// <summary>
        /// Gets the string for easy game mode.
        /// </summary>
        public const string MODE_EASY = "easy";
        /// <summary>
        /// Gets the string for normal game mode.
        /// </summary>
        public const string MODE_NORMAL = "normal";
        /// <summary>
        /// Gets the string for hard game mode.
        /// </summary>
        public const string MODE_HARD = "hard";

        /// <summary>
        /// Gets the name of the camera system.
        /// </summary>
        public const string CAMERA_SYSTEM = "CameraSystem";
        /// <summary>
        /// Gets the name of the game system.
        /// </summary>
        public const string GAME_SYSTEM = "GameSystem";
        /// <summary>
        /// Gets the name of the notice system.
        /// </summary>
        public const string NOTICE_SYSTEM = "NoticeSystem";
        /// <summary>
        /// Gets the name of the sound system.
        /// </summary>
        public const string SOUND_SYSTEM = "SoundSystem";

        /// <summary>
        /// Gets the object type for blocks.
        /// </summary>
        public const string BLOCK_TYPE = "Block";
        /// <summary>
        /// Gets the object type for ground.
        /// </summary>
        public const string GROUND_TYPE = "Ground";
        /// <summary>
        /// Gets the object type for walls.
        /// </summary>
        public const string WALL_TYPE = "Wall";
        /// <summary>
        /// Gets the object type for items.
        /// </summary>
        public const string ITEM_TYPE = "Item";
        /// <summary>
        /// Gets the object type for coins.
        /// </summary>
        public const string COIN_TYPE = "Coin";
        /// <summary>
        /// Gets the object type for balloons.
        /// </summary>
        public const string BALLOON_TYPE = "Balloon";

        /// <summary>
        /// Gets the name of the target objects holder.
        /// </summary>
        public const string TARGETS_OBJECT = "Balloons";

        /// <summary>
        /// Gets the player type string.
        /// </summary>
        public const string PLAYER_TYPE = "Human";
        /// <summary>
        /// Gets the vehicle type string.
        /// </summary>
        public const string VEHICLE_TYPE = "Vehicle";
        /// <summary>
        /// Gets the home type string.
        /// </summary>
        public const string HOME_TYPE = "Home";
        /// <summary>
        /// Gets the level type string.
        /// </summary>
        public const string LEVEL_TYPE = "Level";
        /// <summary>
        /// Gets the despawn type string.
        /// </summary>
        public const string DESPAWN_TYPE = "Despawn";

        /// <summary>
        /// Gets the message for level clear.
        /// </summary>
        public const string MESSAGE_LEVEL_CLEAR = "Level Clear!";
        /// <summary>
        /// Gets the message for level start.
        /// </summary>
        public const string MESSAGE_LEVEL_START = "Start!";
        /// <summary>
        /// Gets the message for game over.
        /// </summary>
        public const string MESSAGE_GAME_OVER = "Game Over!";
        /// <summary>
        /// Gets the message for game pause.
        /// </summary>
        public const string MESSAGE_GAME_PAUSE = "Pause";
        /// <summary>
        /// Gets the message for vehicle stall.
        /// </summary>
        public const string MESSAGE_STALL = "Stall"; // For a vehicle.

        /// <summary>
        /// Gets the color code for red.
        /// </summary>
        /// <remarks>
        /// https://www.color-sample.com/colorschemes/rule/dominant/
        /// </remarks>
        public const string COLOR_RED = "#FF0000";
        /// <summary>
        /// Gets the color code for orange.
        /// </summary>
        public const string COLOR_ORANGE = "#FF7F00";
        /// <summary>
        /// Gets the color code for yellow.
        /// </summary>
        public const string COLOR_YELLOW = "#FFFF00";
        /// <summary>
        /// Gets the color code for lime.
        /// </summary>
        public const string COLOR_LIME = "#7FFF00";
        /// <summary>
        /// Gets the color code for green.
        /// </summary>
        public const string COLOR_GREEN = "#00FF00";
        /// <summary>
        /// Gets the color code for cyan.
        /// </summary>
        public const string COLOR_CYAN = "#00FFFF";
        /// <summary>
        /// Gets the color code for azure.
        /// </summary>
        public const string COLOR_AZURE = "#007FFF";
        /// <summary>
        /// Gets the color code for blue.
        /// </summary>
        public const string COLOR_BLUE = "#002AFF";
        /// <summary>
        /// Gets the color code for purple.
        /// </summary>
        public const string COLOR_PURPLE = "#D400FF";
        /// <summary>
        /// Gets the color code for magenta.
        /// </summary>
        public const string COLOR_MAGENTA = "#FF007F";
        /// <summary>
        /// Gets the color code for white.
        /// </summary>
        public const string COLOR_WHITE = "#FFFFFF";
    }
}