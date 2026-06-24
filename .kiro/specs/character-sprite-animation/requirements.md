# Requirements Document

## Introduction

This feature replaces the placeholder triangle texture for the player character in Frogmageddon with a sprite-based animation system. The system uses three PNG images (stationary, walk1, walk2) to produce a walk cycle animation that responds to movement, sprint state, and facing direction. When the character moves, sprites cycle through a defined walk sequence. When sprinting, the cycle rate increases by 1.3x. Facing direction determines whether sprites are drawn normally (facing right) or horizontally mirrored (facing left).

## Glossary

- **Animation_System**: The subsystem responsible for tracking the current animation frame, elapsed time, frame advancement logic, and sprite selection for the player character.
- **Sprite_Frame**: One of the three PNG images used in the animation: stationary, walk1, or walk2.
- **Walk_Cycle**: The repeating animation sequence: walk1 → stationary → walk2 → stationary.
- **Frame_Duration**: The base time interval (in seconds) each Sprite_Frame is displayed before advancing to the next frame in the Walk_Cycle.
- **Sprint_Animation_Multiplier**: The 1.3x factor applied to the animation cycle rate when the player is sprinting, reducing the Frame_Duration proportionally.
- **Facing_Direction**: The horizontal direction the player character is facing, determined by the horizontal component of the most recent movement input. Either left or right.
- **Canvas_Renderer**: The rendering pipeline that passes sprite data to the JavaScript render function for drawing on the HTML5 Canvas.
- **Player**: The player-controlled entity with position, speed, health, and animation state.
- **Stamina_System**: The existing subsystem that tracks sprint state and provides the IsSprinting flag.

## Requirements

### Requirement 1: Sprite Asset Loading

**User Story:** As a player, I want the game to load character sprite images at startup, so that the character displays with proper artwork instead of a placeholder shape.

#### Acceptance Criteria

1. WHEN the game initializes, THE Canvas_Renderer SHALL preload three PNG images for the player character: stationary, walk1, and walk2, from the application asset directory.
2. WHEN all three sprite images have loaded successfully, THE Canvas_Renderer SHALL mark the player sprite assets as ready for rendering and use the sprite images in place of the placeholder triangle for all subsequent player rendering.
3. IF any sprite image fails to load or does not complete loading within 10 seconds, THEN THE Canvas_Renderer SHALL log an error message to the browser console indicating which image failed and fall back to rendering the existing placeholder triangle for the player.
4. WHILE sprite images are loading and have not yet succeeded or failed, THE Canvas_Renderer SHALL render the player using the existing placeholder triangle.

### Requirement 2: Stationary Sprite Display

**User Story:** As a player, I want my character to display the stationary sprite when not moving, so that the character appears at rest.

#### Acceptance Criteria

1. WHILE the player movement direction has a magnitude of zero (both X and Y components equal zero), THE Animation_System SHALL render the stationary Sprite_Frame as the current player sprite.
2. WHEN the player movement direction transitions from non-zero magnitude to zero magnitude, THE Animation_System SHALL reset the Walk_Cycle elapsed time to zero and set the cycle position to the first frame.
3. WHILE the player movement direction has zero magnitude and the Walk_Cycle is already reset, THE Animation_System SHALL maintain the Walk_Cycle elapsed time at zero and the cycle position at the first frame without re-triggering the reset each frame.

### Requirement 3: Walk Cycle Animation

**User Story:** As a player, I want my character to animate through a walk cycle when moving, so that the character appears to walk.

#### Acceptance Criteria

1. WHILE the player movement direction has non-zero magnitude, THE Animation_System SHALL cycle through the Walk_Cycle sequence: walk1, stationary, walk2, stationary (four frames total, repeating).
2. WHILE the player is moving and not sprinting, THE Animation_System SHALL advance to the next frame in the Walk_Cycle after each Frame_Duration interval of 150 milliseconds elapses.
3. WHEN the Walk_Cycle reaches the end of the four-frame sequence, THE Animation_System SHALL loop back to the first frame (walk1) and continue the cycle.
4. THE Animation_System SHALL accumulate elapsed time using the frame delta time provided by the game loop each update.
5. WHEN the player movement direction changes to zero magnitude, THE Animation_System SHALL reset the Walk_Cycle to the stationary frame and reset the accumulated frame elapsed time to zero.

### Requirement 4: Sprint Animation Speed Increase

**User Story:** As a player, I want the walk animation to speed up when sprinting, so that the visual feedback matches the faster movement speed.

#### Acceptance Criteria

1. WHILE the Stamina_System reports IsSprinting as true and the player movement direction has a non-zero length, THE Animation_System SHALL multiply the animation cycle rate by 1.3 (Sprint_Animation_Multiplier), reducing the effective Frame_Duration from the base value by dividing it by 1.3, and the resulting animation frame index SHALL advance accordingly within the same update frame.
2. WHILE the Stamina_System reports IsSprinting as false or the player movement direction has zero length, THE Animation_System SHALL use the base Frame_Duration without modification.
3. WHEN the player transitions from sprinting to not sprinting (IsSprinting changes from true to false), THE Animation_System SHALL revert to the base Frame_Duration starting on the next game loop update without resetting the current frame index within the animation cycle.

### Requirement 5: Facing Direction Tracking

**User Story:** As a player, I want my character to face the direction I am moving, so that the visual orientation matches my input.

#### Acceptance Criteria

1. WHEN the player movement direction has a positive horizontal component, THE Animation_System SHALL set the Facing_Direction to right.
2. WHEN the player movement direction has a negative horizontal component, THE Animation_System SHALL set the Facing_Direction to left.
3. WHEN the player movement direction has a zero horizontal component, THE Animation_System SHALL retain the previous Facing_Direction value.
4. WHEN a game session starts or restarts, THE Animation_System SHALL set the Facing_Direction to right.

### Requirement 6: Horizontal Sprite Mirroring

**User Story:** As a player, I want my character sprite to flip horizontally when facing left, so that the character visually faces the direction of travel.

#### Acceptance Criteria

1. WHILE the Facing_Direction is right, THE Canvas_Renderer SHALL draw the current Sprite_Frame without horizontal transformation.
2. WHILE the Facing_Direction is left, THE Canvas_Renderer SHALL draw the current Sprite_Frame with a horizontal mirror (flip) applied, reflecting the image across its vertical center axis.
3. THE Canvas_Renderer SHALL apply the mirror transformation per frame without modifying the source image data.
4. WHEN the mirror transformation is applied, THE Canvas_Renderer SHALL keep the rendered sprite's screen-space bounding box at the same position and dimensions as the non-mirrored sprite, ensuring no visible positional shift.

### Requirement 7: Sprite Rendering Integration

**User Story:** As a player, I want the sprite to render at the correct position and size on the game canvas, so that the character aligns with existing game entities.

#### Acceptance Criteria

1. THE Canvas_Renderer SHALL draw the current Sprite_Frame such that the horizontal and vertical center of the image aligns with the player screen position, computed as player world position minus Camera top-left position.
2. THE Canvas_Renderer SHALL scale the Sprite_Frame so that the larger of its width or height equals twice the player Size property (diameter), and the other dimension is scaled proportionally to preserve the source image aspect ratio.
3. THE Canvas_Renderer SHALL render the player sprite in the following draw order from back to front: background, frog entities, player sprite, bullets, UI elements.
4. WHILE the player is in the damage flash state (IsFlashing is true), THE Canvas_Renderer SHALL render a red-tinted version of the current Sprite_Frame by applying a semi-transparent red overlay with an opacity between 0.3 and 0.5 composited on top of the sprite pixels.
5. IF the player sprite assets are not yet loaded or failed to load, THEN THE Canvas_Renderer SHALL render the existing placeholder triangle at the same position and size instead of the Sprite_Frame.

### Requirement 8: Animation State Reset

**User Story:** As a player, I want the animation to reset when I restart the game, so that the character begins in a clean visual state.

#### Acceptance Criteria

1. WHEN the player transitions from GameOver to Playing phase, THE Animation_System SHALL reset the Walk_Cycle to frame index 0 (the stationary pose) before the first frame of the new Playing phase is rendered.
2. WHEN the player transitions from GameOver to Playing phase, THE Animation_System SHALL reset the Walk_Cycle elapsed time to zero seconds.
3. WHEN the player transitions from GameOver to Playing phase, THE Animation_System SHALL reset the Facing_Direction to right.
4. WHEN the player transitions from StartScreen to Playing phase, THE Animation_System SHALL initialize the Walk_Cycle to frame index 0, the elapsed time to zero seconds, and the Facing_Direction to right.
