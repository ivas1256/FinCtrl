namespace FinCtrl.Common.Tests;

public class DateTimeExtensionsTests
{
    [Theory]
    [InlineData("2025-10-15", "2025-11-01")]
    [InlineData("2025-10-01", "2025-11-01")]
    [InlineData("2025-01-31", "2025-02-01")]
    [InlineData("2025-12-31", "2026-01-01")]
    public void NextMonthStart_AnyDay_ReturnsFirstDayOfNextMonth(string date, string expected)
    {
        Assert.Equal(Parse(expected), Parse(date).NextMonthStart());
    }

    [Fact]
    public void NextMonthStart_WithTime_TimeDropped()
    {
        Assert.Equal(new DateTime(2025, 11, 1), new DateTime(2025, 10, 15, 14, 30, 0).NextMonthStart());
    }

    [Theory]
    [InlineData("2025-10-15", "2025-10-01")]
    [InlineData("2025-10-01", "2025-10-01")]
    [InlineData("2024-02-29", "2024-02-01")]
    public void FirstDayOfMonth_AnyDay_ReturnsFirstDayOfSameMonth(string date, string expected)
    {
        Assert.Equal(Parse(expected), Parse(date).FirstDayOfMonth());
    }

    [Fact]
    public void FirstDayOfMonth_WithTime_TimeDropped()
    {
        Assert.Equal(new DateTime(2025, 10, 1), new DateTime(2025, 10, 15, 14, 30, 0).FirstDayOfMonth());
    }

    private static DateTime Parse(string value) => DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
