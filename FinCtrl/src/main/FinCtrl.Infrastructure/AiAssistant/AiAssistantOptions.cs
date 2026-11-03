namespace FinCtrl.Infrastructure.AiAssistant;

/// <summary>Подключение к FinCtrl.AiAssistant (секция <c>AiAssistant</c> в appsettings).</summary>
public class AiAssistantOptions
{
    /// <summary>Адрес сервиса.</summary>
    public string BaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>Таймаут одного запроса: LLM отвечает на пачку десятки секунд.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
