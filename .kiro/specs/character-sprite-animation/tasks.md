# Implementation Plan: Character Sprite Animation

## Overview

This plan replaces the placeholder triangle with a sprite-based animation system for the player character. Implementation follows the established bottom-up pattern: pure C# animation state model first, then integration into GameState, GameLoop (reset), and CanvasRenderer, and finally JavaScript sprite loading/drawing in gameInterop.js. PNG sprite assets are added to wwwroot/images.

## Tasks

- [x] 1. Create PlayerAnimationState model class
  - [x] 1.1 Implement FacingDirection enum and PlayerAnimationState class
    - Create `src/BlazorAsteroids/Game/Models/PlayerAnimationState.cs`
    - Define `FacingDirection` enum with values `Right = 0`, `Left = 1`
    - Define constants: `BaseFrameDuration = 0.150f`, `SprintAnimationMultiplier = 1.3f`, `WalkCycleLength = 4`
    - Define static walk cycle frame array: `{ 1, 0, 2, 0 }` (walk1, stationary, walk2, stationary)
    - Implement properties: `CurrentCyclePosition` (int), `ElapsedFrameTime` (float), `Facing` (FacingDirection), `CurrentSpriteIndex` (computed from cycle array)
    - Implement `Update(float deltaTime, Vector2 movementDirection, bool isSprinting)`:
      - Update facing direction from horizontal component (positive → Right, negative → Left, zero → retain)
      - If movement magnitude is zero: reset `CurrentCyclePosition` to 0 and `ElapsedFrameTime` to 0, return
      - Calculate effective frame duration (base / multiplier when sprinting, base otherwise)
      - Accumulate elapsed time, advance frames via while loop
    - Implement `Reset()`: set `CurrentCyclePosition = 0`, `ElapsedFrameTime = 0f`, `Facing = Right`
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 5.1, 5.2, 5.3, 5.4, 8.1, 8.2, 8.3, 8.4_

  - [ ]* 1.2 Write property test: Zero movement produces stationary state
    - **Property 1: Zero movement produces stationary state**
    - Generate arbitrary `PlayerAnimationState` instances with random cycle positions, elapsed times, and facing directions
    - Call `Update` with zero-magnitude movement direction and any non-negative deltaTime
    - Assert `CurrentCyclePosition == 0`, `ElapsedFrameTime == 0`, `CurrentSpriteIndex == 0`
    - **Validates: Requirements 2.1, 2.2, 2.3, 3.5**

  - [ ]* 1.3 Write property test: Walk cycle sequence correctness
    - **Property 2: Walk cycle sequence correctness**
    - Generate a non-zero movement direction and advance through 4 consecutive frame durations
    - Assert `CurrentSpriteIndex` values follow the repeating sequence [1, 0, 2, 0]
    - After 4 advances, assert the cycle loops back to the beginning
    - **Validates: Requirements 3.1, 3.3**

  - [ ]* 1.4 Write property test: Frame timing respects sprint multiplier
    - **Property 3: Frame timing respects sprint multiplier**
    - Generate random deltaTime sequences with non-zero movement
    - When `isSprinting = false`, assert one frame advance per 150ms accumulated
    - When `isSprinting = true`, assert one frame advance per ~115.4ms accumulated
    - **Validates: Requirements 3.2, 3.4, 4.1, 4.2**

  - [ ]* 1.5 Write property test: Sprint transition preserves cycle position
    - **Property 4: Sprint transition preserves cycle position**
    - Generate a `PlayerAnimationState` at arbitrary cycle position P with non-zero movement
    - Toggle `isSprinting` between consecutive updates (true→false or false→true)
    - Assert `CurrentCyclePosition` remains P after the transition update
    - **Validates: Requirements 4.3**

  - [ ]* 1.6 Write property test: Facing direction follows horizontal input sign
    - **Property 5: Facing direction follows horizontal input sign**
    - Generate movement directions with non-zero X component
    - After `Update`, assert `Facing == Right` if X > 0, `Facing == Left` if X < 0
    - **Validates: Requirements 5.1, 5.2**

  - [ ]* 1.7 Write property test: Zero horizontal input retains facing direction
    - **Property 6: Zero horizontal input retains facing direction**
    - Generate a `PlayerAnimationState` with a known `Facing` value F
    - Call `Update` with movement direction whose X component is zero (but Y may be non-zero)
    - Assert `Facing` remains F
    - **Validates: Requirements 5.3**

  - [ ]* 1.8 Write property test: Reset produces clean initial state
    - **Property 7: Reset produces clean initial state**
    - Generate arbitrary `PlayerAnimationState` instances (random field values)
    - Call `Reset()`
    - Assert `CurrentCyclePosition == 0`, `ElapsedFrameTime == 0.0f`, `Facing == Right`
    - **Validates: Requirements 5.4, 8.1, 8.2, 8.3, 8.4**

- [x] 2. Checkpoint - Verify PlayerAnimationState model
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Integrate PlayerAnimationState into GameState and GameLoop
  - [x] 3.1 Add PlayerAnimationState property to GameState and call Update
    - Add `public PlayerAnimationState PlayerAnimation { get; set; } = new();` to `GameState`
    - In `GameState.Update()`, call `PlayerAnimation.Update(deltaTime, movementDirection, StaminaSystem.IsSprinting)` before player movement logic
    - _Requirements: 2.1, 3.1, 3.2, 4.1, 5.1, 5.2, 5.3_

  - [x] 3.2 Reset PlayerAnimationState on game transitions
    - In `GameLoop.RestartGame()`, add `_gameState.PlayerAnimation.Reset()`
    - In `GameLoop.TransitionToPlaying()`, add `_gameState.PlayerAnimation.Reset()`
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [x] 4. Add sprite assets to wwwroot
  - [x] 4.1 Add placeholder PNG sprite images
    - Create directory `src/BlazorAsteroids/wwwroot/images/` if it doesn't exist
    - Add `player-stationary.png`, `player-walk1.png`, `player-walk2.png` to the images directory
    - These can be placeholder images initially; final art can be swapped in later
    - _Requirements: 1.1_

- [x] 5. Extend CanvasRenderer to pass animation data to JavaScript
  - [x] 5.1 Pass animation frame index and facing direction in RenderAsync
    - In `CanvasRenderer.RenderAsync()`, append two additional parameters to the `renderFrame` JS call:
      - `state.PlayerAnimation.CurrentSpriteIndex` (int: 0, 1, or 2)
      - `(int)state.PlayerAnimation.Facing` (int: 0 = right, 1 = left)
    - _Requirements: 6.1, 6.2, 7.1_

- [x] 6. Checkpoint - Verify C# integration compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Implement sprite loading and rendering in JavaScript
  - [x] 7.1 Implement sprite image preloading in gameInterop.js
    - Add module-level state: `playerSprites` array (3 Image objects), `playerSpritesLoaded` boolean flag (initially false)
    - In `initializeGame` or at module load, begin loading the three PNG images from `./images/player-stationary.png`, `./images/player-walk1.png`, `./images/player-walk2.png`
    - Set a 10-second timeout per image; on timeout or `onerror`, log the failed path to `console.error` and leave `playerSpritesLoaded` as false
    - Set `playerSpritesLoaded = true` only when all 3 images complete successfully
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 7.2 Implement drawPlayerSprite function in gameInterop.js
    - Create function `drawPlayerSprite(ctx, x, y, size, spriteIndex, facingLeft, isFlashing)`
    - Select the sprite image from `playerSprites[spriteIndex]`
    - Scale so the larger dimension equals `2 * size` (player diameter), preserve aspect ratio
    - Center the image on (x, y)
    - If `facingLeft` is true, apply `ctx.save()`, translate, `ctx.scale(-1, 1)`, draw, `ctx.restore()` — ensuring no positional shift
    - If `isFlashing` is true, draw a semi-transparent red overlay (`rgba(255,0,0,0.4)`) composited on top of the sprite pixels using `globalCompositeOperation = 'source-atop'`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.4_

  - [x] 7.3 Modify renderFrame to use sprite rendering with fallback
    - Accept two new parameters at the end of `renderFrame` signature: `animationFrameIndex`, `facingDirection`
    - In the player drawing section, check `playerSpritesLoaded`:
      - If true: call `drawPlayerSprite(ctx, playerScreenX, playerScreenY, playerSize, animationFrameIndex, facingDirection === 1, isFlashing)`
      - If false: retain existing triangle drawing code as fallback
    - Ensure draw order remains: background → frogs → player sprite → bullets → UI
    - _Requirements: 1.2, 1.3, 1.4, 7.3, 7.5_

- [x] 8. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests use FsCheck.Xunit (already configured in the test project)
- The design uses C# throughout — all implementation uses .NET 8 / Blazor WebAssembly
- JavaScript modifications are in `src/BlazorAsteroids/wwwroot/js/gameInterop.js`
- Sprite assets go in `src/BlazorAsteroids/wwwroot/images/`
- The PlayerAnimationState follows the same architectural pattern as StaminaSystem and AmmoSystem
- Checkpoints ensure incremental validation after each major integration point
- Task 4.1 (sprite assets) can use simple placeholder PNGs — final artwork can be swapped later without code changes

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "4.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5", "1.6", "1.7", "1.8", "3.1"] },
    { "id": 2, "tasks": ["3.2", "5.1"] },
    { "id": 3, "tasks": ["7.1"] },
    { "id": 4, "tasks": ["7.2"] },
    { "id": 5, "tasks": ["7.3"] }
  ]
}
```
