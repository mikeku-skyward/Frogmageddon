# Implementation Plan: Frog Group Spawning

## Overview

Modify the frog spawning system to spawn groups of 5–7 clustered frogs per spawn event instead of one at a time, reduce the base spawn interval from 3.0s to 2.0s, raise MaxFrogs from 30 to 40, and update GameState to handle the new `List<Frog>` return type.

## Tasks

- [x] 1. Modify FrogSpawner to support group spawning
  - [x] 1.1 Update FrogSpawner constants and return type
    - Change `BaseSpawnInterval` from `3.0f` to `2.0f`
    - Change `MaxFrogs` default from `30` to `40`
    - Add constants: `MinGroupSize = 5`, `MaxGroupSize = 7`, `ClusterRadius = 120f`, `AnchorCornerMargin = 120f`
    - Change `TrySpawn` signature from `Frog?` to `List<Frog>`
    - _Requirements: 2.1, 5.1, 4.3_

  - [x] 1.2 Implement group spawn and clustering logic in TrySpawn
    - Return empty list when `currentFrogCount >= MaxFrogs` (reset timer)
    - On timer elapse: pick `groupSize` uniformly in [5, 7], clamp to `MaxFrogs - currentFrogCount`
    - Select one random edge for the entire group
    - Compute anchor point along the edge, clamped at least 120px from corners (fallback to midpoint if edge too short)
    - Position each frog at random offset within ±120px of anchor along the edge axis, at SpawnMargin outside the viewport on the perpendicular axis
    - Return the list of spawned frogs
    - _Requirements: 1.1, 1.2, 1.3, 3.1, 3.2, 3.3, 3.4, 5.2, 5.3_

- [x] 2. Update GameState to integrate group spawning
  - [x] 2.1 Change GameState.Update spawn call site
    - Replace `var newFrog = FrogSpawner.TrySpawn(...)` / `if (newFrog != null) Frogs.Add(newFrog)` with `var newFrogs = FrogSpawner.TrySpawn(...)` / `if (newFrogs.Count > 0) Frogs.AddRange(newFrogs)`
    - _Requirements: 4.1, 4.2_

- [x] 3. Verify build and optional tests
  - [x] 3.1 Checkpoint - Verify build succeeds
    - Run `dotnet build` on the solution and ensure zero errors
    - Ensure all tests pass, ask the user if questions arise.

  - [ ]* 3.2 Write property tests for group spawning
    - **Property 1: Group size is always 5–7 (or capped remainder)**
    - **Property 2: MaxFrogs cap invariant**
    - **Property 4: All frogs spawn on same edge at SpawnMargin**
    - **Property 5: Cluster spread within 240px**
    - **Property 6: Anchor at least 120px from edge corners**
    - **Validates: Requirements 1.1, 1.2, 1.3, 2.3, 3.1, 3.2, 3.3, 3.4, 5.1, 5.2, 5.3**

  - [ ]* 3.3 Write unit tests for spawn rate and integration
    - **Property 3: Spawn rate multiplier formula**
    - **Property 7: GameState integrates all spawned frogs**
    - Verify `BaseSpawnInterval == 2.0f`, `MaxFrogs == 40`
    - **Validates: Requirements 2.1, 2.2, 4.1**

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Only two source files change: `FrogSpawner.cs` and `GameState.cs`
- The design uses `List<Frog>` (never null) to eliminate null-check ambiguity in the caller
- Property tests validate universal correctness properties from the design document
- Checkpoints ensure incremental validation

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2"] },
    { "id": 2, "tasks": ["2.1"] },
    { "id": 3, "tasks": ["3.1", "3.2", "3.3"] }
  ]
}
```
