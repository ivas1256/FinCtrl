namespace FinCtrl.Common.Tests;

public class DateRangeTests
{
    [Fact]
    public void FromInclusiveDates_Days_ToIsDayAfterLastDayAndTimeDropped()
    {
        var range = DateRange.FromInclusiveDates(new DateTime(2025, 10, 1, 15, 0, 0), new DateTime(2025, 10, 31, 23, 59, 0));

        Assert.Equal(new DateTime(2025, 10, 1), range.From);
        Assert.Equal(new DateTime(2025, 11, 1), range.To);
    }

    [Fact]
    public void FromInclusiveDates_SameDay_RangeIsOneDay()
    {
        var range = DateRange.FromInclusiveDates(new DateTime(2025, 12, 31), new DateTime(2025, 12, 31));

        Assert.Equal(new DateRange(new DateTime(2025, 12, 31), new DateTime(2026, 1, 1)), range);
    }

    [Fact]
    public void Equals_SameBounds_ReturnsTrueAndSameHashCode()
    {
        var first = new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1));
        var second = new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1));

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentBoundsNullOrOtherType_ReturnsFalse()
    {
        var range = new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1));

        Assert.False(range.Equals(new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 10, 31))));
        Assert.False(range.Equals(new DateRange(new DateTime(2025, 10, 2), new DateTime(2025, 11, 1))));
        Assert.False(range.Equals(null));
        Assert.False(range.Equals(new DateTime(2025, 10, 1)));
    }

    [Fact]
    public void CompareTo_ByFromThenByTo()
    {
        var october = new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1));
        var octoberFirstHalf = new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 10, 16));
        var november = new DateRange(new DateTime(2025, 11, 1), new DateTime(2025, 12, 1));

        Assert.True(october.CompareTo(november) < 0);
        Assert.True(november.CompareTo(october) > 0);
        Assert.True(octoberFirstHalf.CompareTo(october) < 0);
        Assert.Equal(0, october.CompareTo(new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1))));
    }

    [Fact]
    public void CompareTo_Null_RangeIsGreater()
    {
        var range = new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1));

        Assert.Equal(1, range.CompareTo(null));
    }

    [Fact]
    public void Order_Ranges_SortedChronologically()
    {
        var november = new DateRange(new DateTime(2025, 11, 1), new DateTime(2025, 12, 1));
        var october = new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1));

        var ordered = new[] { november, october }.Order().ToList();

        Assert.Equal(new[] { october, november }, ordered);
    }
}
