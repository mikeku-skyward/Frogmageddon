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

        await _module.InvokeVoidAsync("renderFrame",
            _canvas,
            state.Camera.Position.X,
            state.Camera.Position.Y,
            state.Player.Position.X,
            state.Player.Position.Y,
            state.Player.Rotation,
            state.Player.Size);
    }
}
