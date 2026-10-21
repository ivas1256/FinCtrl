using FinCtrl.Application.Tags;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.Tags;

public class TagServiceTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    private readonly ITagRepository _repository = Substitute.For<ITagRepository>();
    private readonly TagService _service;
    private readonly Tag _trip = new(1, "Поездка", Now, null);

    public TagServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _repository.GetById(1).Returns(_trip);
        _repository.GetByName("Поездка").Returns(_trip);

        _service = new TagService(_repository, timeProvider);
    }

    [Fact]
    public async Task GetById_UnknownTag_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetById(99));
    }

    [Fact]
    public async Task Create_NewName_CreatesTag()
    {
        _repository.Create(Arg.Any<Tag>()).Returns(2);

        Assert.Equal(2, await _service.Create(Tag.CreateNew("Ремонт", Now)));
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsConflictException()
    {
        _repository.Exists("Поездка").Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.Create(Tag.CreateNew("Поездка", Now)));

        await _repository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Create_EmptyName_ThrowsValidationException(string name)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.Create(Tag.CreateNew(name, Now)));

        await _repository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Fact]
    public async Task Create_TooLongName_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.Create(Tag.CreateNew(new string('т', TagService.MaxNameLength + 1), Now)));
    }

    [Fact]
    public async Task Update_SameNameSameTag_Saves()
    {
        await _service.Update(Tag.CreateFromExisting(1, "Поездка"));

        await _repository.Received(1).Update(Arg.Is<Tag[]>(t => t.Length == 1 && t[0].LastUpdatedAt == Now));
    }

    [Fact]
    public async Task Update_NameOfAnotherTag_ThrowsConflictException()
    {
        _repository.GetById(2).Returns(new Tag(2, "Ремонт", Now, null));

        await Assert.ThrowsAsync<ConflictException>(() => _service.Update(Tag.CreateFromExisting(2, "Поездка")));

        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Update_UnknownTag_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Update(Tag.CreateFromExisting(99, "Ремонт")));

        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Delete_ExistingTag_Deletes()
    {
        await _service.Delete(1);

        await _repository.Received(1).Delete(Arg.Is<Tag[]>(t => t.Length == 1 && t[0].TagId == 1));
    }

    [Fact]
    public async Task Delete_UnknownTag_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Delete(99));

        await _repository.DidNotReceiveWithAnyArgs().Delete();
    }
}
