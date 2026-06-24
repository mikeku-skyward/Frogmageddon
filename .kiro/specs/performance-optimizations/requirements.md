# Requirements Document

## Introduction

This spec covers internal performance optimizations for the Frogmageddon Blazor WebAssembly game. All changes reduce per-frame memory allocations and unnecessary rendering work without altering gameplay behavior, visuals, scoring, physics, or collision detection. The game targets .NET 10 / Blazor WebAssembly with JavaScript interop via HTML5 Canvas.

## Glossary

- **CanvasRenderer**: The C# class (`CanvasRenderer.cs`) responsible for marshalling game state into flat arrays and invoking JavaScript rendering functions via JS interop.
- **GameLoop**: The C# class (`GameLoop.cs`) that orchestrates per-frame updates, input handling, and rendering dispatch based on the current game phase.
- **FrogSpawner**: The C# class (`FrogSpawner.cs`) that determines when and where to spawn new frog enemies.
- **GameState**: The C# class (`GameState.cs`) that holds all mutable game state including frog and bullet lists.
- **gameInterop**: The JavaScript module (`gameInterop.js`) that performs canvas rendering and handles browser input events.
- **Render_Buffer**: A pre-allocated float array that grows as needed but never shrinks, used to pass entity data to JavaScript.
- **Object_Pool**: A collection of pre-instantiated objects that are reused rather than allocated and garbage-collected each lifecycle.
- **Static_Screen**: A game phase screen (Start, GameOver, Paused) whose visual content does not change between frames.
- **Bounding_Rect_Cache**: A cached result of `getBoundingClientRect()` that is invalidated only on window resize.
- **Offscreen_Canvas_Cache**: A reusable offscreen canvas element allocated once at initialization for compositing the damage flash effect.
- **GC_Pressure**: The rate at which short-lived objects are allocated and subsequently collected by the garbage collector, causing frame-time spikes.

## Requirements

### Requirement 1: Pre-allocated Render Buffers

**User Story:** As a player, I want smooth frame rates without GC-induced stutters, so that gameplay feels responsive on lower-end hardware.

#### Acceptance Criteria

1. THE CanvasRenderer SHALL maintain a persistent frog data Render_Buffer and a persistent bullet data Render_Buffer across frames.
2. WHEN the number of frogs or bullets exceeds the current Render_Buffer capacity, THE CanvasRenderer SHALL grow the Render_Buffer to accommodate the new count.
3. THE CanvasRenderer SHALL NOT shrink a Render_Buffer when the entity count decreases.
4. WHEN RenderAsync is called, THE CanvasRenderer SHALL populate the Render_Buffer with entity data and pass only the filled portion to JavaScript.
5. WHEN RenderAsync is called, THE CanvasRenderer SHALL NOT allocate a new array for frog data or bullet data.

### Requirement 2: Cached Offscreen Canvas for Damage Flash

**User Story:** As a player, I want the damage flash effect to render without per-frame allocations, so that taking hits does not cause frame drops.

#### Acceptance Criteria

1. WHEN gameInterop initializes, THE gameInterop module SHALL allocate a single offscreen canvas element for damage flash compositing.
2. WHEN drawPlayerSprite is called with isFlashing true, THE gameInterop module SHALL reuse the cached Offscreen_Canvas_Cache instead of creating a new canvas element.
3. WHEN the cached offscreen canvas dimensions are smaller than required, THE gameInterop module SHALL resize the cached canvas to fit.
4. THE gameInterop module SHALL produce the same visual output for the damage flash as the current implementation.

### Requirement 3: Cached Bounding Rect for Mouse Events

**User Story:** As a player, I want mouse input to remain accurate without triggering expensive layout recalculations every frame, so that aiming feels smooth.

#### Acceptance Criteria

1. WHEN initializeGame is called, THE gameInterop module SHALL compute and cache the canvas bounding rectangle.
2. WHEN a mousemove or click event occurs, THE gameInterop module SHALL use the Bounding_Rect_Cache to compute canvas-relative coordinates instead of calling getBoundingClientRect.
3. WHEN the window is resized, THE gameInterop module SHALL invalidate and recompute the Bounding_Rect_Cache.
4. THE gameInterop module SHALL produce the same canvas-relative coordinates as calling getBoundingClientRect directly.

### Requirement 4: Static Empty List from FrogSpawner

**User Story:** As a player, I want the game to avoid allocating throwaway lists on frames where no frogs spawn, so that GC pressure remains low during normal gameplay.

#### Acceptance Criteria

1. WHEN FrogSpawner.TrySpawn determines that no frogs should spawn (timer not expired or at max capacity), THE FrogSpawner SHALL return a cached static empty list rather than allocating a new list.
2. THE returned static empty list SHALL be read-only to prevent accidental mutation by callers.
3. WHEN frogs are spawned, THE FrogSpawner SHALL return a newly allocated list containing the spawned frogs.
4. THE FrogSpawner SHALL produce the same spawn timing, positions, and quantities as the current implementation.

### Requirement 5: Render Static Screens Only on Phase Transition

**User Story:** As a player, I want static menu screens to avoid unnecessary rendering work, so that my laptop's battery and thermal headroom are preserved.

#### Acceptance Criteria

1. WHEN the game transitions to a Static_Screen phase (StartScreen, GameOver, Paused), THE GameLoop SHALL render that screen exactly once.
2. WHILE the game remains in a Static_Screen phase with no phase change, THE GameLoop SHALL NOT re-invoke the render function for that screen on subsequent frames.
3. WHEN user input occurs during a Static_Screen phase, THE GameLoop SHALL continue processing input normally without triggering a re-render unless the phase changes.
4. WHEN the game transitions from a Static_Screen phase to Playing, THE GameLoop SHALL resume per-frame rendering of the game state.

### Requirement 6: Object Pooling for Bullets and Frogs

**User Story:** As a player, I want bullets and frogs to be recycled from a pool, so that frequent spawning and death cycles do not cause GC pauses.

#### Acceptance Criteria

1. THE GameState SHALL maintain an Object_Pool for Bullet instances and an Object_Pool for Frog instances.
2. WHEN a new bullet is fired, THE GameState SHALL acquire a Bullet from the Object_Pool and initialize it with the new position and direction.
3. WHEN a new frog is spawned, THE GameState SHALL acquire a Frog from the Object_Pool and initialize it with the new position and rotation.
4. WHEN a bullet or frog is marked as dead, THE GameState SHALL return the instance to its Object_Pool for future reuse instead of allowing it to be garbage collected.
5. WHEN the Object_Pool is empty and a new instance is needed, THE Object_Pool SHALL allocate a new instance.
6. THE Object_Pool SHALL NOT impose a maximum capacity limit on pooled instances.
7. THE game SHALL produce identical gameplay behavior (collision detection, scoring, physics, spawn timing) with pooling enabled as without pooling.
