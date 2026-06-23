# Implementation Plan: Start Screen

## Overview

Implement a canvas-rendered start screen for Frogmageddon that displays before gameplay begins. The game loop initializes into a StartScreen phase, renders the title and button on the canvas via JS interop, then transitions to Playing when the player clicks the start button or presses Enter. All rendering happens on the existing 800×600 canvas — no HTML overlays are introduced.

## Tasks

- [x] 1. Add GamePhase enum and StartButtonBounds model
  - [x] 1.1 Create the `GamePhase` enum in `Game/Models/GamePhase.cs`
    - Define `StartScreen` and `Playing` values
    - _Requirements: 6.1_
  - [x] 1.2 Create the `StartButtonBounds` record in `Game/Models/StartButtonBounds.cs`
    - Implement `Contains(float clickX, float clickY)` for hit detection
    - Implement `static Create(int canvasWidth, int canvasHeight)` factory method with 160×50 button dimensions centered horizontally, positioned below vertical center
    - _Requirements: 2.3, 3.3_
  - [x]* 1.3 Write property test for StartButtonBounds horizontal centering
    - **Property 1: Start button is horizontally centered**
    - **Validates: Requirements 2.3**
  - [x]* 1.4 Write property test for click hit detection
    - **Property 2: Click hit detection determines state transition**
    - **Validates: Requirements 3.1, 3.3**

- [x] 2. Extend InputManager for Enter key and mouse clicks
  - [x] 2.1 Update `IInputManager` interface with `SetMouseClick` and `ConsumePendingClick` methods
    - Add `void SetMouseClick(float x, float y)` to the interface
    - Add `(float X, float Y)? ConsumePendingClick()` to the interface
    - _Requirements: 3.3, 4.1_
  - [x] 2.2 Update `InputManager` to accept Enter key and store mouse click state
    - Add `"enter"` to `ValidKeys` set
    - Add `_pendingClick` field and implement `SetMouseClick` / `ConsumePendingClick`
    - _Requirements: 3.3, 4.1_
  - [x]* 2.3 Write unit tests for InputManager mouse click and Enter key handling
    - Test `SetMouseClick` stores coordinates and `ConsumePendingClick` retrieves and clears them
    - Test Enter key is accepted by `SetKeyDown` and reported by `IsKeyPressed`
    - _Requirements: 3.3, 4.1_

- [x] 3. Update GameLoop with phase-based branching
  - [x] 3.1 Add phase state machine to `GameLoop`
    - Add `_currentPhase` field initialized to `GamePhase.StartScreen`
    - Add `_buttonBounds` field computed in `InitializeAsync`
    - Add `CurrentPhase` property
    - Render start screen immediately after initialization
    - _Requirements: 5.1, 6.1_
  - [x] 3.2 Update `Tick` method to branch on current phase
    - In `StartScreen` phase: call `HandleStartScreenInput()` then render start screen
    - In `Playing` phase: process movement input, update state, render gameplay
    - _Requirements: 5.2, 6.1_
  - [x] 3.3 Implement `HandleStartScreenInput` and `TransitionToPlaying` methods
    - Check Enter key and mouse click for transition trigger
    - On transition: set phase to Playing, center player position
    - _Requirements: 3.1, 4.1, 6.2_
  - [x] 3.4 Add `[JSInvokable] OnMouseClick` method to GameLoop
    - Forward mouse click coordinates to `_inputManager.SetMouseClick`
    - _Requirements: 3.3_
  - [x]* 3.5 Write property test for Enter key ignored during Playing phase
    - **Property 3: Enter key is ignored during Playing phase**
    - **Validates: Requirements 4.3**
  - [x]* 3.6 Write property test for no player movement during StartScreen phase
    - **Property 4: No player movement during StartScreen phase**
    - **Validates: Requirements 5.2**
  - [x]* 3.7 Write property test for player centering on transition
    - **Property 5: Player initializes at canvas center on transition**
    - **Validates: Requirements 6.2**

- [x] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement start screen rendering
  - [x] 5.1 Update `IRenderer` interface with `RenderStartScreenAsync` method
    - Add `Task RenderStartScreenAsync(int canvasWidth, int canvasHeight, StartButtonBounds buttonBounds)` signature
    - _Requirements: 2.1, 2.5_
  - [x] 5.2 Implement `RenderStartScreenAsync` in `CanvasRenderer`
    - Call JS interop function `drawStartScreen` with canvas reference and button bounds parameters
    - _Requirements: 2.1, 2.3, 2.5_
  - [x] 5.3 Add `drawStartScreen` function to `gameInterop.js`
    - Clear canvas, draw "Frogmageddon" title text centered above vertical center
    - Draw filled button rectangle with border and "Start" text centered within
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
  - [x] 5.4 Add mouse click event listener in `initializeGame` function
    - Register click listener on canvas that computes coordinates relative to canvas
    - Invoke `OnMouseClick` on dotNetRef with computed (x, y) coordinates
    - _Requirements: 3.3_

- [x] 6. Update index.html and GamePage.razor
  - [x] 6.1 Remove loading spinner from `index.html`
    - Remove SVG progress indicator and loading text from `<div id="app">`
    - Leave the div empty so the page loads to a black screen
    - _Requirements: 1.1, 1.2, 1.3_
  - [x] 6.2 Verify `GamePage.razor` works with start screen flow
    - No auto-start occurs because `GameLoop.Tick` only renders start screen until player triggers transition
    - Ensure no code changes needed beyond what GameLoop handles
    - _Requirements: 5.1, 6.3_

- [x] 7. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests use FsCheck.Xunit (already in test project) to validate universal correctness properties
- Unit tests validate specific examples and edge cases
- The project uses C# with Blazor WebAssembly, xUnit, FsCheck, and bUnit

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "1.4", "2.1"] },
    { "id": 2, "tasks": ["2.2", "5.1"] },
    { "id": 3, "tasks": ["2.3", "3.1", "5.2"] },
    { "id": 4, "tasks": ["3.2", "3.3", "3.4", "5.3", "5.4"] },
    { "id": 5, "tasks": ["3.5", "3.6", "3.7", "6.1", "6.2"] }
  ]
}
```
