using BlazorAsteroids.Game.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorAsteroids.Game.Interfaces;

public interface IRenderer
{
    Task InitializeAsync(ElementReference canvas);
    Task RenderAsync(GameState state);
    Task RenderStartScreenAsync(int canvasWidth, int canvasHeight, StartButtonBounds buttonBounds);
    Task RenderGameOverAsync(int canvasWidth, int canvasHeight);
    Task ClearAsync();
}
