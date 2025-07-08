
// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System.Collections.Generic;

namespace Germio {
    /// <summary>
    /// Root data class for Germio game save data.
    /// Contains the overall game state and all worlds.
    /// </summary>
    public class Data {
        /// <summary>
        /// The current game state (flags, inventory, turn, etc).
        /// </summary>
        public State state { get; set; } = new State();

        /// <summary>
        /// List of all worlds in the game.
        /// </summary>
        public List<World> worlds { get; set; } = new List<World>();
    }

    /// <summary>
    /// Represents the player's current state, including flags, inventory, and progress.
    /// </summary>
    public class State {
        /// <summary>
        /// Boolean flags for game events, achievements, etc.
        /// </summary>
        public Dictionary<string, bool> flags { get; set; } = new Dictionary<string, bool>();

        /// <summary>
        /// Inventory items and their values (can be int, string, etc).
        /// </summary>
        public Dictionary<string, object> inventory { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Current turn number (if applicable).
        /// </summary>
        public int? turn { get; set; }

        /// <summary>
        /// Player's score (optional).
        /// </summary>
        public int? score { get; set; }

        /// <summary>
        /// Number of lives remaining (optional).
        /// </summary>
        public int? lives { get; set; }

        /// <summary>
        /// Number of bombs (optional).
        /// </summary>
        public int? bombs { get; set; }

        /// <summary>
        /// Number of power-ups (optional).
        /// </summary>
        public int? powerUps { get; set; }

        /// <summary>
        /// Name of the current scene.
        /// </summary>
        public string currentScene { get; set; } = string.Empty;

        /// <summary>
        /// Name of the current team (if applicable).
        /// </summary>
        public string currentTeam { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a world in the game, containing multiple levels.
    /// </summary>
    public class World {
        /// <summary>
        /// Unique world identifier.
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the world.
        /// </summary>
        public string name { get; set; } = string.Empty;

        /// <summary>
        /// Scene name associated with this world.
        /// </summary>
        public string scene { get; set; } = string.Empty;

        /// <summary>
        /// List of levels in this world.
        /// </summary>
        public List<Level> levels { get; set; } = new List<Level>();
    }

    /// <summary>
    /// Represents a level within a world.
    /// </summary>
    public class Level {
        /// <summary>
        /// Unique level identifier.
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the level.
        /// </summary>
        public string name { get; set; } = string.Empty;

        /// <summary>
        /// Scene name associated with this level.
        /// </summary>
        public string scene { get; set; } = string.Empty;

        /// <summary>
        /// List of possible next levels and their conditions.
        /// </summary>
        public List<Next> next { get; set; } = new List<Next>();

        /// <summary>
        /// List of events that can occur in this level.
        /// </summary>
        public List<Event> events { get; set; } = new List<Event>();
    }

    /// <summary>
    /// Represents a transition to another level, with an optional condition.
    /// </summary>
    public class Next {
        /// <summary>
        /// ID of the next level.
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Condition required to unlock this transition (expression or flag).
        /// </summary>
        public string condition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an event that can occur in a level.
    /// </summary>
    public class Event {
        /// <summary>
        /// Unique event identifier.
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Trigger type for this event (e.g., "onEnter").
        /// </summary>
        public string trigger { get; set; } = string.Empty;

        /// <summary>
        /// Action to perform when this event is triggered.
        /// </summary>
        public GameAction action { get; set; } = new GameAction();
    }

    /// <summary>
    /// Represents an action to be performed as part of an event.
    /// Extend this class to support more action types.
    /// </summary>
    public class GameAction {
        /// <summary>
        /// Name of the flag to set (if any).
        /// </summary>
        public string setFlag { get; set; } = string.Empty;
        // Extend as needed for more action types
    }
}
