# Requirements Document

## Introduction

This feature adds a sprint mechanic to Frogmageddon. The player can hold the Shift key to move at 1.3x speed, consuming stamina over time. Stamina is displayed as a bar below the health indicator and recharges when the player is not sprinting, subject to a 2-second cooldown delay after releasing the Shift key.

## Glossary

- **Stamina_System**: The game subsystem responsible for tracking stamina level, depletion, recharge delay, and recharge rate.
- **Input_Manager**: The engine component that tracks keyboard state and provides key-press queries to the game loop.
- **Game_State**: The central model that updates player movement, physics, and game entities each frame.
- **Canvas_Renderer**: The rendering pipeline that passes HUD and game data to the JavaScript render function.
- **Stamina_Bar**: The UI element displayed below the health indicator showing current stamina as a filled rectangle.
- **Sprint_Multiplier**: The 1.3x factor applied to base player speed while sprinting is active.
- **Recharge_Delay**: A 2-second cooldown period after the player releases Shift, during which stamina does not recharge.
- **Player**: The player-controlled entity with position, speed, health, and stamina properties.

## Requirements

### Requirement 1: Shift Key Recognition

**User Story:** As a player, I want the game to recognize the Shift key, so that I can activate sprint.

#### Acceptance Criteria

1. THE Input_Manager SHALL include "shift" in the set of valid keys that are tracked for pressed state, recognizing both left and right Shift keys as the same "shift" key.
2. WHEN the player presses the Shift key (left or right), THE Input_Manager SHALL register "shift" as a pressed key using case-insensitive matching, such that `IsKeyPressed("shift")` returns true.
3. WHEN the player releases the Shift key, THE Input_Manager SHALL remove "shift" from the pressed key set, such that `IsKeyPressed("shift")` returns false.

### Requirement 2: Sprint Activation

**User Story:** As a player, I want to sprint by holding the Shift key, so that I can move faster to evade frogs.

#### Acceptance Criteria

1. WHILE either Shift key (left or right) is pressed and the Stamina_System has stamina greater than zero, THE Game_State SHALL multiply the Player base speed by 1.3 (Sprint_Multiplier), applied multiplicatively with any other active speed modifiers, for movement calculations each update frame.
2. WHILE the Shift key is not pressed, THE Game_State SHALL apply a speed multiplier of 1.0 (no sprint bonus) to the Player base speed.
3. WHILE the Stamina_System has stamina equal to zero, THE Game_State SHALL apply a speed multiplier of 1.0 regardless of Shift key state.
4. WHEN the Shift key is released OR the Stamina_System reaches zero stamina, THE Game_State SHALL remove the Sprint_Multiplier from the Player speed calculation on the next update frame.

### Requirement 3: Stamina Depletion

**User Story:** As a player, I want stamina to deplete while sprinting, so that sprint is a limited resource requiring tactical use.

#### Acceptance Criteria

1. THE Stamina_System SHALL maintain a stamina value ranging from 0.0 to 1.0, where 1.0 represents full stamina.
2. WHILE the Player is sprinting, THE Stamina_System SHALL deplete stamina at a rate of 0.2 per second (full depletion in 5 seconds).
3. WHEN stamina reaches exactly 0.0 during sprinting, THE Stamina_System SHALL clamp stamina to exactly 0.0 and sprint SHALL deactivate; this rule SHALL only apply when stamina reaches exactly 0.0.
4. THE Stamina_System SHALL NOT allow stamina to become negative.

### Requirement 4: Stamina Recharge Delay

**User Story:** As a player, I want a short cooldown after I stop sprinting before stamina recharges, so that there is a cost to using sprint.

#### Acceptance Criteria

1. WHEN the Player releases the Shift key while stamina is below maximum, THE Stamina_System SHALL set the system state to RECHARGE_DELAY and begin a Recharge_Delay timer of 2 seconds.
2. WHEN the Player's stamina reaches zero during active sprinting, THE Stamina_System SHALL end the sprint, set the system state to RECHARGE_DELAY, and begin a Recharge_Delay timer of 2 seconds; the recharge delay SHALL only trigger when stamina hits zero during active sprinting.
3. WHILE the Recharge_Delay timer is active, THE Stamina_System SHALL hold stamina at its current value and not recharge.
4. WHEN the Recharge_Delay timer reaches zero, THE Stamina_System SHALL transition to the recharging state.
5. IF the Player presses the Shift key during the Recharge_Delay period and stamina is greater than zero, THEN THE Stamina_System SHALL cancel the Recharge_Delay timer and resume sprinting.
6. IF the Player presses the Shift key during the Recharge_Delay period and stamina is equal to zero, THEN THE Stamina_System SHALL not cancel the Recharge_Delay timer and SHALL not begin sprinting.

### Requirement 5: Stamina Recharge

**User Story:** As a player, I want stamina to recharge when I am not sprinting, so that I can sprint again after waiting.

#### Acceptance Criteria

1. WHILE the Player is not sprinting and the Recharge_Delay timer is inactive, THE Stamina_System SHALL increase stamina by 0.0667 multiplied by the frame delta time each frame (full recharge from 0.0 to 1.0 in 15 seconds), with no cap on per-frame recharge amount.
2. WHEN stamina reaches or exceeds 1.0 during recharging, THE Stamina_System SHALL clamp stamina to exactly 1.0 and stop recharging.
3. WHILE the Player is sprinting, THE Stamina_System SHALL not recharge stamina.
4. IF the Player begins sprinting while stamina is recharging, THEN THE Stamina_System SHALL stop recharging and retain the current stamina value at the moment sprint resumes.

### Requirement 6: Stamina Bar Display

**User Story:** As a player, I want to see a stamina bar on screen, so that I know how much sprint I have remaining.

#### Acceptance Criteria

1. THE Canvas_Renderer SHALL pass stamina ratio data to the JavaScript render function each frame, WHERE the stamina ratio value SHALL be enforced within the valid 0.0 to 1.0 range before rendering.
2. THE Stamina_Bar SHALL render in the top-left area of the game HUD, positioned below the health indicator with a vertical gap of no more than 4 pixels.
3. THE Stamina_Bar SHALL have a fixed width of 200 pixels and a height of 12 pixels, with a filled portion that scales horizontally from the left edge proportional to the current stamina ratio.
4. WHEN stamina is at 1.0, THE Stamina_Bar SHALL appear completely filled across its entire width.
5. WHEN stamina is at 0.0, THE Stamina_Bar SHALL appear completely empty with no filled portion visible.

### Requirement 7: Stamina Reset on Game Restart

**User Story:** As a player, I want stamina to reset when I restart the game, so that I begin with full sprint capacity.

#### Acceptance Criteria

1. WHEN the player transitions from GameOver to Playing phase, THE Stamina_System SHALL reset stamina to 1.0.
2. WHEN the player transitions from GameOver to Playing phase, THE Stamina_System SHALL reset the Recharge_Delay timer to zero and set the recharge delay state to inactive.
3. WHEN the player transitions from GameOver to Playing phase, THE Stamina_System SHALL set the sprint state to inactive.
