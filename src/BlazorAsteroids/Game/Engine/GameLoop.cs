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
    private GamePhase _currentPhase = GamePhase.StartScreen;
    private StartButtonBounds _buttonBounds = null!;

    public GamePhase CurrentPhase => _currentPhase;

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

        _buttonBounds = StartButtonBounds.Create(_gameState.CanvasWidth, _gameState.CanvasHeight);

        // Render start screen immediately
        await _renderer.RenderStartScreenAsync(_gameState.CanvasWidth, _gameState.CanvasHeight, _buttonBounds);
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

        if (_currentPhase == GamePhase.StartScreen)
        {
            HandleStartScreenInput();
            _ = _renderer.RenderStartScreenAsync(_gameState.CanvasWidth, _gameState.CanvasHeight, _buttonBounds);
        }
        else if (_currentPhase == GamePhase.GameOver)
        {
            HandleGameOverInput();
            _ = _renderer.RenderGameOverAsync(_gameState.CanvasWidth, _gameState.CanvasHeight,
                _buttonBounds.X, _buttonBounds.Y, _buttonBounds.Width, _buttonBounds.Height);
        }
        else
        {
            // Phase 1: Read Input
            Vector2 direction = _inputManager.GetMovementDirection();

            // Get mouse position in world space for camera targeting
            var mouse = _inputManager.MousePosition;
            Vector2 cursorWorld = new Vector2(
                mouse.X + _gameState.Camera.Position.X,
                mouse.Y + _gameState.Camera.Position.Y
            );

            // Check for shooting
            var click = _inputManager.ConsumePendingClick();
            if (click.HasValue)
            {
                // Convert screen click to world position
                float worldX = click.Value.X + _gameState.Camera.Position.X;
                float worldY = click.Value.Y + _gameState.Camera.Position.Y;
                _gameState.FireBullet(new Vector2(worldX, worldY));
            }

            // Phase 2: Update State
            _gameState.Update(deltaTimeSec, direction, cursorWorld);

            // Check if player died
            if (_gameState.Player.Health <= 0)
            {
                _currentPhase = GamePhase.GameOver;
            }

            // Phase 3: Render (fire and forget)
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

    private void HandleGameOverInput()
    {
        // Check Enter key
        if (_inputManager.IsKeyPressed("enter"))
        {
            RestartGame();
            return;
        }

        // Check mouse click on restart button
        var click = _inputManager.ConsumePendingClick();
        if (click.HasValue && _buttonBounds.Contains(click.Value.X, click.Value.Y))
        {
            RestartGame();
        }
    }

    private void RestartGame()
    {
        _gameState.Player.Health = 100;
        _gameState.Player.Score = 0;
        _gameState.Player.DamageFlashTimer = 0f;
        _gameState.Player.InvincibilityTimer = 0f;
        _gameState.Player.Position = new Vector2(
            _gameState.WorldWidth / 2f,
            _gameState.WorldHeight / 2f);
        _gameState.Frogs.Clear();
        _gameState.Bullets.Clear();
        _gameState.Camera.SnapTo(_gameState.Player.Position);
        _currentPhase = GamePhase.Playing;
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

    [JSInvokable]
    public void OnMouseClick(float x, float y)
    {
        _inputManager.SetMouseClick(x, y);
    }

    [JSInvokable]
    public void OnMouseMove(float x, float y)
    {
        _inputManager.SetMousePosition(x, y);
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
