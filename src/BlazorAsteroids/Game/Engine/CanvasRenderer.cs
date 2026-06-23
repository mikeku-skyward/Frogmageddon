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
        await ClearAsync();

        if (_module is not null)
        {
            await _module.InvokeVoidAsync("drawPlayer",
                _canvas,
                state.Player.Position.X,
                state.Player.Position.Y,
                state.Player.Rotation,
                state.Player.Size);
        }
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
}
