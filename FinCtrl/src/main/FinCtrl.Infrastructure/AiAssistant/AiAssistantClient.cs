using System.Net.Http.Json;
using System.Text.Json;
using FinCtrl.Application.PaymentSources.Classification;
using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.AiAssistant;

/// <summary>
/// Клиент FinCtrl.AiAssistant: <c>POST /payment-sources/classify</c>,
/// тело <c>[{Id, Name}]</c> → <c>[{Id, Name, Classification}]</c> (имена полей в PascalCase).
/// </summary>
internal class AiAssistantClient(HttpClient httpClient) : IPaymentSourceClassifier
{
    private const string ClassifyPath = "payment-sources/classify";
    private const string UnavailableMessage = "Сервис классификации (AiAssistant) недоступен или вернул ошибку — источники остались без классификации";

    // Без политики имён: сервис ждёт PascalCase, а PostAsJsonAsync по умолчанию пишет camelCase.
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<PaymentSourceClassification>> Classify(
        IReadOnlyList<PaymentSource> paymentSources, CancellationToken cancellationToken)
    {
        var request = paymentSources.Select(x => new ClassifyRequestItem(x.PaymentSourceId, x.PaymentSourceName)).ToList();

        List<ClassifyResponseItem>? response;
        try
        {
            using var httpResponse = await _httpClient.PostAsJsonAsync(ClassifyPath, request, JsonOptions, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();
            response = await httpResponse.Content.ReadFromJsonAsync<List<ClassifyResponseItem>>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentSourceClassificationException(UnavailableMessage, ex);
        }
        catch (JsonException ex)
        {
            throw new PaymentSourceClassificationException(UnavailableMessage, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Отмена без запроса отмены — сработал таймаут HttpClient.
            throw new PaymentSourceClassificationException("Сервис классификации (AiAssistant) не ответил вовремя — источники остались без классификации", ex);
        }

        return (response ?? [])
            .Select(x => new PaymentSourceClassification(x.Id, x.Classification ?? string.Empty))
            .ToList();
    }

    private sealed record ClassifyRequestItem(int Id, string Name);

    private sealed record ClassifyResponseItem(int Id, string? Name, string? Classification);
}
