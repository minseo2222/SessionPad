using SessionPad.App.Services;

namespace SessionPad.Tests;

public class WindowPlacementCalculatorTests
{
    private const int Gap = 8;
    private static readonly WindowBounds PrimaryWorkArea = new(0, 0, 1920, 1080);

    [Fact]
    public void Right_space_exactly_equal_to_pad_width_places_on_right()
    {
        var result = Calculate(new WindowBounds(800, 100, 1612, 700), 300, 400);

        Assert.Equal(new WindowPlacement(1620, 100, "Right"), result);
    }

    [Fact]
    public void Ample_right_space_places_on_right()
    {
        var result = Calculate(new WindowBounds(100, 100, 500, 700), 300, 400);

        Assert.Equal(new WindowPlacement(508, 100, "Right"), result);
    }

    [Fact]
    public void Negative_coordinate_monitor_with_right_space_places_on_right()
    {
        var workArea = new WindowBounds(-1920, 0, 0, 1080);

        var result = Calculate(new WindowBounds(-1200, 100, -500, 700), 300, 400, workArea);

        Assert.Equal(new WindowPlacement(-492, 100, "Right"), result);
    }

    [Fact]
    public void Right_space_insufficient_and_left_space_exactly_equal_places_on_left()
    {
        var result = Calculate(new WindowBounds(308, 100, 1800, 700), 300, 400);

        Assert.Equal(new WindowPlacement(0, 100, "Left"), result);
    }

    [Fact]
    public void Right_space_insufficient_and_left_space_ample_places_on_left()
    {
        var result = Calculate(new WindowBounds(600, 100, 1800, 700), 300, 400);

        Assert.Equal(new WindowPlacement(292, 100, "Left"), result);
    }

    [Fact]
    public void Negative_coordinate_monitor_with_left_space_places_on_left()
    {
        var workArea = new WindowBounds(-1920, 0, 0, 1080);

        var result = Calculate(new WindowBounds(-1000, 100, -100, 700), 300, 400, workArea);

        Assert.Equal(new WindowPlacement(-1308, 100, "Left"), result);
    }

    [Fact]
    public void Both_sides_insufficient_and_right_space_larger_clamps_right()
    {
        var workArea = new WindowBounds(0, 0, 1000, 800);

        var result = Calculate(new WindowBounds(100, 100, 600, 600), 400, 300, workArea);

        Assert.Equal(new WindowPlacement(600, 100, "Clamped Right"), result);
    }

    [Fact]
    public void Both_sides_insufficient_and_left_space_larger_clamps_left()
    {
        var workArea = new WindowBounds(0, 0, 1000, 800);

        var result = Calculate(new WindowBounds(400, 100, 900, 600), 400, 300, workArea);

        Assert.Equal(new WindowPlacement(0, 100, "Clamped Left"), result);
    }

    [Fact]
    public void Equal_insufficient_space_preserves_clamped_right_tie_breaker()
    {
        var workArea = new WindowBounds(0, 0, 1000, 800);

        var result = Calculate(new WindowBounds(300, 100, 700, 600), 400, 300, workArea);

        Assert.Equal(new WindowPlacement(600, 100, "Clamped Right"), result);
    }

    [Fact]
    public void Target_overlapping_right_work_area_edge_clamps_left()
    {
        var workArea = new WindowBounds(0, 0, 1000, 800);

        var result = Calculate(new WindowBounds(100, 100, 1100, 600), 400, 300, workArea);

        Assert.Equal(new WindowPlacement(0, 100, "Clamped Left"), result);
    }

    [Fact]
    public void Target_overlapping_left_work_area_edge_clamps_right()
    {
        var workArea = new WindowBounds(0, 0, 1000, 800);

        var result = Calculate(new WindowBounds(-100, 100, 900, 600), 400, 300, workArea);

        Assert.Equal(new WindowPlacement(600, 100, "Clamped Right"), result);
    }

    [Fact]
    public void Target_top_inside_work_area_is_preserved()
    {
        var result = Calculate(new WindowBounds(100, 250, 500, 700), 300, 400);

        Assert.Equal(250, result.Y);
    }

    [Fact]
    public void Target_top_above_work_area_clamps_to_top()
    {
        var result = Calculate(new WindowBounds(100, -50, 500, 400), 300, 400);

        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void Pad_bottom_below_work_area_clamps_up()
    {
        var result = Calculate(new WindowBounds(100, 900, 500, 1000), 300, 300);

        Assert.Equal(780, result.Y);
    }

    [Fact]
    public void Negative_y_monitor_preserves_in_range_target_top()
    {
        var workArea = new WindowBounds(0, -900, 1600, 0);

        var result = Calculate(new WindowBounds(100, -800, 500, -200), 300, 200, workArea);

        Assert.Equal(-800, result.Y);
    }

    [Fact]
    public void Pad_taller_than_work_area_falls_back_to_work_area_top()
    {
        var workArea = new WindowBounds(0, 50, 1000, 650);

        var result = Calculate(new WindowBounds(100, 200, 500, 500), 300, 700, workArea);

        Assert.Equal(50, result.Y);
    }

    [Fact]
    public void Pad_width_equal_to_work_area_width_falls_back_to_work_area_left()
    {
        var workArea = new WindowBounds(0, 0, 1000, 800);

        var result = Calculate(new WindowBounds(300, 100, 700, 600), 1000, 300, workArea);

        Assert.Equal(new WindowPlacement(0, 100, "Clamped Right"), result);
    }

    [Fact]
    public void Pad_wider_than_work_area_falls_back_to_work_area_left()
    {
        var workArea = new WindowBounds(0, 0, 1000, 800);

        var result = Calculate(new WindowBounds(300, 100, 700, 600), 1200, 300, workArea);

        Assert.Equal(new WindowPlacement(0, 100, "Clamped Right"), result);
    }

    [Fact]
    public void Very_small_target_uses_normal_right_placement()
    {
        var result = Calculate(new WindowBounds(450, 100, 451, 101), 100, 100);

        Assert.Equal(new WindowPlacement(459, 100, "Right"), result);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(100, 0)]
    [InlineData(100, -1)]
    public void Non_positive_pad_size_is_rejected(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Calculate(new WindowBounds(100, 100, 500, 500), width, height));
    }

    [Fact]
    public void Missing_monitor_work_area_preserves_legacy_unclamped_right_fallback()
    {
        var result = WindowPlacementCalculator.Calculate(
            new WindowBounds(1800, -50, 2200, 700),
            300,
            400,
            workArea: null,
            Gap);

        Assert.Equal(new WindowPlacement(2208, -50, "Right"), result);
    }

    [Fact]
    public void Representative_legacy_geometry_matches_previous_algorithm()
    {
        var workArea = new WindowBounds(-1600, -200, 0, 700);

        var result = Calculate(new WindowBounds(-350, 500, -50, 850), 420, 300, workArea);

        Assert.Equal(new WindowPlacement(-778, 400, "Left"), result);
    }

    private static WindowPlacement Calculate(
        WindowBounds target,
        int padWidth,
        int padHeight,
        WindowBounds? workArea = null)
    {
        return WindowPlacementCalculator.Calculate(
            target,
            padWidth,
            padHeight,
            workArea ?? PrimaryWorkArea,
            Gap);
    }
}
