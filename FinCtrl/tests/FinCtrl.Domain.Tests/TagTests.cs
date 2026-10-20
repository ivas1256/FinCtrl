namespace FinCtrl.Domain.Tests;

public class TagTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    [Fact]
    public void CreateNew_Name_SetsNameAndCreatedAt()
    {
        var tag = Tag.CreateNew("Поездка", Now);

        Assert.Equal(0, tag.TagId);
        Assert.Equal("Поездка", tag.TagName);
        Assert.Equal(Now, tag.CreatedAt);
        Assert.Null(tag.LastUpdatedAt);
    }

    [Fact]
    public void CreateFromExisting_Fields_SetsIdAndNameWithoutDates()
    {
        var tag = Tag.CreateFromExisting(3, "Ремонт");

        Assert.Equal(3, tag.TagId);
        Assert.Equal("Ремонт", tag.TagName);
        Assert.Equal(default, tag.CreatedAt);
        Assert.Null(tag.LastUpdatedAt);
    }

    [Fact]
    public void Updated_SetsLastUpdatedAtToPassedTime()
    {
        var tag = new Tag(3, "Ремонт", Now.AddDays(-1), null);

        tag.Updated(Now);

        Assert.Equal(Now, tag.LastUpdatedAt);
    }
}
