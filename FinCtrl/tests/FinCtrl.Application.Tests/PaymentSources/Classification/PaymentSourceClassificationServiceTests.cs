using FinCtrl.Application.PaymentSources;
using FinCtrl.Application.PaymentSources.Classification;
using FinCtrl.Domain;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinCtrl.Application.Tests.PaymentSources.Classification;

public class PaymentSourceClassificationServiceTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    private readonly IPaymentSourceClassifier _classifier = Substitute.For<IPaymentSourceClassifier>();
    private readonly IPaymentSourceRepository _repository = Substitute.For<IPaymentSourceRepository>();

    public PaymentSourceClassificationServiceTests()
    {
        // По умолчанию классификатор возвращает «Группа <id>» для каждого источника пачки.
        _classifier.Classify(Arg.Any<IReadOnlyList<PaymentSource>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyList<PaymentSource>>()
                .Select(s => new PaymentSourceClassification(s.PaymentSourceId, $"Группа {s.PaymentSourceId}"))
                .ToList());
    }

    [Fact]
    public async Task Classify_MoreThanBatchSize_SendsBatchesOfConfiguredSize()
    {
        var sources = Sources(65);
        var batchSizes = new List<int>();
        _classifier.When(x => x.Classify(Arg.Any<IReadOnlyList<PaymentSource>>(), Arg.Any<CancellationToken>()))
            .Do(call => batchSizes.Add(call.Arg<IReadOnlyList<PaymentSource>>().Count));

        var error = await CreateService(batchSize: 30).Classify(sources);

        Assert.Null(error);
        Assert.Equal(new[] { 30, 30, 5 }, batchSizes);
        await _repository.Received(3).UpdateClassifications(Arg.Any<IReadOnlyList<PaymentSource>>());
    }

    [Fact]
    public async Task Classify_Response_SetsAiClassificationAndSavesIt()
    {
        var sources = Sources(2);

        await CreateService().Classify(sources);

        Assert.All(sources, s => Assert.True(s.IsAiClassified));
        Assert.Equal(new[] { "Группа 1", "Группа 2" }, sources.Select(s => s.Classification));
        await _repository.Received(1).UpdateClassifications(Arg.Is<IReadOnlyList<PaymentSource>>(x => x.Count == 2));
    }

    [Fact]
    public async Task Classify_EmptyList_DoesNotCallClassifier()
    {
        var error = await CreateService().Classify([]);

        Assert.Null(error);
        await _classifier.DidNotReceiveWithAnyArgs().Classify(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().UpdateClassifications(default!);
    }

    [Fact]
    public async Task Classify_UnknownIdsAndBlankClassifications_AreIgnored()
    {
        var sources = Sources(2);
        _classifier.Classify(Arg.Any<IReadOnlyList<PaymentSource>>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentSourceClassification> { new(1, "  "), new(2, "Кафе"), new(99, "Чужой") });

        await CreateService().Classify(sources);

        Assert.Null(sources[0].Classification);
        Assert.False(sources[0].IsAiClassified);
        Assert.Equal("Кафе", sources[1].Classification);
        await _repository.Received(1).UpdateClassifications(Arg.Is<IReadOnlyList<PaymentSource>>(x => x.Single() == sources[1]));
    }

    [Fact]
    public async Task Classify_ClassifierFailsOnSecondBatch_ReturnsErrorAndKeepsFirstBatch()
    {
        var sources = Sources(3);
        _classifier.Classify(Arg.Is<IReadOnlyList<PaymentSource>>(x => x[0].PaymentSourceId == 3), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PaymentSourceClassificationException("сервис недоступен"));

        var error = await CreateService(batchSize: 2).Classify(sources);

        Assert.Equal("сервис недоступен", error);
        Assert.True(sources[1].IsAiClassified);
        Assert.Null(sources[2].Classification);
        await _repository.Received(1).UpdateClassifications(Arg.Any<IReadOnlyList<PaymentSource>>());
    }

    private PaymentSourceClassificationService CreateService(int batchSize = 30) =>
        new(_classifier, _repository,
            Options.Create(new PaymentSourceClassificationOptions { BatchSize = batchSize }),
            new FakeLogger<PaymentSourceClassificationService>());

    private static List<PaymentSource> Sources(int count) =>
        Enumerable.Range(1, count).Select(id => new PaymentSource(id, $"Источник {id}", null, null, Now, null)).ToList();
}
