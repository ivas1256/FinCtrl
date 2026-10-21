using FinCtrl.Application.Tags;
using FinCtrl.Domain;
using FinCtrl.Website.Controllers.TagsApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Website.Tests.Controllers.TagsApi;

public class TagsApiControllerTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    private readonly ITagService _tagService = Substitute.For<ITagService>();
    private readonly TagsApiController _controller;

    public TagsApiControllerTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));
        _controller = new TagsApiController(_tagService, timeProvider);
    }

    [Fact]
    public async Task Create_Dto_CreatesTagWithTimeAndReturnsSaved()
    {
        _tagService.Create(Arg.Any<Tag>()).Returns(3);
        _tagService.GetById(3).Returns(new Tag(3, "Поездка", Now, null));

        var result = await _controller.Create(new TagDto { TagId = 99, TagName = "Поездка" });

        await _tagService.Received(1).Create(Arg.Is<Tag>(t => t.TagId == 0 && t.TagName == "Поездка" && t.CreatedAt == Now));
        var saved = Assert.IsType<TagDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal((3, "Поездка", Now), (saved.TagId, saved.TagName, saved.CreatedAt));
    }

    [Fact]
    public async Task Update_RouteId_UpdatesTagWithRouteIdAndReturnsNoContent()
    {
        var result = await _controller.Update(3, new TagDto { TagId = 99, TagName = "Отпуск" });

        Assert.IsType<NoContentResult>(result);
        await _tagService.Received(1).Update(Arg.Is<Tag>(t => t.TagId == 3 && t.TagName == "Отпуск"));
    }

    [Fact]
    public async Task GetAll_Tags_MapsToDtos()
    {
        _tagService.GetAll().Returns(new List<Tag> { new(3, "Поездка", Now, null) });

        var result = await _controller.GetAll();

        var tags = Assert.IsAssignableFrom<IEnumerable<TagDto>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Поездка", Assert.Single(tags).TagName);
    }
}
