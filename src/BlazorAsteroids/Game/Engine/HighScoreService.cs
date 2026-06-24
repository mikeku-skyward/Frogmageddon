using BlazorAsteroids.Game.Interfaces;
using Microsoft.JSInterop;

namespace BlazorAsteroids.Game.Engine;

/// <summary>
/// Persists the player's high score to the browser's localStorage,
/// which acts as a local temp file on the user's computer.
/// </summary>
public class HighScoreService : IHighScoreService
{
    private const string StorageKey = "frogmageddon_highscore";
    private readonly IJSRuntime _jsRuntime;

    public HighScoreService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<int> GetHighScoreAsync()
    {
        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        return int.TryParse(value, out var score) ? score : 0;
    }

    public async Task SaveHighScoreAsync(int score)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, score.ToString());
    }
}
