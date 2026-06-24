using BlazorAsteroids.Game.Interfaces;
using BlazorAsteroids.Game.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAsteroids.Game.Engine;

public class CanvasRenderer : IRenderer
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private ElementReference _canvas;

    public CanvasRenderer(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync(ElementReference canvas)
    {
        _canvas = canvas;
        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./js/gameInterop.js");
    }

    public async Task ClearAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("clearCanvas", _canvas);
        }
    }

    public async Task RenderAsync(GameState state)
    {
        if (_module is null) return;

        // Build frog data as flat array: [x, y, rotation, size, ...]
        var frogData = new float[state.Frogs.Count * 4];
        for (int i = 0; i < state.Frogs.Count; i++)
        {
            var frog = state.Frogs[i];
            frogData[i * 4] = frog.Position.X;
            frogData[i * 4 + 1] = frog.Position.Y;
            frogData[i * 4 + 2] = frog.Rotation;
            frogData[i * 4 + 3] = frog.Size;
        }

        // Build bullet data as flat array: [x, y, radius, ...]
        var bulletData = new float[state.Bullets.Count * 3];
        for (int i = 0; i < state.Bullets.Count; i++)
        {
            var bullet = state.Bullets[i];
            bulletData[i * 3] = bullet.Position.X;
            bulletData[i * 3 + 1] = bullet.Position.Y;
            bulletData[i * 3 + 2] = bullet.Radius;
        }

        // Player position relative to camera (screen coordinates)
        float playerScreenX = state.Player.Position.X - state.Camera.Position.X;
        float playerScreenY = state.Player.Position.Y - state.Camera.Position.Y;

        await _module.InvokeVoidAsync("renderFrame",
            _canvas,
            state.Camera.Position.X,
            state.Camera.Position.Y,
            state.Player.Position.X,
            state.Player.Position.Y,
            state.Player.Rotation,
            state.Player.Size,
            frogData,
            bulletData,
            state.Player.IsFlashing,
            state.AmmoSystem.CurrentAmmo,
            AmmoSystem.MaxAmmo,
            state.AmmoSystem.IsReloading,
            state.AmmoSystem.ReloadProgress,
            playerScreenX,
            playerScreenY,
            state.Player.Size,
            Math.Clamp(state.StaminaSystem.Stamina, 0f, 1f));
    }

    public async Task RenderStartScreenAsync(int canvasWidth, int canvasHeight, StartButtonBounds buttonBounds)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("drawStartScreen",
                _canvas, canvasWidth, canvasHeight,
                buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height);
        }
    }

    public async Task RenderGameOverAsync(int canvasWidth, int canvasHeight, float btnX, float btnY, float btnW, float btnH)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("drawGameOverScreen", _canvas, canvasWidth, canvasHeight, btnX, btnY, btnW, btnH);
        }
    }

    public async Task RenderPausedAsync(int canvasWidth, int canvasHeight, float btnX, float btnY, float btnW, float btnH, float restartBtnY)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("drawPausedScreen", _canvas, canvasWidth, canvasHeight, btnX, btnY, btnW, btnH, restartBtnY);
        }
    }
}
