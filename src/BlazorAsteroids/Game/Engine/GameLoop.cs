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
    private readonly IHighScoreService _highScoreService;
    private IJSObjectReference? _module;
    private DotNetObjectReference<GameLoop>? _dotNetRef;
    private bool _isRunning;
    private GamePhase _currentPhase = GamePhase.StartScreen;
    private GamePhase? _lastRenderedPhase;
    private StartButtonBounds _buttonBounds = null!;
    private StartButtonBounds _restartButtonBounds = null!;
    private StartButtonBounds _instructionsButtonBounds = null!;
    private int _highScore;
    private float _gameOverFadeTimer;
    private const float GameOverFadeDuration = 1.0f; // seconds

    public GamePhase CurrentPhase => _currentPhase;
    public int HighScore => _highScore;

    public GameLoop(IJSRuntime jsRuntime, IInputManager inputManager, GameState gameState, IRenderer renderer, IHighScoreService highScoreService)
    {
        _jsRuntime = jsRuntime;
        _inputManager = inputManager;
        _gameState = gameState;
        _renderer = renderer;
        _highScoreService = highScoreService;
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

        _highScore = await _highScoreService.GetHighScoreAsync();

        _buttonBounds = StartButtonBounds.Create(_gameState.CanvasWidth, _gameState.CanvasHeight);

        // Restart button sits below the main button with some spacing
        _restartButtonBounds = new StartButtonBounds(
            _buttonBounds.X,
            _buttonBounds.Y + _buttonBounds.Height + 20f,
            _buttonBounds.Width,
            _buttonBounds.Height);

        // Instructions button sits below the start button on the start screen
        _instructionsButtonBounds = new StartButtonBounds(
            _buttonBounds.X,
            _buttonBounds.Y + _buttonBounds.Height + 20f,
            _buttonBounds.Width,
            _buttonBounds.Height);

        // Start screen will render on the first Tick (allows time for sprites to load)
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
            if (_currentPhase == GamePhase.StartScreen && _lastRenderedPhase != _currentPhase)
            {
                _ = _renderer.RenderStartScreenAsync(_gameState.CanvasWidth, _gameState.CanvasHeight, _buttonBounds);
                _lastRenderedPhase = _currentPhase;
            }
        }
        else if (_currentPhase == GamePhase.Instructions)
        {
            HandleInstructionsInput();
            if (_currentPhase == GamePhase.Instructions && _lastRenderedPhase != _currentPhase)
            {
                _ = _renderer.RenderInstructionsAsync(_gameState.CanvasWidth, _gameState.CanvasHeight,
                    _buttonBounds.X, _buttonBounds.Y, _buttonBounds.Width, _buttonBounds.Height);
                _lastRenderedPhase = _currentPhase;
            }
        }
        else if (_currentPhase == GamePhase.GameOver)
        {
            _gameOverFadeTimer += deltaTimeSec;
            float fadeAlpha = MathF.Min(_gameOverFadeTimer / GameOverFadeDuration, 1.0f);

            // During fade: render the frozen game state then overlay on top each frame
            // After fade completes: render-once
            if (fadeAlpha < 1.0f)
            {
                // Render the frozen death scene
                _ = _renderer.RenderAsync(_gameState);
                // Then overlay
                _ = _renderer.RenderGameOverAsync(_gameState.CanvasWidth, _gameState.CanvasHeight,
                    _buttonBounds.X, _buttonBounds.Y, _buttonBounds.Width, _buttonBounds.Height, fadeAlpha);
                HandleGameOverInput();
            }
            else if (_lastRenderedPhase != _currentPhase)
            {
                // Final frame: render once at full alpha
                _ = _renderer.RenderAsync(_gameState);
                _ = _renderer.RenderGameOverAsync(_gameState.CanvasWidth, _gameState.CanvasHeight,
                    _buttonBounds.X, _buttonBounds.Y, _buttonBounds.Width, _buttonBounds.Height, 1.0f);
                _lastRenderedPhase = _currentPhase;
                HandleGameOverInput();
            }
            else
            {
                HandleGameOverInput();
            }
        }
        else if (_currentPhase == GamePhase.Paused)
        {
            HandlePausedInput();
            if (_currentPhase == GamePhase.Paused && _lastRenderedPhase != _currentPhase)
            {
                _ = _renderer.RenderPausedAsync(_gameState.CanvasWidth, _gameState.CanvasHeight,
                    _buttonBounds.X, _buttonBounds.Y, _buttonBounds.Width, _buttonBounds.Height,
                    _restartButtonBounds.Y);
                _lastRenderedPhase = _currentPhase;
            }
        }
        else
        {
            // Check for pause input (Escape key)
            if (_inputManager.ConsumeKeyPress("escape"))
            {
                _currentPhase = GamePhase.Paused;
                _ = UpdateHighScoreAsync();
                return;
            }
            // Phase 1: Read Input
            Vector2 direction = _inputManager.GetMovementDirection();
            bool shiftPressed = _inputManager.IsKeyPressed("shift");

            // Get mouse position in world space for camera targeting
            var mouse = _inputManager.MousePosition;
            Vector2 cursorWorld = new Vector2(
                mouse.X + _gameState.Camera.Position.X,
                mouse.Y + _gameState.Camera.Position.Y
            );

            // Track reload state to detect when a reload starts (from any trigger)
            bool wasReloading = _gameState.AmmoSystem.IsReloading;

            // Check for reload input (R key)
            if (_inputManager.IsKeyPressed("r"))
            {
                _gameState.AmmoSystem.StartReload();
            }

            // Check for shooting
            var click = _inputManager.ConsumePendingClick();
            if (click.HasValue)
            {
                // Convert screen click to world position
                float worldX = click.Value.X + _gameState.Camera.Position.X;
                float worldY = click.Value.Y + _gameState.Camera.Position.Y;
                if (_gameState.FireBullet(new Vector2(worldX, worldY)))
                {
                    _ = _module!.InvokeVoidAsync("playShootSound");
                }
            }

            // Play reload sound if a reload just started (manual or auto-triggered)
            if (!wasReloading && _gameState.AmmoSystem.IsReloading)
            {
                _ = _module!.InvokeVoidAsync("playReloadSound");
            }

            // Update stamina system before game state update
            bool wasSprinting = _gameState.StaminaSystem.IsSprinting;
            _gameState.StaminaSystem.Update(deltaTimeSec, shiftPressed);

            // Play dash sound when sprint starts
            if (!wasSprinting && _gameState.StaminaSystem.IsSprinting)
            {
                _ = _module!.InvokeVoidAsync("playDashSound");
            }

            // Phase 2: Update State
            int healthBefore = _gameState.Player.Health;
            int scoreBefore = _gameState.Player.Score;
            _gameState.Update(deltaTimeSec, direction, cursorWorld);

            // Play damage sound if the player took a hit this frame
            if (_gameState.Player.Health < healthBefore)
            {
                _ = _module!.InvokeVoidAsync("playDamageSound");
            }

            // Play score sound if the player's score increased
            if (_gameState.Player.Score > scoreBefore)
            {
                _ = _module!.InvokeVoidAsync("playScoreSound");
            }

            // Play croak sound for frogs that just started hopping (limit to 1 per frame)
            bool croakPlayed = false;
            foreach (var frog in _gameState.Frogs)
            {
                if (frog.JustStartedHopping)
                {
                    frog.JustStartedHopping = false;
                    if (!croakPlayed)
                    {
                        _ = _module!.InvokeVoidAsync("playCroakSound");
                        croakPlayed = true;
                    }
                }
            }

            // Check if player died
            if (_gameState.Player.Health <= 0)
            {
                _gameState.AmmoSystem.CancelReload();
                _currentPhase = GamePhase.GameOver;
                _gameOverFadeTimer = 0f;
                _ = UpdateHighScoreAsync();
                _ = _module!.InvokeVoidAsync("playGameOverSound");
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
        if (click.HasValue)
        {
            if (_buttonBounds.Contains(click.Value.X, click.Value.Y))
            {
                TransitionToPlaying();
            }
            else if (_instructionsButtonBounds.Contains(click.Value.X, click.Value.Y))
            {
                _currentPhase = GamePhase.Instructions;
                _lastRenderedPhase = null;
            }
        }
    }

    private void TransitionToPlaying()
    {
        _currentPhase = GamePhase.Playing;
        _lastRenderedPhase = null;
        _gameState.Player.Position = new Vector2(
            _gameState.CanvasWidth / 2f,
            _gameState.CanvasHeight / 2f);
        _gameState.PlayerAnimation.Reset();
    }

    private void HandleInstructionsInput()
    {
        // Check Escape key to go back
        if (_inputManager.ConsumeKeyPress("escape"))
        {
            _currentPhase = GamePhase.StartScreen;
            _lastRenderedPhase = null;
            return;
        }

        // Compute Back button Y matching the JS layout calculation
        const float KEYCAP_SIZE = 32f;
        const float ROW_HEIGHT = 48f;
        const float WASD_CLUSTER_H = 2f * KEYCAP_SIZE + 4f;
        const float TOP_MARGIN = 50f;
        const float titleH = 40f;
        const float objectiveH = 24f;
        const float gap = 20f;
        float instructionsH = WASD_CLUSTER_H + 20f + 4f * ROW_HEIGHT;
        float totalContentH = titleH + gap + objectiveH + gap + instructionsH + gap + _buttonBounds.Height;
        float availableH = _gameState.CanvasHeight - TOP_MARGIN;
        float startY = TOP_MARGIN + MathF.Max(0f, (availableH - totalContentH) / 2f);
        float backBtnY = startY + titleH + gap + objectiveH + gap + instructionsH + gap;

        var backBounds = new StartButtonBounds(_buttonBounds.X, backBtnY, _buttonBounds.Width, _buttonBounds.Height);

        var click = _inputManager.ConsumePendingClick();
        if (click.HasValue && backBounds.Contains(click.Value.X, click.Value.Y))
        {
            _currentPhase = GamePhase.StartScreen;
            _lastRenderedPhase = null;
        }
    }

    private void HandleGameOverInput()
    {
        // Check Enter key
        if (_inputManager.IsKeyPressed("enter"))
        {
            RestartGame();
            return;
        }

        // Restart button is vertically centered: matches JS calculation
        const float titleH = 48f;
        const float gap = 30f;
        float totalH = titleH + gap + _buttonBounds.Height;
        float groupStartY = (_gameState.CanvasHeight - totalH) / 2f;
        float restartBtnY = groupStartY + titleH + gap;
        var restartBounds = new StartButtonBounds(_buttonBounds.X, restartBtnY, _buttonBounds.Width, _buttonBounds.Height);

        var click = _inputManager.ConsumePendingClick();
        if (click.HasValue && restartBounds.Contains(click.Value.X, click.Value.Y))
        {
            RestartGame();
        }
    }

    private void HandlePausedInput()
    {
        // Check Escape key to resume
        if (_inputManager.ConsumeKeyPress("escape"))
        {
            _currentPhase = GamePhase.Playing;
            _lastRenderedPhase = null;
            return;
        }

        // Check mouse click on resume or restart button
        var click = _inputManager.ConsumePendingClick();
        if (click.HasValue)
        {
            if (_buttonBounds.Contains(click.Value.X, click.Value.Y))
            {
                _currentPhase = GamePhase.Playing;
                _lastRenderedPhase = null;
            }
            else if (_restartButtonBounds.Contains(click.Value.X, click.Value.Y))
            {
                RestartGame();
            }
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

        // Return all entities to pools before clearing
        foreach (var bullet in _gameState.Bullets)
        {
            bullet.Reset();
            _gameState.BulletPool.Release(bullet);
        }
        foreach (var frog in _gameState.Frogs)
        {
            frog.Reset();
            _gameState.FrogPool.Release(frog);
        }
        _gameState.Frogs.Clear();
        _gameState.Bullets.Clear();

        _gameState.Camera.SnapTo(_gameState.Player.Position);
        _gameState.AmmoSystem.Reset();
        _gameState.StaminaSystem.Reset();
        _gameState.PlayerAnimation.Reset();
        _lastRenderedPhase = null;
        _currentPhase = GamePhase.Playing;
    }

    private async Task UpdateHighScoreAsync()
    {
        int currentScore = _gameState.Player.Score;
        if (currentScore > _highScore)
        {
            _highScore = currentScore;
            await _highScoreService.SaveHighScoreAsync(_highScore);
        }
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
