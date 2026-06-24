# Requirements Document

## Introduction

This feature modifies the existing frog spawning system in Frogmageddon to spawn frogs in groups rather than one at a time, and to increase the overall spawn rate slightly. Currently, the FrogSpawner produces a single frog every time the spawn timer elapses. The new behavior spawns a cluster of 5–7 frogs at once each spawn event, with a reduced base interval to produce a higher overall frog density on screen.

## Glossary

- **FrogSpawner**: The component responsible for deciding when and where frogs appear in the game world. Located in `FrogSpawner.cs`.
- **Spawn_Event**: A single timer-triggered occasion on which the FrogSpawner creates new frogs.
- **Group_Size**: The number of frogs created during a single Spawn_Event (between 5 and 7, inclusive).
- **Base_Spawn_Interval**: The time in seconds between consecutive Spawn_Events at a 1.0× rate multiplier.
- **Camera_Viewport**: The rectangular region of the game world currently visible to the player.
- **MaxFrogs**: The configured upper limit of simultaneously alive frogs in the game (40).
- **GameState**: The central state object that holds the list of active frogs and orchestrates per-frame updates.
- **Spawn_Margin**: The distance in pixels (50) outside the viewport edge where frogs are placed.

## Requirements

### Requirement 1: Group Spawning

**User Story:** As a player, I want frogs to appear in groups, so that the game feels more intense with waves of enemies arriving together.

#### Acceptance Criteria

1. WHEN a Spawn_Event occurs, THE FrogSpawner SHALL create a group of frogs with a Group_Size chosen uniformly at random between 5 and 7, inclusive.
2. WHEN a Spawn_Event occurs, THE FrogSpawner SHALL spawn each frog in the group at a position outside the Camera_Viewport at the Spawn_Margin distance (50 pixels) from the selected edge.
3. IF spawning the full Group_Size would cause the total active frog count to exceed MaxFrogs (40), THEN THE FrogSpawner SHALL spawn only enough frogs to reach MaxFrogs and discard the remainder.

### Requirement 2: Increased Spawn Rate

**User Story:** As a player, I want to encounter slightly more frogs than before, so that the game provides a greater challenge.

#### Acceptance Criteria

1. THE FrogSpawner SHALL use a Base_Spawn_Interval of 2.0 seconds (reduced from 3.0 seconds).
2. WHILE the elapsed game time exceeds 30 seconds, THE FrogSpawner SHALL apply a linear rate multiplier that ramps from 1.0× at 30 seconds to 2.0× at 120 seconds of elapsed time, resulting in a minimum effective spawn interval of 1.0 second at full ramp.
3. WHILE the number of active frogs equals MaxFrogs (40), THE FrogSpawner SHALL suppress spawning until the active frog count drops below 40, regardless of the current spawn interval.

### Requirement 3: Group Clustering

**User Story:** As a player, I want grouped frogs to spawn near each other, so that the wave feels cohesive rather than scattered randomly around the viewport.

#### Acceptance Criteria

1. WHEN a Spawn_Event occurs, THE FrogSpawner SHALL select one of the four viewport edges (top, bottom, left, right) with uniform probability for the entire group.
2. WHEN a Spawn_Event occurs, THE FrogSpawner SHALL choose an anchor point at a uniformly random position along the full length of the selected edge, clamped so that the anchor is at least 120 pixels from each corner of that edge.
3. WHEN a Spawn_Event occurs, THE FrogSpawner SHALL position each frog in the group at a random offset along the edge axis within 120 pixels of the anchor point.
4. WHEN a Spawn_Event occurs, THE FrogSpawner SHALL place all frogs in the group on the perpendicular axis at the Spawn_Margin (50 pixels) outside the Camera_Viewport boundary of the selected edge.

### Requirement 4: GameState Integration

**User Story:** As a developer, I want the GameState update loop to handle multiple frogs returned from a single spawn event, so that group spawning integrates cleanly with the existing game loop.

#### Acceptance Criteria

1. WHEN FrogSpawner returns a group of frogs, THE GameState SHALL add all returned frogs to the active Frogs list in the same frame.
2. THE GameState SHALL not call FrogSpawner more than once per frame.
3. THE FrogSpawner.TrySpawn method SHALL return a List of Frog objects (possibly empty) instead of a single nullable Frog.

### Requirement 5: MaxFrogs Cap Preservation

**User Story:** As a designer, I want the maximum frog cap to remain enforced, so that performance stays stable even with group spawning.

#### Acceptance Criteria

1. THE FrogSpawner SHALL not spawn frogs that would cause the total active frog count to exceed MaxFrogs (40).
2. WHILE the active frog count equals MaxFrogs, THE FrogSpawner SHALL skip the Spawn_Event entirely and reset the spawn timer.
3. IF the active frog count plus the chosen Group_Size would exceed MaxFrogs, THEN THE FrogSpawner SHALL reduce the spawned count to (MaxFrogs - currentFrogCount).
