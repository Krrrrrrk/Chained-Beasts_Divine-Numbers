namespace ChainedBeasts.DivineNumbers;

// Screen coordinates, with the origin at bottom left. Kept independent of Unity.
internal readonly record struct Bounds(float Left, float Bottom, float Right, float Top);
internal readonly record struct PanelPosition(float Left, float Top);
internal static class PanelPlacement
{
    internal static Bounds Union(Bounds a, Bounds b) => new(Math.Min(a.Left, b.Left), Math.Min(a.Bottom, b.Bottom),
        Math.Max(a.Right, b.Right), Math.Max(a.Top, b.Top));
    internal static bool Contains(Bounds bounds, float x, float y) =>
        x >= bounds.Left && x <= bounds.Right && y >= bounds.Bottom && y <= bounds.Top;
    internal static PanelPosition Place(Bounds anchor, float width, float height,
        float screenWidth, float screenHeight, float margin = 16, float gap = 18)
    {
        float x, y;
        if (anchor.Right + gap + width <= screenWidth - margin)
            (x, y) = (anchor.Right + gap, anchor.Top);
        else if (anchor.Left - gap - width >= margin)
            (x, y) = (anchor.Left - gap - width, anchor.Top);
        else if (anchor.Bottom - gap - height >= margin)
            (x, y) = (anchor.Left, anchor.Bottom - gap);
        else
            (x, y) = (anchor.Left, anchor.Top + gap + height);
        return new PanelPosition(Math.Clamp(x, margin, Math.Max(margin, screenWidth - width - margin)),
            Math.Clamp(y, height + margin, Math.Max(height + margin, screenHeight - margin)));
    }
}
