# Implementation Plan: Ammo System

## Overview

This plan implements a magazine-based ammo system for Frogmageddon. The player has a 20-round magazine with manual reload (R key), auto-reload on empty fire, and visual feedback via an ammo counter HUD and reload progress bar. Implementation follows a bottom-up dependency order: pure model first, then integration into game state, input, game loop, firing logic, and finally rendering.

## Tasks

- [x] 1. Create AmmoSystem model class
  - [x] 1.1 Implement the AmmoSystem class as a pure state machine
    - Create `src/BlazorAsteroids/Game/Models/AmmoSystem.cs`
    - Define constants: `MaxAmmo = 20`, `ReloadDuration = 2.0f`
    - Implement properties: `CurrentAmmo`, `IsReloading`, `ReloadTimeRemaining`, `CanFire`, `ReloadProgress`
    - Implement `TryFire()`: decrement ammo if CanFire, auto-reload if ammo is 0 and not reloading, return false if reloading
    - Implement `StartReload()`: begin reload if ammo < max and not already reloading, no-op otherwise
    - Implement `CancelReload()`: exit reloading state without restoring ammo
    - Implement `Update(float deltaTime)`: decrement ReloadTimeRemaining, complete reload when timer hits 0
    - Implement `Reset()`: restore to initial full-magazine state
    - Implement `GetHudText()`: return formatted `"{current}/{max}"` string
    - _Requirements: 1.1, 1.2, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 7.2_

  - [ ]* 1.2 Write property test: Reset always restores full magazine
    - **Property 1: Reset always restores full magazine**
    - Generate arbitrary AmmoSystem states (varying currentAmmo, isReloading, reloadTimeRemaining)
    - After calling Reset(), assert CurrentAmmo == 20, IsReloading == false, ReloadTimeRemaining == 0
    - **Validates: Requirements 1.3**

  - [ ]* 1.3 Write property test: Firing decreases ammo by exactly 1
    - **Property 2: Firing decreases ammo by exactly 1**
    - Generate AmmoSystem states where CurrentAmmo in [1..20] and IsReloading == false
    - After calling TryFire(), assert CurrentAmmo == previousAmmo - 1 and return value is true
    - **Validates: Requirements 2.1**

  - [ ]* 1.4 Write property test: Ammo non-negative invariant
    - **Property 3: Ammo non-negative invariant**
    - Generate random sequences of TryFire, StartReload, Update(dt), Reset, CancelReload calls
    - After each operation, assert CurrentAmmo >= 0
    - **Validates: Requirements 2.4**

  - [ ]* 1.5 Write property test: CanFire iff ammo > 0 and not reloading
    - **Property 4: CanFire iff ammo > 0 and not reloading**
    - Generate arbitrary AmmoSystem states
    - Assert CanFire == (CurrentAmmo > 0 && !IsReloading)
    - **Validates: Requirements 2.2, 6.1**

  - [ ]* 1.6 Write property test: StartReload transitions when ammo below max
    - **Property 5: StartReload transitions when ammo below max**
    - Generate states where CurrentAmmo in [0..19] and IsReloading == false
    - After calling StartReload(), assert IsReloading == true and ReloadTimeRemaining == ReloadDuration
    - **Validates: Requirements 3.1**

  - [ ]* 1.7 Write property test: Active reload timer is not restarted
    - **Property 6: Active reload timer is not restarted**
    - Generate states where IsReloading == true and ReloadTimeRemaining in (0..ReloadDuration]
    - After calling StartReload() or TryFire(), assert ReloadTimeRemaining is unchanged
    - **Validates: Requirements 3.3, 4.3**

  - [ ]* 1.8 Write property test: Reload completion restores full magazine
    - **Property 7: Reload completion restores full magazine**
    - Generate reloading states with ReloadTimeRemaining > 0
    - Call Update(deltaTime) where deltaTime >= ReloadTimeRemaining
    - Assert CurrentAmmo == MaxAmmo and IsReloading == false
    - **Validates: Requirements 5.2**

  - [ ]* 1.9 Write property test: Cancel reload preserves current ammo
    - **Property 8: Cancel reload preserves current ammo**
    - Generate reloading states with any CurrentAmmo value
    - After calling CancelReload(), assert IsReloading == false and CurrentAmmo unchanged
    - **Validates: Requirements 5.3**

  - [ ]* 1.10 Write property test: HUD text format
    - **Property 9: HUD text format**
    - Generate CurrentAmmo values in [0..20]
    - Assert GetHudText() returns string matching "{CurrentAmmo}/20"
    - **Validates: Requirements 7.2**

  - [ ]* 1.11 Write property test: Reload progress ratio
    - **Property 10: Reload progress ratio**
    - Generate reloading states with ReloadTimeRemaining in (0..ReloadDuration]
    - Assert ReloadProgress == (ReloadDuration - ReloadTimeRemaining) / ReloadDuration
    - **Validates: Requirements 8.2**

- [x] 2. Integrate AmmoSystem into GameState
  - [x] 2.1 Add AmmoSystem property to GameState and wire into Update
    - Add `public AmmoSystem AmmoSystem { get; set; } = new();` to `GameState`
    - Add `AmmoSystem.Update(deltaTime)` call at the end of the `Update()` method body
    - _Requirements: 5.1, 5.4_

  - [x] 2.2 Gate bullet firing through AmmoSystem.TryFire
    - Modify `GameState.FireBullet()` to call `AmmoSystem.TryFire()` as a guard clause
    - If TryFire returns false, return early without spawning a bullet
    - Remove the need for external ammo checks — the method now self-gates
    - _Requirements: 2.1, 2.2, 2.3, 4.1, 4.2, 6.1, 6.2, 6.3_

- [x] 3. Checkpoint - Verify AmmoSystem model and GameState integration
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Add InputManager support for 'R' key
  - [x] 4.1 Add "r" to InputManager ValidKeys set
    - Add `"r"` to the `ValidKeys` HashSet in `InputManager.cs`
    - No interface changes needed — `IsKeyPressed("r")` works via existing contract
    - _Requirements: 9.1, 9.2, 9.3_

- [x] 5. Integrate AmmoSystem into GameLoop
  - [x] 5.1 Handle reload input in GameLoop.Tick Playing phase
    - In the Playing phase of `Tick()`, after reading movement direction, check `_inputManager.IsKeyPressed("r")`
    - If pressed, call `_gameState.AmmoSystem.StartReload()`
    - _Requirements: 3.1, 3.2, 3.3, 9.2_

  - [x] 5.2 Cancel reload on player death
    - In the `Tick()` method, when detecting `Player.Health <= 0`, call `_gameState.AmmoSystem.CancelReload()` before transitioning to GameOver phase
    - _Requirements: 5.3_

  - [x] 5.3 Reset AmmoSystem on game restart
    - In `RestartGame()`, add `_gameState.AmmoSystem.Reset()` call
    - _Requirements: 1.3_

- [x] 6. Checkpoint - Verify input and game loop integration
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Add renderer support for Ammo HUD
  - [x] 7.1 Pass ammo data from CanvasRenderer to JavaScript
    - Modify `CanvasRenderer.RenderAsync()` to pass `AmmoSystem.CurrentAmmo`, `AmmoSystem.MaxAmmo`, `AmmoSystem.IsReloading`, and `AmmoSystem.ReloadProgress` as additional parameters to the `renderFrame` JS call
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 7.2 Implement ammo HUD rendering in JavaScript
    - In `gameInterop.js` `renderFrame` function, accept the new ammo parameters
    - Draw text `"{current}/{max}"` in the bottom-right corner of the canvas
    - Use a font size of at least 16px, white fill color
    - Position within 32px margin from right and bottom canvas edges
    - Only render during playing phase (already implied by RenderAsync only being called in Playing)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 8. Add renderer support for Reload Progress Bar
  - [x] 8.1 Implement reload progress bar rendering in JavaScript
    - In `gameInterop.js` `renderFrame` function, when `isReloading` is true, draw a progress bar
    - Center the bar horizontally above the player character, offset 20px above the player's top edge
    - Bar dimensions: 40px wide, 6px tall
    - Fill from left to right based on reload progress ratio
    - Use a background color (dark) for the unfilled area and a contrasting color (bright) for the filled area
    - Account for camera offset when positioning relative to the player in world space
    - Do not render when `isReloading` is false
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [x] 9. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests target the AmmoSystem class directly as a pure state machine (ideal for PBT)
- The design uses C# throughout — all implementation uses .NET 8 / Blazor WebAssembly
- Checkpoints ensure incremental validation after each major integration point
- The JavaScript rendering tasks (7.2, 8.1) modify `wwwroot/js/gameInterop.js`

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5", "1.6", "1.7", "1.8", "1.9", "1.10", "1.11", "2.1"] },
    { "id": 2, "tasks": ["2.2", "4.1"] },
    { "id": 3, "tasks": ["5.1", "5.2", "5.3"] },
    { "id": 4, "tasks": ["7.1"] },
    { "id": 5, "tasks": ["7.2", "8.1"] }
  ]
}
```
