namespace BlazorAsteroids.Game.Interfaces;

public interface IHighScoreService
{
    Task<int> GetHighScoreAsync();
    Task SaveHighScoreAsync(int score);
}
