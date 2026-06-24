using BlazorAsteroids.Game.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorAsteroids.Game.Interfaces;

public interface IRenderer
{
    Task InitializeAsync(ElementReference canvas);
    Task RenderAsync(GameState state);
    Task RenderStartScreenAsync(int canvasWidth, int canvasHeight, StartButtonBounds buttonBounds);
    Task RenderGameOverAsync(int canvasWidth, int canvasHeight, float btnX, float btnY, float btnW, float btnH);
    Task RenderPausedAsync(int canvasWidth, int canvasHeight, float btnX, float btnY, float btnW, float btnH, float restartBtnY);
    Task RenderInstructionsAsync(int canvasWidth, int canvasHeight, float btnX, float btnY, float btnW, float btnH);
    Task ClearAsync();
}
