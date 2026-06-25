using System.Reflection;
using FsCheck;
using FsCheck.Xunit;
using BlazorAsteroids.Game.Engine;
using BlazorAsteroids.Game.Interfaces;
using BlazorAsteroids.Game.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAsteroids.Tests;

/// <summary>
/// Property-based tests for static screen render-once optimization in GameLoop.
/// Validates: Requirements 5.2, 5.3
/// </summary>
[Trait("Feature", "performance-optimizations")]
public class StaticScreenRenderPropertyTests
{
    #region Mock Implementations

    private class MockRenderer : IRenderer
    {
        public int StartScreenRenderCount { get; private set; }
        public int GameOverRenderCount { get; private set; }
        public int PausedRenderCount { get; private set; }

        public Task InitializeAsync(ElementReference canvas) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
        public Task RenderAsync(GameState state) => Task.CompletedTask;

        public Task RenderStartScreenAsync(int canvasWidth, int canvasHeight, StartButtonBounds buttonBounds)
        {
            StartScreenRenderCount++;
            return Task.CompletedTask;
        }

        public Task RenderGameOverAsync(int canvasWidth, int canvasHeight, float btnX, float btnY, float btnW, float btnH, float fadeAlpha)
        {
            GameOverRenderCount++;
            return Task.CompletedTask;
        }

        public Task RenderPausedAsync(int canvasWidth, int canvasHeight, float btnX, float btnY, float btnW, float btnH, float restartBtnY)
        {
            PausedRenderCount++;
            return Task.CompletedTask;
        }

        public Task RenderInstructionsAsync(int canvasWidth, int canvasHeight, float btnX, float btnY, float btnW, float btnH)
        {
            return Task.CompletedTask;
        }
    }

    private class MockInputManager : IInputManager
    {
        public (float X, float Y) MousePosition => (0, 0);
        public void SetKeyDown(string key) { }
        public void SetKeyUp(string key) { }
        public Vector2 GetMovementDirection() => new Vector2(0, 0);
        public bool IsKeyPressed(string key) => false;
        public bool ConsumeKeyPress(string key) => false;
        public void SetMouseClick(float x, float y) { }
        public void SetMousePosition(float x, float y) { }
        public (float X, float Y)? ConsumePendingClick() => null;
    }

    private class MockHighScoreService : IHighScoreService
    {
        public Task<int> GetHighScoreAsync() => Task.FromResult(0);
        public Task SaveHighScoreAsync(int score) => Task.CompletedTask;
    }

    private class MockJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new ValueTask<TValue>(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => new ValueTask<TValue>(default(TValue)!);
    }

    #endregion

    /// <summary>
    /// Sets up a GameLoop with mocked dependencies and uses reflection to configure
    /// internal state for testing the render-once behavior.
    /// </summary>
    private static (GameLoop gameLoop, MockRenderer renderer) CreateGameLoopInPhase(GamePhase phase)
    {
        var renderer = new MockRenderer();
        var inputManager = new MockInputManager();
        var highScoreService = new MockHighScoreService();
        var jsRuntime = new MockJSRuntime();
        var gameState = new GameState
        {
            CanvasWidth = 800,
            CanvasHeight = 600,
            WorldWidth = 5000,
            WorldHeight = 5000
        };

        var gameLoop = new GameLoop(jsRuntime, inputManager, gameState, renderer, highScoreService);

        // Use reflection to set internal state so we can test Tick without InitializeAsync
        var type = typeof(GameLoop);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        type.GetField("_isRunning", flags)!.SetValue(gameLoop, true);
        type.GetField("_currentPhase", flags)!.SetValue(gameLoop, phase);
        // Set _lastRenderedPhase to null so first Tick triggers a render
        type.GetField("_lastRenderedPhase", flags)!.SetValue(gameLoop, null);

        // For GameOver phase, set fade timer past the fade duration so we test
        // the render-once behavior after the fade animation completes.
        if (phase == GamePhase.GameOver)
        {
            type.GetField("_gameOverFadeTimer", flags)!.SetValue(gameLoop, 2.0f);
        }

        // Set button bounds (needed for render calls)
        var buttonBounds = new StartButtonBounds(320f, 340f, 160f, 50f);
        type.GetField("_buttonBounds", flags)!.SetValue(gameLoop, buttonBounds);

        var restartButtonBounds = new StartButtonBounds(320f, 410f, 160f, 50f);
        type.GetField("_restartButtonBounds", flags)!.SetValue(gameLoop, restartButtonBounds);

        return (gameLoop, renderer);
    }

    /// <summary>
    /// Gets the render count for the specific phase from the mock renderer.
    /// </summary>
    private static int GetRenderCountForPhase(MockRenderer renderer, GamePhase phase) => phase switch
    {
        GamePhase.StartScreen => renderer.StartScreenRenderCount,
        GamePhase.GameOver => renderer.GameOverRenderCount,
        GamePhase.Paused => renderer.PausedRenderCount,
        _ => throw new ArgumentException($"Phase {phase} is not a static screen phase")
    };

    /// <summary>
    /// Property 7: Static screen render count stays at one regardless of tick count.
    /// For any number of ticks N (N ≥ 1) occurring while the game remains in the same static phase
    /// (GameOver or Paused) without a phase change, the render function for that screen
    /// SHALL be invoked exactly once (on the initial transition) and zero additional times during
    /// subsequent ticks.
    /// Note: StartScreen is excluded because it renders continuously (frog sprites load async).
    /// Validates: Requirements 5.2, 5.3
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "7: Static screen render count stays at one regardless of tick count")]
    public bool StaticScreen_RendersOnce_RegardlessOfTickCount(PositiveInt tickCountArg)
    {
        // Clamp tick count to a reasonable range (1-500)
        int tickCount = Math.Max(1, tickCountArg.Get % 500);

        // StartScreen is excluded — it now renders every tick (continuous render for async sprites)
        var phases = new[] { GamePhase.GameOver, GamePhase.Paused };

        foreach (var phase in phases)
        {
            var (gameLoop, renderer) = CreateGameLoopInPhase(phase);

            // Tick N times with a valid deltaTime (16ms = ~60fps)
            for (int i = 0; i < tickCount; i++)
            {
                gameLoop.Tick(16.0f);
            }

            int renderCount = GetRenderCountForPhase(renderer, phase);

            // The render function should be called exactly once regardless of tick count
            if (renderCount != 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Property: Start screen renders every tick (continuous rendering).
    /// For any number of ticks N (N ≥ 1) while the game remains in StartScreen phase,
    /// RenderStartScreenAsync SHALL be invoked exactly N times.
    /// Validates: Requirements 2.1, 2.2
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "StartScreen renders continuously every tick")]
    public bool StartScreen_RendersContinuously_EveryTick(PositiveInt tickCountArg)
    {
        // Clamp tick count to a reasonable range (1-500)
        int tickCount = Math.Max(1, tickCountArg.Get % 500);

        var (gameLoop, renderer) = CreateGameLoopInPhase(GamePhase.StartScreen);

        // Tick N times with a valid deltaTime (16ms = ~60fps)
        for (int i = 0; i < tickCount; i++)
        {
            gameLoop.Tick(16.0f);
        }

        // StartScreen should render every tick (no render-once gate)
        return renderer.StartScreenRenderCount == tickCount;
    }
}
