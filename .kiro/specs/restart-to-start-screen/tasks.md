# Implementation Plan: Restart to Start Screen & Frog Sprite Display Fix

## Overview

Two targeted bug fixes in `GameLoop.cs`: change `RestartGame()` to navigate to `StartScreen` instead of `Playing`, and remove the render-once gate for the start screen phase so frog sprites appear after async load.

## Tasks

- [x] 1. Fix RestartGame to navigate to StartScreen
  - In `GameLoop.cs`, change `_currentPhase = GamePhase.Playing` to `_currentPhase = GamePhase.StartScreen` at the end of `RestartGame()`
  - Ensure `_lastRenderedPhase = null` is set before the phase assignment (already present)
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 2. Remove render-once gate for StartScreen phase
  - In `GameLoop.cs` `Tick()` method, remove the `_lastRenderedPhase != _currentPhase` condition from the StartScreen rendering block
  - The start screen should call `RenderStartScreenAsync` every tick unconditionally (when phase is still StartScreen after input handling)
  - _Requirements: 2.1, 2.2_

- [x] 3. Checkpoint - Verify both fixes
  - Build the project with `dotnet build` to confirm no compilation errors
  - Ensure all tests pass, ask the user if questions arise.
