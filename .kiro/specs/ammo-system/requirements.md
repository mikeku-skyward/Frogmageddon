# Requirements Document

## Introduction

This feature adds an ammunition management system to Frogmageddon. The player has a limited magazine of 20 bullets that must be manually reloaded by pressing the 'R' key. Reloading takes 2 seconds, during which a progress bar displays above the player and firing is disabled. The current ammo count is shown in the bottom-right corner of the screen. If the player attempts to fire with an empty magazine, a reload initiates automatically.

## Glossary

- **Ammo_System**: The subsystem responsible for tracking magazine capacity, current ammo count, reload state, and reload timing.
- **Player**: The player-controlled character in the game world.
- **Magazine**: The current loaded set of bullets available for firing, with a capacity of 20.
- **Reload_Timer**: A countdown timer that tracks the 2-second duration of the reload process.
- **Ammo_HUD**: The on-screen text element in the bottom-right corner displaying current ammo and magazine capacity.
- **Reload_Progress_Bar**: A visual indicator rendered above the Player showing reload completion progress.
- **Renderer**: The canvas rendering engine responsible for drawing game visuals to the screen.
- **Input_Manager**: The subsystem that captures and processes keyboard and mouse input.

## Requirements

### Requirement 1: Magazine Capacity and Initial State

**User Story:** As a player, I want to start the game with a full magazine of 20 bullets, so that I can begin fighting frogs immediately.

#### Acceptance Criteria

1. THE Ammo_System SHALL initialize the magazine with 20 bullets at the start of a new game.
2. THE Ammo_System SHALL set the maximum magazine capacity to 20 bullets.
3. WHEN the game restarts after a game-over, THE Ammo_System SHALL reset the magazine to 20 bullets.

### Requirement 2: Ammo Consumption on Firing

**User Story:** As a player, I want each shot to consume one bullet from my magazine, so that I must manage my ammo carefully.

#### Acceptance Criteria

1. WHEN the Player fires a bullet, THE Ammo_System SHALL decrease the current ammo count by 1 before the next frame is rendered.
2. WHILE the current ammo count is greater than 0 and the Ammo_System is not in the reloading state, THE Ammo_System SHALL permit the Player to fire.
3. WHILE the current ammo count equals 0, THE Ammo_System SHALL prevent the Player from firing by not spawning a bullet.
4. THE Ammo_System SHALL NOT decrease the current ammo count below 0.

### Requirement 3: Manual Reload Initiation

**User Story:** As a player, I want to press 'R' to reload my weapon, so that I can refill my magazine when I choose.

#### Acceptance Criteria

1. WHILE the game is in the playing phase, WHEN the Player presses the 'R' key and the current ammo count is less than 20, THE Ammo_System SHALL begin the reload process.
2. IF the Player presses the 'R' key and the current ammo count equals 20, THEN THE Ammo_System SHALL not begin the reload process and SHALL maintain the current state unchanged.
3. WHILE the Ammo_System is in the reloading state, WHEN the Player presses the 'R' key, THE Ammo_System SHALL ignore the input and SHALL not restart the Reload_Timer.

### Requirement 4: Automatic Reload on Empty Fire Attempt

**User Story:** As a player, I want the gun to automatically reload when I try to fire with an empty magazine, so that I don't have to remember to press 'R' every time.

#### Acceptance Criteria

1. WHEN the Player attempts to fire and the current ammo count equals 0 and the Ammo_System is not already in the reloading state, THE Ammo_System SHALL begin the reload process automatically.
2. WHEN the automatic reload begins, THE Ammo_System SHALL not fire a bullet for that input.
3. WHILE the Ammo_System is in the reloading state, WHEN the Player attempts to fire, THE Ammo_System SHALL not restart the reload process.

### Requirement 5: Reload Duration and Completion

**User Story:** As a player, I want reloading to take 2 seconds, so that there is a tactical cost to running out of ammo.

#### Acceptance Criteria

1. WHEN the reload process begins, THE Reload_Timer SHALL start a 2-second countdown using elapsed game time.
2. WHEN the Reload_Timer reaches 0 seconds remaining, THE Ammo_System SHALL set the current ammo count to 20 and exit the reloading state within the same frame.
3. IF the Player dies while the Ammo_System is in the reloading state, THEN THE Ammo_System SHALL cancel the reload process and exit the reloading state without restoring ammo.
4. WHILE the game is paused, THE Reload_Timer SHALL not decrement.

### Requirement 6: Firing Disabled During Reload

**User Story:** As a player, I want to be unable to fire while reloading, so that the reload mechanic has meaningful gameplay impact.

#### Acceptance Criteria

1. WHILE the Ammo_System is in the reloading state, THE Ammo_System SHALL prevent the Player from firing bullets and SHALL NOT decrease the current ammo count.
2. WHILE the Ammo_System is in the reloading state, WHEN the Player clicks to fire, THE Ammo_System SHALL discard the fire input without buffering or queuing it for execution after the reload completes.
3. WHILE the Ammo_System is in the reloading state, WHEN the Player clicks to fire, THE Ammo_System SHALL NOT produce any bullet or firing visual effect.

### Requirement 7: Ammo HUD Display

**User Story:** As a player, I want to see my current ammo count on screen, so that I know how many bullets I have left.

#### Acceptance Criteria

1. WHILE the game is in the Playing phase, THE Renderer SHALL display the Ammo_HUD in a fixed screen position within the bottom-right quadrant of the canvas, with a margin of no more than 32 pixels from the right and bottom edges.
2. THE Ammo_HUD SHALL display the current ammo count and magazine capacity in the format "{current}/{max}" (e.g., "8/20") using a font size of at least 16 pixels.
3. WHEN the current ammo count changes, THE Ammo_HUD SHALL update to reflect the new count within the same render frame.
4. IF the current ammo count reaches 0, THEN THE Ammo_HUD SHALL display "0/{max}" and remain visible until ammo is replenished or the Playing phase ends.
5. WHILE the game is in the StartScreen or GameOver phase, THE Renderer SHALL NOT display the Ammo_HUD.

### Requirement 8: Reload Progress Bar Display

**User Story:** As a player, I want to see a progress bar above my character while reloading, so that I know how much time is left before I can fire again.

#### Acceptance Criteria

1. WHILE the Ammo_System is in the reloading state, THE Renderer SHALL display the Reload_Progress_Bar centered horizontally above the Player character at a fixed vertical offset of 20 pixels above the Player's top edge.
2. THE Reload_Progress_Bar SHALL have a fixed width of 40 pixels and a height of 6 pixels, and SHALL render a filled portion from left to right representing the ratio of elapsed reload time to the total 2-second reload duration.
3. WHEN the Ammo_System exits the reloading state, THE Renderer SHALL hide the Reload_Progress_Bar within the same frame.
4. WHILE the Ammo_System is in the reloading state and the Player moves, THE Renderer SHALL update the Reload_Progress_Bar position to remain above the Player character on each rendered frame.

### Requirement 9: Input Manager Supports Reload Key

**User Story:** As a developer, I want the input manager to recognize the 'R' key, so that the reload action can be triggered by player input.

#### Acceptance Criteria

1. THE Input_Manager SHALL recognize the 'R' key as a valid input key.
2. WHEN the 'R' key is pressed, THE Input_Manager SHALL report the key press to the game loop.
3. WHEN the 'R' key is released, THE Input_Manager SHALL report the key release to the game loop.
