using FinCtrl.Common.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Website.ErrorHandling;

/// <summary>
/// Глобальный обработчик исключений API: исключения приложения превращаются в ProblemDetails с кодом
/// 400/404/409, непредвиденные — в 500 без подробностей (подробности — в лог, а в Development — и в ответ).
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment environment,
        ILogger<ApiExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>Пишет ответ ProblemDetails для исключения.</summary>
    /// <returns><c>true</c> — ответ записан.</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Клиент закрыл соединение (например, отменил загрузку файла) — отвечать некому, это не ошибка сервера.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var problem = exception switch
        {
            ValidationException => CreateProblem(StatusCodes.Status400BadRequest, "Некорректные данные", exception.Message),
            NotFoundException => CreateProblem(StatusCodes.Status404NotFound, "Не найдено", exception.Message),
            ConflictException => CreateProblem(StatusCodes.Status409Conflict, "Конфликт данных", exception.Message),
            _ => null
        };

        if (problem is null)
        {
            _logger.LogError(exception, "Необработанная ошибка {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            problem = CreateProblem(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера",
                _environment.IsDevelopment() ? exception.Message : "Попробуйте повторить действие позже");
        }
        else
        {
            // Ожидаемая ошибка запроса — без стека, только для истории в логе
            _logger.LogInformation("Ошибка запроса {Method} {Path}: {Status} {Detail}",
                httpContext.Request.Method, httpContext.Request.Path, problem.Status, problem.Detail);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });

        // Писатель ProblemDetails отказывается, если клиент не принимает JSON (Accept: text/html) — отвечаем JSON всё равно,
        // иначе middleware отдаст пустой 500 и потеряет код 400/404/409.
        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null,
                "application/problem+json", cancellationToken);
        }

        return true;
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };
}
