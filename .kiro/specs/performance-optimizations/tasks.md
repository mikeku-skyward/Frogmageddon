# Implementation Plan: Performance Optimizations

## Overview

This plan implements six targeted performance optimizations for the Frogmageddon Blazor WebAssembly game. Tasks are ordered so that foundational types (RenderBuffer, ObjectPool, IPoolable) are created first, then consuming code is modified incrementally. Each optimization is self-contained enough to validate independently at checkpoints.

## Tasks

- [x] 1. Create foundational types and interfaces
  - [x] 1.1 Create the `RenderBuffer` class in `src/BlazorAsteroids/Game/Engine/RenderBuffer.cs`
    - Implement a grow-only float buffer with `EnsureCapacity(int required)` that doubles capacity when exceeded
    - Expose `float[] Data` property and never shrink the backing array
    - Initial capacity accepted via constructor parameter
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 1.2 Create the `IPoolable` interface in `src/BlazorAsteroids/Game/Models/IPoolable.cs`
    - Define a single `void Reset()` method for reinitializing pooled objects
    - _Requirements: 6.2, 6.3_

  - [x] 1.3 Create the `ObjectPool<T>` class in `src/BlazorAsteroids/Game/Models/ObjectPool.cs`
    - Generic pool using `Stack<T>` with `Acquire()` and `Release(T instance)` methods
    - `Acquire` returns from pool if available, otherwise `new T()`
    - `Release` guards against null, pushes instance onto stack
    - No maximum capacity limit
    - Expose `int Count` property
    - _Requirements: 6.5, 6.6_

  - [x] 1.4 Write property tests for `RenderBuffer` (Properties 1 and 2)
    - **Property 1: Buffer capacity is monotonically non-decreasing and always sufficient**
    - **Property 2: Buffer identity is preserved when capacity is sufficient**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.5**

  - [x] 1.5 Write property tests for `ObjectPool<T>` (Property 8)
    - **Property 8: Pool acquire-release round-trip preserves count**
    - **Validates: Requirements 6.5, 6.6**

- [x] 2. Implement pre-allocated render buffers in CanvasRenderer
  - [x] 2.1 Modify `CanvasRenderer` to use `RenderBuffer` instances
    - Add `_frogBuffer` and `_bulletBuffer` fields (initialized in constructor with reasonable initial capacity)
    - In `RenderAsync`, call `EnsureCapacity(state.Frogs.Count * 5)` and `EnsureCapacity(state.Bullets.Count * 3)`
    - Populate the existing buffer arrays in-place and pass `_frogBuffer.Data` with an explicit count to JS interop
    - Remove the `new float[...]` allocations currently in `RenderAsync`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [x] 2.2 Update JavaScript `renderFrame` to accept entity counts
    - Modify `renderFrame` in `gameInterop.js` to accept `frogCount` and `bulletCount` parameters
    - Use only the first `frogCount * 5` elements of `frogData` and first `bulletCount * 3` elements of `bulletData`
    - _Requirements: 1.4_

  - [x] 2.3 Write property test for render data count (Property 3)
    - **Property 3: Render data count matches entity count**
    - **Validates: Requirements 1.4**

- [x] 3. Implement cached offscreen canvas and bounding rect in JavaScript
  - [x] 3.1 Add offscreen canvas cache to `gameInterop.js`
    - Declare module-level `_offscreenCanvas` and `_offscreenCtx` variables
    - In `initializeGame`, create the offscreen canvas element once
    - In `drawPlayerSprite`, reuse `_offscreenCanvas` instead of `document.createElement('canvas')`
    - Resize cached canvas only when required dimensions exceed current dimensions
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 3.2 Add bounding rect cache to `gameInterop.js`
    - Declare module-level `_cachedRect` variable
    - In `initializeGame`, compute and store `canvasElement.getBoundingClientRect()`
    - Add a `resize` event listener on `window` that invalidates and recomputes `_cachedRect`
    - Replace `getBoundingClientRect()` calls in click and mousemove handlers with `_cachedRect`
    - Fall back to direct call if `_cachedRect` is null
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 4. Checkpoint - Validate render buffer and JS caching changes
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement static empty list in FrogSpawner
  - [x] 5.1 Modify `FrogSpawner.TrySpawn` to return cached empty list
    - Add `private static readonly IReadOnlyList<Frog> EmptyFrogList = Array.Empty<Frog>();`
    - Change return type from `List<Frog>` to `IReadOnlyList<Frog>`
    - Return `EmptyFrogList` on all no-spawn paths (timer not expired, at max capacity)
    - Keep `new List<Frog>(groupSize)` for actual spawn paths
    - Update `GameState.Update` to work with `IReadOnlyList<Frog>` (it already checks `.Count > 0`)
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

  - [x] 5.2 Write property test for FrogSpawner empty list identity (Property 6)
    - **Property 6: FrogSpawner returns static empty instance if and only if no frogs are spawned**
    - **Validates: Requirements 4.1, 4.3**

- [x] 6. Implement render-once for static screens in GameLoop
  - [x] 6.1 Modify `GameLoop` to skip redundant static screen renders
    - Add `private GamePhase? _lastRenderedPhase` field
    - In `Tick`, for StartScreen/GameOver/Paused: only call render if `_lastRenderedPhase != _currentPhase`
    - After rendering, set `_lastRenderedPhase = _currentPhase`
    - On transition to Playing (in `TransitionToPlaying` and resume from Paused), set `_lastRenderedPhase = null`
    - On `RestartGame`, set `_lastRenderedPhase = null`
    - Continue processing input normally during static phases regardless of render skip
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 6.2 Write property test for static screen render-once (Property 7)
    - **Property 7: Static screen render count stays at one regardless of tick count**
    - **Validates: Requirements 5.2, 5.3**

- [x] 7. Checkpoint - Validate FrogSpawner and render-once changes
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Implement object pooling for Bullets and Frogs
  - [x] 8.1 Add pooling support to `Bullet` class
    - Add a parameterless constructor `Bullet()` that creates an uninitialized instance
    - Add `Initialize(Vector2 startPosition, Vector2 direction)` method that sets Position, PreviousPosition, Direction (normalized), Lifetime = 0, IsAlive = true
    - Existing constructor delegates to `Initialize` for backward compatibility
    - Implement `IPoolable` interface with `Reset()` that sets `IsAlive = false`
    - _Requirements: 6.2, 6.7_

  - [x] 8.2 Add pooling support to `Frog` class
    - Add a parameterless constructor `Frog()` that creates an uninitialized instance
    - Add `Initialize(Vector2 spawnPosition, float rotation)` method that sets Position, Rotation, IsAlive = true, state to Sitting, resets timers
    - Existing constructor delegates to `Initialize` for backward compatibility
    - Implement `IPoolable` interface with `Reset()` that sets `IsAlive = false`
    - _Requirements: 6.3, 6.7_

  - [x] 8.3 Integrate object pools into `GameState`
    - Add `ObjectPool<Bullet> BulletPool` and `ObjectPool<Frog> FrogPool` fields, initialized in constructor
    - Modify `FireBullet` to acquire from `BulletPool` and call `Initialize` instead of `new Bullet(...)`
    - Modify frog spawning: acquire frogs from `FrogPool` and call `Initialize` instead of `new Frog(...)`
    - Update `FrogSpawner.TrySpawn` to accept the `ObjectPool<Frog>` (or return spawn parameters and let `GameState` create from pool)
    - Modify dead entity removal to return instances to their pools before removing from lists
    - Update `RestartGame` in `GameLoop` to clear lists and return all instances to pools
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.7_

  - [x] 8.4 Write property tests for pooled bullet initialization (Property 9)
    - **Property 9: Pooled bullet initialization preserves fire parameters**
    - **Validates: Requirements 6.2**

  - [x] 8.5 Write property tests for pooled frog initialization (Property 10)
    - **Property 10: Pooled frog initialization preserves spawn parameters**
    - **Validates: Requirements 6.3**

  - [x] 8.6 Write property test for dead entity pool return (Property 11)
    - **Property 11: Dead entities are returned to pool after cleanup**
    - **Validates: Requirements 6.4**

- [x] 9. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The test project at `tests/BlazorAsteroids.Tests/` already has FsCheck.Xunit 3.3.3 and xUnit configured
- JavaScript tests (Properties 4 and 5 — offscreen canvas dimensions and bounding rect equivalence) are deferred to manual or integration testing since no JS test runner is currently configured in the project

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["1.4", "1.5", "2.1", "3.1", "3.2", "5.1"] },
    { "id": 2, "tasks": ["2.2", "5.2", "6.1"] },
    { "id": 3, "tasks": ["2.3", "6.2", "8.1", "8.2"] },
    { "id": 4, "tasks": ["8.3"] },
    { "id": 5, "tasks": ["8.4", "8.5", "8.6"] }
  ]
}
```
