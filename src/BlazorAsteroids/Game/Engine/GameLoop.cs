using BlazorAsteroids.Game.Interfaces;
using BlazorAsteroids.Game.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAsteroids.Game.Engine;

public class GameLoop : IGameLoop, IDisposable
{
    private const float MAX_DELTA_TIME = 0.1f;

    private readonly IJSRuntime _jsRuntime;
    private readonly IInputManager _inputManager;
    private readonly GameState _gameState;
    private readonly IRenderer _renderer;
    private IJSObjectReference? _module;
    private DotNetObjectReference<GameLoop>? _dotNetRef;
    private bool _isRunning;

    public GameLoop(IJSRuntime jsRuntime, IInputManager inputManager, GameState gameState, IRenderer renderer)
    {
        _jsRuntime = jsRuntime;
        _inputManager = inputManager;
        _gameState = gameState;
        _renderer = renderer;
    }

    public async Task InitializeAsync(ElementReference canvas)
    {
        // Initialize the renderer with the canvas element
        await _renderer.InitializeAsync(canvas);

        // Set up the JS interop module for the game loop
        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./js/gameInterop.js");

        // Create a DotNetObjectReference so JS can call back into this instance
        _dotNetRef = DotNetObjectReference.Create(this);

        // Initialize the game loop in JS (registers keyboard listeners, starts RAF loop)
        await _module.InvokeVoidAsync("initializeGame", canvas, _dotNetRef);

        _isRunning = true;
    }

    [JSInvokable]
    public void Tick(float deltaTimeMs)
    {
        // Validate deltaTimeMs is non-negative; discard frame if invalid
        if (deltaTimeMs < 0)
            return;

        if (!_isRunning)
            return;

        // Convert to seconds
        float deltaTimeSec = deltaTimeMs / 1000f;

        // Clamp to MAX_DELTA_TIME to prevent large jumps (e.g., tab backgrounded)
        deltaTimeSec = MathF.Min(deltaTimeSec, MAX_DELTA_TIME);

        // Phase 1: Read Input
        Vector2 direction = _inputManager.GetMovementDirection();

        // Phase 2: Update State
        _gameState.Update(deltaTimeSec, direction);

        // Phase 3: Render (fire and forget)
        _ = _renderer.RenderAsync(_gameState);
    }

    [JSInvokable]
    public void SetKeyDown(string key)
    {
        _inputManager.SetKeyDown(key);
    }

    [JSInvokable]
    public void SetKeyUp(string key)
    {
        _inputManager.SetKeyUp(key);
    }

    public void Stop()
    {
        _isRunning = false;
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
