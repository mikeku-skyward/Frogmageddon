# Design Document

## Overview

The start screen feature introduces a `GamePhase` state machine that controls whether the game loop renders the start screen or processes active gameplay. The current auto-start behavior in `GamePage.razor` is replaced with a two-phase approach: the game loop initializes into a `StartScreen` phase, renders the title and button on the canvas, then transitions to `Playing` when the player clicks the start button or presses Enter.

All rendering happens on the existing 800×600 canvas via JS interop — no HTML overlays are introduced. The `InputManager` is extended to handle Enter key presses and mouse click events, and the `GameLoop.Tick` method branches logic based on the current phase.

## Architecture

### Component Diagram

```
GamePage.razor
    └── GameLoop (owns phase state machine)
            ├── InputManager (keyboard + mouse input)
            ├── CanvasRenderer
            │       ├── RenderStartScreenAsync() ← new
            │       └── RenderAsync(GameState)   ← existing
            └── GameState (player, canvas dimensions)
```

### State Machine

```
┌─────────────┐   Enter key / Click in button   ┌──────────────┐
│ StartScreen  │ ──────────────────────────────► │   Playing    │
└─────────────┘                                  └──────────────┘
     │                                                  │
     │  Tick → render start screen only                 │  Tick → update + render gameplay
     │  Ignore WASD movement                            │  Ignore Enter as start trigger
```

## Components and Interfaces

### GamePhase Enum

A new enum representing the two distinct execution phases.

```csharp
namespace BlazorAsteroids.Game.Models;

public enum GamePhase
{
    StartScreen,
    Playing
}
```

### StartButtonBounds Record

Encapsulates the button hit-detection rectangle. Computed from canvas dimensions.

```csharp
namespace BlazorAsteroids.Game.Models;

public record StartButtonBounds(float X, float Y, float Width, float Height)
{
    public bool Contains(float clickX, float clickY)
    {
        return clickX >= X && clickX <= X + Width
            && clickY >= Y && clickY <= Y + Height;
    }

    public static StartButtonBounds Create(int canvasWidth, int canvasHeight)
    {
        const float buttonWidth = 160f;
        const float buttonHeight = 50f;
        float x = (canvasWidth - buttonWidth) / 2f;
        float y = (canvasHeight / 2f) + 40f; // Below vertical center
        return new StartButtonBounds(x, y, buttonWidth, buttonHeight);
    }
}
```

### InputManager Changes

Extend `InputManager` to accept Enter key and mouse click coordinates.

```csharp
public class InputManager : IInputManager
{
    private static readonly HashSet<string> ValidKeys = new() { "w", "a", "s", "d", "enter" };
    private readonly HashSet<string> _pressedKeys = new();
    private (float X, float Y)? _pendingClick;

    public void SetKeyDown(string key) { /* existing + now accepts "enter" */ }
    public void SetKeyUp(string key) { /* existing */ }

    public void SetMouseClick(float x, float y)
    {
        _pendingClick = (x, y);
    }

    public (float X, float Y)? ConsumePendingClick()
    {
        var click = _pendingClick;
        _pendingClick = null;
        return click;
    }

    public Vector2 GetMovementDirection() { /* existing */ }
    public bool IsKeyPressed(string key) { /* existing */ }
}
```

### IInputManager Interface Update

```csharp
public interface IInputManager
{
    void SetKeyDown(string key);
    void SetKeyUp(string key);
    Vector2 GetMovementDirection();
    bool IsKeyPressed(string key);
    void SetMouseClick(float x, float y);
    (float X, float Y)? ConsumePendingClick();
}
```

### GameLoop Changes

The `GameLoop` gains a `CurrentPhase` property and branches `Tick` logic accordingly.

```csharp
public class GameLoop : IGameLoop, IDisposable
{
    private GamePhase _currentPhase = GamePhase.StartScreen;
    private StartButtonBounds _buttonBounds;

    public GamePhase CurrentPhase => _currentPhase;

    public async Task InitializeAsync(ElementReference canvas)
    {
        // Existing initialization...
        _buttonBounds = StartButtonBounds.Create(_gameState.CanvasWidth, _gameState.CanvasHeight);
        _isRunning = true;

        // Render the start screen immediately after initialization
        await _renderer.RenderStartScreenAsync(_gameState.CanvasWidth, _gameState.CanvasHeight, _buttonBounds);
    }

    [JSInvokable]
    public void Tick(float deltaTimeMs)
    {
        if (deltaTimeMs < 0 || !_isRunning) return;

        float deltaTimeSec = MathF.Min(deltaTimeMs / 1000f, MAX_DELTA_TIME);

        if (_currentPhase == GamePhase.StartScreen)
        {
            HandleStartScreenInput();
            _ = _renderer.RenderStartScreenAsync(_gameState.CanvasWidth, _gameState.CanvasHeight, _buttonBounds);
        }
        else
        {
            Vector2 direction = _inputManager.GetMovementDirection();
            _gameState.Update(deltaTimeSec, direction);
            _ = _renderer.RenderAsync(_gameState);
        }
    }

    private void HandleStartScreenInput()
    {
        // Check Enter key
        if (_inputManager.IsKeyPressed("enter"))
        {
            TransitionToPlaying();
            return;
        }

        // Check mouse click
        var click = _inputManager.ConsumePendingClick();
        if (click.HasValue && _buttonBounds.Contains(click.Value.X, click.Value.Y))
        {
            TransitionToPlaying();
        }
    }

    private void TransitionToPlaying()
    {
        _currentPhase = GamePhase.Playing;
        _gameState.Player.Position = new Vector2(
            _gameState.CanvasWidth / 2f,
            _gameState.CanvasHeight / 2f);
    }

    [JSInvokable]
    public void OnMouseClick(float x, float y)
    {
        _inputManager.SetMouseClick(x, y);
    }
}
```

### IRenderer Interface Update

```csharp
public interface IRenderer
{
    Task InitializeAsync(ElementReference canvas);
    Task RenderAsync(GameState state);
    Task RenderStartScreenAsync(int canvasWidth, int canvasHeight, StartButtonBounds buttonBounds);
    Task ClearAsync();
}
```

### CanvasRenderer Changes

Add `RenderStartScreenAsync` that calls a new JS interop function.

```csharp
public async Task RenderStartScreenAsync(int canvasWidth, int canvasHeight, StartButtonBounds buttonBounds)
{
    if (_module is not null)
    {
        await _module.InvokeVoidAsync("drawStartScreen",
            _canvas, canvasWidth, canvasHeight,
            buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height);
    }
}
```

### gameInterop.js Changes

Add `drawStartScreen` function and mouse click listener.

```javascript
export function drawStartScreen(canvasElement, canvasWidth, canvasHeight, btnX, btnY, btnW, btnH) {
    const ctx = canvasElement.getContext('2d');
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);

    // Title text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 48px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Frogmageddon', canvasWidth / 2, canvasHeight / 2 - 60);

    // Start button rectangle
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, btnY, btnW, btnH);

    // Button border
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, btnY, btnW, btnH);

    // Button text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px monospace';
    ctx.fillText('Start', btnX + btnW / 2, btnY + btnH / 2);
}
```

Mouse click registration added to `initializeGame`:

```javascript
canvasElement.addEventListener('click', (e) => {
    const rect = canvasElement.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    dotNetRef.invokeMethodAsync('OnMouseClick', x, y);
});
```

### index.html Changes

Remove the loading spinner content from `<div id="app">`:

```html
<div id="app"></div>
```

The `#app` div remains empty so the page loads to a blank (black via CSS) screen until Blazor renders the canvas.

### GamePage.razor Changes

The `OnAfterRenderAsync` method still calls `InitializeGameAsync`, but the game loop now starts in `StartScreen` phase instead of immediately processing gameplay. No auto-start occurs because `GameLoop.Tick` only renders the start screen until the player triggers the transition.

## Data Models

### GamePhase

| Value | Description |
|-------|-------------|
| `StartScreen` | Game loop renders title + button, ignores movement input |
| `Playing` | Game loop processes input, updates state, renders player |

### StartButtonBounds

| Property | Type | Description |
|----------|------|-------------|
| `X` | float | Left edge x-coordinate |
| `Y` | float | Top edge y-coordinate |
| `Width` | float | Button width (160px) |
| `Height` | float | Button height (50px) |

## Error Handling

- **Mouse click outside canvas**: The `getBoundingClientRect()` calculation ensures coordinates are relative to the canvas. Clicks outside the canvas element don't fire the canvas click listener.
- **Rapid clicks/key presses during transition**: The phase check in `Tick` ensures that once `_currentPhase` is `Playing`, start-screen input handling is skipped entirely.
- **Negative deltaTime**: Existing guard in `Tick` discards the frame.
- **Canvas context loss**: Existing `contextlost`/`contextrestored` handlers in `gameInterop.js` continue to work — the start screen will simply re-render on restore.

## Testing Strategy

### Unit Tests

- Verify `StartButtonBounds.Contains` with specific in-bounds and out-of-bounds coordinates
- Verify `StartButtonBounds.Create` produces expected values for 800×600 canvas
- Verify `GameLoop.Tick` renders start screen when phase is `StartScreen`
- Verify `GameLoop.TransitionToPlaying` sets phase and player position
- Verify `InputManager.SetMouseClick` and `ConsumePendingClick` store and clear click state
- Verify Enter key is accepted by `InputManager.SetKeyDown`

### Property-Based Tests

- Button centering holds for arbitrary canvas widths
- Hit detection correctness for random (x, y) coordinates against computed button bounds
- Enter key press during Playing phase never changes phase
- WASD input during StartScreen phase never changes player position
- Player position equals canvas center after transition for arbitrary dimensions

### Integration Tests

- Full initialization flow: GamePage renders canvas → GameLoop starts in StartScreen → start screen is drawn
- Click on button transitions to gameplay and renders player
- Enter key press transitions to gameplay

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Start button is horizontally centered

*For any* canvas width, the start button's x-position SHALL equal `(canvasWidth - buttonWidth) / 2`, ensuring the button is always horizontally centered regardless of canvas dimensions.

**Validates: Requirements 2.3**

### Property 2: Click hit detection determines state transition

*For any* click coordinates (x, y) on the canvas while in StartScreen phase, the game SHALL transition to Playing if and only if the coordinates fall within the start button rectangle boundaries (x >= btnX AND x <= btnX + btnWidth AND y >= btnY AND y <= btnY + btnHeight).

**Validates: Requirements 3.1, 3.3**

### Property 3: Enter key is ignored during Playing phase

*For any* game state in the Playing phase, pressing the Enter key SHALL not change the game phase — the phase remains Playing.

**Validates: Requirements 4.3**

### Property 4: No player movement during StartScreen phase

*For any* movement input (WASD key combination) while the game is in StartScreen phase, the player's position and rotation SHALL remain unchanged after a Tick is processed.

**Validates: Requirements 5.2**

### Property 5: Player initializes at canvas center on transition

*For any* valid canvas dimensions (width, height), when the game transitions from StartScreen to Playing, the player's position SHALL be set to (width / 2, height / 2).

**Validates: Requirements 6.2**
