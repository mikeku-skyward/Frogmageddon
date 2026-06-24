using FsCheck;
using FsCheck.Xunit;
using BlazorAsteroids.Game.Engine;

namespace BlazorAsteroids.Tests;

/// <summary>
/// Property-based tests for the RenderBuffer class.
/// Validates: Requirements 1.1, 1.2, 1.3, 1.5
/// </summary>
[Trait("Feature", "performance-optimizations")]
public class RenderBufferPropertyTests
{
    /// <summary>
    /// Property 1: Buffer capacity is monotonically non-decreasing and always sufficient.
    /// For any sequence of required capacities passed to RenderBuffer.EnsureCapacity,
    /// the buffer's Data.Length SHALL be monotonically non-decreasing AND always ≥ the most recent required capacity.
    /// Validates: Requirements 1.2, 1.3
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "1: Buffer capacity is monotonically non-decreasing and always sufficient")]
    public bool BufferCapacity_IsMonotonicallyNonDecreasing_AndAlwaysSufficient(PositiveInt initialCap, PositiveInt[] capacities)
    {
        var initialCapacity = Math.Max(1, initialCap.Get % 1024);
        var buffer = new RenderBuffer(initialCapacity);

        var previousLength = buffer.Data.Length;

        foreach (var cap in capacities)
        {
            var required = cap.Get % 10000; // Keep capacities reasonable
            buffer.EnsureCapacity(required);

            var currentLength = buffer.Data.Length;

            if (currentLength < previousLength)
                return false; // Violated monotonic non-decreasing

            if (currentLength < required)
                return false; // Violated always sufficient

            previousLength = currentLength;
        }

        return true;
    }

    /// <summary>
    /// Property 2: Buffer identity is preserved when capacity is sufficient.
    /// For any call to RenderBuffer.EnsureCapacity where the required capacity is ≤ the current Data.Length,
    /// the array reference returned by Data SHALL be the same object as before the call (no reallocation).
    /// Validates: Requirements 1.1, 1.5
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "2: Buffer identity is preserved when capacity is sufficient")]
    public bool BufferIdentity_IsPreserved_WhenCapacityIsSufficient(PositiveInt initialCap, PositiveInt requiredCap)
    {
        var initialCapacity = Math.Max(1, initialCap.Get % 10000);
        var buffer = new RenderBuffer(initialCapacity);

        var currentLength = buffer.Data.Length;
        var required = requiredCap.Get % (currentLength + 1); // Ensure required <= currentLength

        var arrayBefore = buffer.Data;
        buffer.EnsureCapacity(required);
        var arrayAfter = buffer.Data;

        return ReferenceEquals(arrayBefore, arrayAfter);
    }
}
