using Microsoft.AspNetCore.Components;

namespace BlazorAsteroids.Game.Interfaces;

public interface IGameLoop
{
    Task InitializeAsync(ElementReference canvas);
    void Tick(float deltaTimeMs);
    void Stop();
}
