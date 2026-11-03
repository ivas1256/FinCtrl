using System.Net;
using System.Text;
using FinCtrl.Application.PaymentSources.Classification;
using FinCtrl.Domain;
using FinCtrl.Infrastructure.AiAssistant;

namespace FinCtrl.Infrastructure.Tests.AiAssistant;

public class AiAssistantClientTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    private readonly List<PaymentSource> _sources =
    [
        new(250, "Cleverfish", null, null, Now, null),
        new(251, "YANDEX TAXI", null, null, Now, null)
    ];

    [Fact]
    public async Task Classify_SendsPascalCaseBodyAndMapsResponse()
    {
        var handler = new StubHandler(HttpStatusCode.OK,
            """[{"Id":250,"Name":"Cleverfish","Classification":"Рыбный магазин"},{"Id":251,"Name":"YANDEX TAXI","Classification":"Такси"}]""");

        var result = await CreateClient(handler).Classify(_sources, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("http://ai.test/payment-sources/classify", handler.Request.RequestUri!.ToString());
        Assert.Equal("""[{"Id":250,"Name":"Cleverfish"},{"Id":251,"Name":"YANDEX TAXI"}]""", handler.Body);
        Assert.Equal(
            new[] { new PaymentSourceClassification(250, "Рыбный магазин"), new PaymentSourceClassification(251, "Такси") },
            result);
    }

    [Fact]
    public async Task Classify_ErrorStatus_ThrowsClassificationException()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError, """{"detail":"Внутренняя ошибка сервиса"}""");

        await Assert.ThrowsAsync<PaymentSourceClassificationException>(() => CreateClient(handler).Classify(_sources, CancellationToken.None));
    }

    [Fact]
    public async Task Classify_InvalidJson_ThrowsClassificationException()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "не json");

        await Assert.ThrowsAsync<PaymentSourceClassificationException>(() => CreateClient(handler).Classify(_sources, CancellationToken.None));
    }

    [Fact]
    public async Task Classify_ServiceUnavailable_ThrowsClassificationException()
    {
        var handler = new StubHandler(new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<PaymentSourceClassificationException>(() => CreateClient(handler).Classify(_sources, CancellationToken.None));
    }

    [Fact]
    public async Task Classify_Timeout_ThrowsClassificationException()
    {
        var handler = new StubHandler(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<PaymentSourceClassificationException>(() => CreateClient(handler).Classify(_sources, CancellationToken.None));
    }

    private static AiAssistantClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://ai.test/") });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody = string.Empty;
        private readonly Exception? _exception;

        public StubHandler(HttpStatusCode status, string responseBody) => (_status, _responseBody) = (status, responseBody);

        public StubHandler(Exception exception) => _exception = exception;

        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            if (_exception is not null)
                throw _exception;

            return new HttpResponseMessage(_status) { Content = new StringContent(_responseBody, Encoding.UTF8, "application/json") };
        }
    }
}
