# Implementation Plan: Blazor Asteroids Game

## Overview

This plan implements a top-down 2D Asteroids-style game in Blazor WebAssembly. The player ship is rendered on an HTML5 Canvas with WASD movement, using a game loop driven by `requestAnimationFrame` via JS interop. All game state and logic is managed in C#. The implementation proceeds from data models and interfaces, through core engine components, to JS interop wiring and rendering.

## Tasks

- [x] 1. Set up project structure and core data models
  - [x] 1.1 Create project structure and install dependencies
    - Create the Blazor WebAssembly project (if not already present) with folders: `Game/`, `Game/Models/`, `Game/Interfaces/`, `Game/Engine/`, `wwwroot/js/`
    - Add NuGet packages: `xUnit`, `FsCheck.Xunit`, `bUnit` to a test project
    - _Requirements: 1.1_

  - [x] 1.2 Implement Vector2 struct
    - Create `Game/Models/Vector2.cs` with X, Y properties
    - Implement `Length()`, `Normalized()`, operator `+`, and operator `*` (scalar)
    - Ensure normalization returns zero vector for zero-length input
    - _Requirements: 3.4_

  - [x] 1.3 Implement Player class
    - Create `Game/Models/Player.cs` with Position (Vector2), Speed (float), Size (float), Rotation (float)
    - _Requirements: 1.2, 1.3_

  - [x] 1.4 Implement GameState class
    - Create `Game/Models/GameState.cs` with Player, CanvasWidth, CanvasHeight properties
    - _Requirements: 1.2, 4.1_

  - [x] 1.5 Define core interfaces
    - Create `Game/Interfaces/IGameLoop.cs` with `InitializeAsync`, `Tick`, `Stop`
    - Create `Game/Interfaces/IInputManager.cs` with `SetKeyDown`, `SetKeyUp`, `GetMovementDirection`, `IsKeyPressed`
    - Create `Game/Interfaces/IRenderer.cs` with `InitializeAsync`, `RenderAsync`, `ClearAsync`
    - Create `Game/Interfaces/IGameState.cs` with Player, CanvasWidth, CanvasHeight, `Update`
    - _Requirements: 2.1, 3.1, 6.1_

- [x] 2. Implement InputManager
  - [x] 2.1 Implement InputManager class
    - Create `Game/Engine/InputManager.cs` implementing `IInputManager`
    - Maintain a `HashSet<string>` of currently pressed keys
    - `SetKeyDown` converts key to lowercase, only adds if it is "w", "a", "s", or "d"
    - `SetKeyUp` converts key to lowercase, removes from set
    - Validate that key parameter is a non-empty string before processing
    - _Requirements: 3.1, 3.2, 8.1, 8.3_

  - [x] 2.2 Implement GetMovementDirection
    - Sum directional contributions: W=-Y, S=+Y, A=-X, D=+X
    - Normalize result so length never exceeds 1.0
    - Return (0,0) when no WASD keys are pressed
    - Handle opposing keys (W+S, A+D) canceling to zero on contested axis
    - _Requirements: 3.3, 3.4, 3.5, 3.6_

  - [ ]* 2.3 Write property test: Normalized Movement (Property 2)
    - **Property 2: Normalized Movement**
    - For any combination of WASD key states, verify `GetMovementDirection()` returns a Vector2 with Length() <= 1.0
    - **Validates: Requirements 3.4**

  - [ ]* 2.4 Write property test: No Movement Without Input (Property 3)
    - **Property 3: No Movement Without Input**
    - When no WASD keys are pressed, verify `GetMovementDirection()` returns (0, 0)
    - **Validates: Requirements 3.5, 4.3**

  - [ ]* 2.5 Write property test: Unrecognized Keys Ignored (Property 7)
    - **Property 7: Unrecognized Keys Ignored**
    - For any key string that is not "w", "a", "s", or "d", calling `SetKeyDown` does not affect `GetMovementDirection()` output
    - **Validates: Requirements 8.1**

  - [ ]* 2.6 Write property test: Key Combination Direction (Property 8)
    - **Property 8: Key Combination Direction**
    - For any subset of WASD keys pressed, verify the direction vector components reflect the sum of individual key contributions before normalization
    - **Validates: Requirements 3.3**

- [x] 3. Checkpoint - Verify data models and input
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement GameState update logic
  - [x] 4.1 Implement GameState.Update method
    - Apply movement: `Position += direction * speed * deltaTime`
    - Clamp position to [Size, CanvasWidth - Size] on X and [Size, CanvasHeight - Size] on Y
    - Do not change position when direction is zero
    - Update rotation to `atan2(direction.Y, direction.X)` when direction is non-zero; preserve rotation otherwise
    - _Requirements: 4.1, 4.2, 4.3, 5.1, 5.2_

  - [ ]* 4.2 Write property test: Boundary Invariant (Property 1)
    - **Property 1: Boundary Invariant**
    - For any player position, speed, movement direction, and delta time, after `Update()`, position X is in [Size, CanvasWidth - Size] and Y is in [Size, CanvasHeight - Size]
    - **Validates: Requirements 4.2**

  - [ ]* 4.3 Write property test: Frame Independence (Property 4)
    - **Property 4: Frame Independence**
    - For any direction and total time, the final position is the same whether the interval is divided into N or M frames (when no boundary clamping occurs)
    - **Validates: Requirements 4.4**

  - [ ]* 4.4 Write property test: Rotation Consistency (Property 5)
    - **Property 5: Rotation Consistency**
    - If direction has non-zero length, rotation equals `atan2(direction.Y, direction.X)` after update; if direction is zero, rotation is unchanged
    - **Validates: Requirements 5.1, 5.2**

- [x] 5. Implement Renderer
  - [x] 5.1 Implement CanvasRenderer class
    - Create `Game/Engine/CanvasRenderer.cs` implementing `IRenderer`
    - `InitializeAsync`: Store canvas reference and get JS module reference
    - `ClearAsync`: Call JS interop to clear entire canvas
    - `RenderAsync`: Clear canvas, then draw player as triangle at position with rotation and size
    - Ensure rendering does not modify GameState
    - _Requirements: 6.1, 6.2, 6.3_

  - [ ]* 5.2 Write property test: Rendering Purity (Property 6)
    - **Property 6: Rendering Purity**
    - For any game state, calling `RenderAsync(state)` does not modify any field of the game state
    - **Validates: Requirements 6.3**

- [x] 6. Implement GameLoop
  - [x] 6.1 Implement GameLoop class
    - Create `Game/Engine/GameLoop.cs` implementing `IGameLoop`
    - `InitializeAsync`: Initialize renderer, set up JS interop module, invoke `initializeGame` passing DotNetObjectReference
    - `Tick(float deltaTimeMs)`: Convert to seconds, clamp to MAX_DELTA_TIME (0.1s), validate non-negative, read input, update state, render
    - `Stop`: Set running flag to false
    - Mark `Tick`, `SetKeyDown`, `SetKeyUp` as `[JSInvokable]`
    - Validate deltaTimeMs is non-negative; discard frame if invalid
    - _Requirements: 2.1, 2.2, 2.3, 7.1, 8.2_

  - [ ]* 6.2 Write unit tests for GameLoop
    - Test delta time clamping when value exceeds 0.1 seconds
    - Test that negative deltaTimeMs discards the frame
    - Test that update/render order is maintained
    - _Requirements: 2.1, 2.2, 7.1, 8.2_

- [x] 7. Checkpoint - Verify core engine logic
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Implement JavaScript interop layer
  - [x] 8.1 Create gameInterop.js module
    - Create `wwwroot/js/gameInterop.js`
    - Implement `initializeGame(canvasElement, dotNetRef)`: get 2D context, register keydown/keyup listeners that invoke `SetKeyDown`/`SetKeyUp` on dotNetRef, start requestAnimationFrame loop that invokes `Tick` with delta time
    - Convert key to lowercase in JS before sending to C#
    - _Requirements: 1.1, 2.3, 3.1, 3.2_

  - [x] 8.2 Create canvas rendering JS functions
    - Implement `clearCanvas(canvasElement)` to clear the full canvas
    - Implement `drawPlayer(canvasElement, x, y, rotation, size)` to draw a triangle ship shape at position with rotation
    - Export all functions as ES module
    - _Requirements: 6.1, 6.2_

  - [x] 8.3 Add error handling to JS interop
    - Handle canvas context loss: listen for `contextlost` and `contextrestored` events
    - On context lost: pause rendering loop
    - On context restored: re-initialize context and resume
    - Wrap initialization in try/catch; surface errors to C# caller
    - _Requirements: 7.2, 7.3_

- [x] 9. Implement Blazor game page component
  - [x] 9.1 Create GamePage.razor component
    - Create `Pages/GamePage.razor` with route `/game`
    - Add `<canvas>` element with 800x600 dimensions, `tabindex="0"` for keyboard focus
    - In `OnAfterRenderAsync(firstRender)`: instantiate InputManager, CanvasRenderer, GameState (player centered at 400,300, speed=200, size=15, rotation=0), GameLoop; call `InitializeAsync`
    - Display error message with retry button if initialization fails
    - _Requirements: 1.1, 1.2, 1.3, 5.3, 7.3_

  - [ ]* 9.2 Write integration tests with bUnit
    - Test that the game page renders a canvas element
    - Test that initialization is invoked on first render
    - Test error display when JS interop fails
    - _Requirements: 1.1, 7.3_

- [x] 10. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The implementation language is C# with Blazor WebAssembly
- FsCheck with xUnit integration is used for property-based testing
- bUnit is used for Blazor component integration testing

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5"] },
    { "id": 2, "tasks": ["2.1"] },
    { "id": 3, "tasks": ["2.2"] },
    { "id": 4, "tasks": ["2.3", "2.4", "2.5", "2.6"] },
    { "id": 5, "tasks": ["4.1"] },
    { "id": 6, "tasks": ["4.2", "4.3", "4.4"] },
    { "id": 7, "tasks": ["5.1"] },
    { "id": 8, "tasks": ["5.2", "6.1"] },
    { "id": 9, "tasks": ["6.2", "8.1", "8.2"] },
    { "id": 10, "tasks": ["8.3"] },
    { "id": 11, "tasks": ["9.1"] },
    { "id": 12, "tasks": ["9.2"] }
  ]
}
```
