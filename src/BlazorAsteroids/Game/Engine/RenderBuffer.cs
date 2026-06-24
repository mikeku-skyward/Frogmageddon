namespace BlazorAsteroids.Game.Engine;

/// <summary>
/// A grow-only float buffer that avoids per-frame allocations.
/// </summary>
internal sealed class RenderBuffer
{
    private float[] _data;

    public RenderBuffer(int initialCapacity)
    {
        _data = new float[initialCapacity];
    }

    public float[] Data => _data;

    /// <summary>
    /// Ensures capacity is at least <paramref name="required"/> elements.
    /// Grows by doubling if needed. Never shrinks.
    /// </summary>
    public void EnsureCapacity(int required)
    {
        if (_data.Length >= required) return;
        int newSize = _data.Length;
        while (newSize < required) newSize *= 2;
        _data = new float[newSize];
    }
}
