# Implementation Plan: Sprint Stamina

## Overview

This plan implements a sprint mechanic for Frogmageddon. The player holds Shift to move at 1.3× speed, consuming stamina that recharges after a 2-second cooldown. Implementation follows the same bottom-up pattern as the AmmoSystem: pure state-machine model first, then integration into GameState, InputManager, GameLoop, and finally rendering (CanvasRenderer + JavaScript stamina bar).

## Tasks

- [x] 1. Create StaminaSystem model class
  - [x] 1.1 Implement the StaminaState enum and StaminaSystem class
    - Create `src/BlazorAsteroids/Game/Models/StaminaState.cs` with enum values: Idle, Sprinting, RechargeDelay, Recharging
    - Create `src/BlazorAsteroids/Game/Models/StaminaSystem.cs`
    - Define constants: `SprintMultiplier = 1.3f`, `DepletionRate = 0.2f`, `RechargeRate = 0.0667f`, `RechargeDelayDuration = 2.0f`
    - Implement properties: `Stamina` (float, clamped [0,1]), `State` (StaminaState), `RechargeDelayTimer` (float), `IsSprinting` (bool, read-only), `SpeedMultiplier` (float, read-only: 1.3 if sprinting, else 1.0)
    - Implement `Update(float deltaTime, bool shiftPressed)`: advance state machine per design — handle Idle→Sprinting, Sprinting→RechargeDelay, RechargeDelay→Recharging, Recharging→Idle transitions, depletion at 0.2/s, recharge at 0.0667/s, delay timer countdown, shift-during-delay resume logic
    - Implement `Reset()`: set Stamina=1.0, State=Idle, RechargeDelayTimer=0
    - Treat negative deltaTime as 0 (defensive)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3_

  - [ ]* 1.2 Write property test: Shift Key Press/Release Round-Trip
    - **Property 1: Shift Key Press/Release Round-Trip**
    - Generate random case variations of the string "shift"
    - After `SetKeyDown(variant)`, assert `IsKeyPressed("shift")` returns true
    - After `SetKeyUp(variant)`, assert `IsKeyPressed("shift")` returns false
    - **Validates: Requirements 1.2, 1.3**

  - [ ]* 1.3 Write property test: Speed Multiplier Correctness
    - **Property 2: Speed Multiplier Correctness**
    - Generate arbitrary StaminaSystem states (any stamina, any StaminaState)
    - Assert `SpeedMultiplier == 1.3f` if and only if `IsSprinting == true`; otherwise assert `SpeedMultiplier == 1.0f`
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4**

  - [ ]* 1.4 Write property test: Stamina Bounds Invariant
    - **Property 3: Stamina Bounds Invariant**
    - Generate random sequences of `(deltaTime, shiftPressed)` pairs with non-negative deltaTime values
    - After each `Update` call, assert `Stamina >= 0.0f && Stamina <= 1.0f`
    - **Validates: Requirements 3.1, 3.4**

  - [ ]* 1.5 Write property test: Depletion Rate During Sprint
    - **Property 4: Depletion Rate During Sprint**
    - Generate a StaminaSystem in Sprinting state with stamina `s > 0`, and a random `dt > 0`
    - After `Update(dt, true)`, assert stamina equals `max(0, s - 0.2 * dt)`
    - **Validates: Requirements 3.2, 5.3**

  - [ ]* 1.6 Write property test: Sprint End Triggers Recharge Delay
    - **Property 5: Sprint End Triggers Recharge Delay**
    - Generate a StaminaSystem in Sprinting state with stamina `0 < s < 1.0`
    - After `Update(dt, false)` (shift released), assert State == RechargeDelay and RechargeDelayTimer == 2.0
    - Also test: Sprinting state where Update causes stamina to hit 0, assert same transition
    - **Validates: Requirements 4.1, 4.2**

  - [ ]* 1.7 Write property test: Stamina Frozen During Recharge Delay
    - **Property 6: Stamina Frozen During Recharge Delay**
    - Generate a StaminaSystem in RechargeDelay state with stamina `s` and timer `t > 0`
    - Call `Update(dt, false)` where `dt < t`
    - Assert stamina remains exactly `s` (unchanged)
    - **Validates: Requirements 4.3**

  - [ ]* 1.8 Write property test: Recharge Delay Timer Expiry Transitions to Recharging
    - **Property 7: Recharge Delay Timer Expiry Transitions to Recharging**
    - Generate a StaminaSystem in RechargeDelay state with timer `t > 0`
    - Call `Update(dt, false)` where `dt >= t`
    - Assert State == Recharging
    - **Validates: Requirements 4.4**

  - [ ]* 1.9 Write property test: Shift During Recharge Delay Resumes Sprint
    - **Property 8: Shift During Recharge Delay Resumes Sprint**
    - Generate a StaminaSystem in RechargeDelay state with stamina `s > 0`
    - Call `Update(dt, true)` (shift pressed)
    - Assert State == Sprinting
    - **Validates: Requirements 4.5**

  - [ ]* 1.10 Write property test: Recharge Rate When Recharging
    - **Property 9: Recharge Rate When Recharging**
    - Generate a StaminaSystem in Recharging state with stamina `s < 1.0`
    - Call `Update(dt, false)` where `s + 0.0667 * dt <= 1.0`
    - Assert stamina equals `s + 0.0667 * dt`
    - **Validates: Requirements 5.1**

  - [ ]* 1.11 Write property test: Transition from Recharging to Sprinting Preserves Stamina
    - **Property 10: Transition from Recharging to Sprinting Preserves Stamina**
    - Generate a StaminaSystem in Recharging state with stamina `s > 0`
    - Call `Update(dt, true)` (shift pressed)
    - Assert State == Sprinting and stamina == `s` (no recharge applied on transition frame)
    - **Validates: Requirements 5.4**

  - [ ]* 1.12 Write property test: Reset Postconditions
    - **Property 11: Reset Postconditions**
    - Generate arbitrary StaminaSystem states (any stamina, any state, any timer)
    - After calling `Reset()`, assert Stamina == 1.0, State == Idle, RechargeDelayTimer == 0, IsSprinting == false
    - **Validates: Requirements 7.1, 7.2, 7.3**

- [x] 2. Checkpoint - Verify StaminaSystem model
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Add InputManager support for Shift key
  - [x] 3.1 Add "shift" to InputManager ValidKeys set
    - Add `"shift"` to the `ValidKeys` HashSet in `InputManager.cs`
    - No interface changes needed — `IsKeyPressed("shift")` works via existing `ToLowerInvariant()` handling
    - The existing keyboard listener in gameInterop.js already sends `e.key.toLowerCase()` which maps both left/right Shift to `"shift"`
    - _Requirements: 1.1, 1.2, 1.3_

- [x] 4. Integrate StaminaSystem into GameState
  - [x] 4.1 Add StaminaSystem property to GameState and apply SpeedMultiplier
    - Add `public StaminaSystem StaminaSystem { get; set; } = new();` to `GameState`
    - In the `Update()` method, multiply the velocity calculation by `StaminaSystem.SpeedMultiplier` alongside the existing backpedal `speedMultiplier`
    - Specifically, change: `Vector2 velocity = movementDirection * (Player.Speed * speedMultiplier) * deltaTime;`
    - To: `Vector2 velocity = movementDirection * (Player.Speed * speedMultiplier * StaminaSystem.SpeedMultiplier) * deltaTime;`
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 5. Integrate StaminaSystem into GameLoop
  - [x] 5.1 Pass shift key state to StaminaSystem.Update in GameLoop.Tick
    - In the Playing phase of `Tick()`, after reading movement direction, query `_inputManager.IsKeyPressed("shift")`
    - Call `_gameState.StaminaSystem.Update(deltaTimeSec, shiftPressed)` in the Playing phase, before `_gameState.Update()`
    - _Requirements: 2.1, 3.2, 4.1, 4.2, 5.1_

  - [x] 5.2 Reset StaminaSystem on game restart
    - In `RestartGame()`, add `_gameState.StaminaSystem.Reset()` call
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 6. Checkpoint - Verify input and game loop integration
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Add renderer support for Stamina Bar
  - [x] 7.1 Pass stamina ratio from CanvasRenderer to JavaScript
    - Modify `CanvasRenderer.RenderAsync()` to pass `Math.Clamp(_gameState.StaminaSystem.Stamina, 0f, 1f)` as an additional parameter to the `renderFrame` JS call
    - Add the parameter at the end of the existing parameter list
    - _Requirements: 6.1_

  - [x] 7.2 Implement stamina bar rendering in JavaScript
    - In `gameInterop.js` `renderFrame` function, accept the new `staminaRatio` parameter
    - Draw a 200×12px stamina bar in the top-left HUD area, positioned below the health indicator (if any) with no more than 4px vertical gap
    - Background: dark gray (`#333333`). Fill: green (`#00cc00`) scaled horizontally by staminaRatio from the left edge
    - When staminaRatio is 1.0, the bar is fully filled; when 0.0, no fill is visible
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 8. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests use FsCheck.Xunit (already configured in the test project)
- The design uses C# throughout — all implementation uses .NET 8 / Blazor WebAssembly
- Checkpoints ensure incremental validation after each major integration point
- The JavaScript rendering task (7.2) modifies `wwwroot/js/gameInterop.js`
- The StaminaSystem follows the same architectural pattern as the existing AmmoSystem

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5", "1.6", "1.7", "1.8", "1.9", "1.10", "1.11", "1.12", "3.1"] },
    { "id": 2, "tasks": ["4.1"] },
    { "id": 3, "tasks": ["5.1", "5.2"] },
    { "id": 4, "tasks": ["7.1"] },
    { "id": 5, "tasks": ["7.2"] }
  ]
}
```
