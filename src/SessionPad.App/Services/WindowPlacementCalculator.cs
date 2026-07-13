namespace SessionPad.App.Services;

internal static class WindowPlacementCalculator
{
    public static WindowPlacement Calculate(
        WindowBounds targetBounds,
        int sessionPadWidth,
        int sessionPadHeight,
        WindowBounds? workArea,
        int gap)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sessionPadWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sessionPadHeight);

        var x = targetBounds.Right + gap;
        var y = targetBounds.Top;
        var side = "Right";

        if (workArea is { } area)
        {
            var rightX = targetBounds.Right + gap;
            var leftX = targetBounds.Left - gap - sessionPadWidth;
            var rightSpace = area.Right - rightX;
            var leftSpace = targetBounds.Left - gap - area.Left;

            if (rightSpace >= sessionPadWidth)
            {
                x = rightX;
                side = "Right";
            }
            else if (leftX >= area.Left)
            {
                x = leftX;
                side = "Left";
            }
            else if (rightSpace >= leftSpace)
            {
                x = Clamp(rightX, area.Left, area.Right - sessionPadWidth);
                side = "Clamped Right";
            }
            else
            {
                x = Clamp(leftX, area.Left, area.Right - sessionPadWidth);
                side = "Clamped Left";
            }

            y = Clamp(targetBounds.Top, area.Top, area.Bottom - sessionPadHeight);
        }

        return new WindowPlacement(x, y, side);
    }

    private static int Clamp(int value, int min, int max)
    {
        if (max < min)
        {
            return min;
        }

        return Math.Min(Math.Max(value, min), max);
    }
}

internal readonly record struct WindowBounds(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;
}

internal readonly record struct WindowPlacement(int X, int Y, string Side);
