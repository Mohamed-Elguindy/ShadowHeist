using System.Numerics;

namespace ShadowHeist.Core;

/// <summary>
/// Pure C# AABB rectangle struct used for headless collision and trigger bounds.
/// </summary>
public readonly record struct RectangleBounds(float X, float Y, float Width, float Height)
{
    public Vector2 Center => new(X + (Width * 0.5f), Y + (Height * 0.5f));
}
