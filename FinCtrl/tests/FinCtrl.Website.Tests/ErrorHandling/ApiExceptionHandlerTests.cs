using System.Text.Json;
using FinCtrl.Common.Application;
using FinCtrl.Website.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FinCtrl.Website.Tests.ErrorHandling;

public class ApiExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetailsService = Substitute.For<IProblemDetailsService>();
    private readonly IHostEnvironment _environment = Substitute.For<IHostEnvironment>();
    private readonly ApiExceptionHandler _handler;
    private ProblemDetailsContext? _written;

    public ApiExceptionHandlerTests()
    {
        _environment.EnvironmentName.Returns(Environments.Production);
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(c => _written = c)).Returns(ValueTask.FromResult(true));
        _handler = new ApiExceptionHandler(_problemDetailsService, _environment, NullLogger<ApiExceptionHandler>.Instance);
    }

    public static TheoryData<Exception, int, string> ApplicationExceptions => new()
    {
        { new ValidationException("Название тега обязательно"), StatusCodes.Status400BadRequest, "Некорректные данные" },
        { new NotFoundException("Тег 5 не найден"), StatusCodes.Status404NotFound, "Не найдено" },
        { new ConflictException("Тег «Поездка» уже существует"), StatusCodes.Status409Conflict, "Конфликт данных" }
    };

    [Theory]
    [MemberData(nameof(ApplicationExceptions))]
    public async Task TryHandleAsync_ApplicationException_WritesProblemWithStatusAndMessage(Exception exception, int status, string title)
    {
        var context = new DefaultHttpContext();

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(status, context.Response.StatusCode);
        Assert.NotNull(_written);
        Assert.Same(context, _written.HttpContext);
        Assert.Same(exception, _written.Exception);
        Assert.Equal(status, _written.ProblemDetails.Status);
        Assert.Equal(title, _written.ProblemDetails.Title);
        Assert.Equal(exception.Message, _written.ProblemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedExceptionInProduction_HidesDetails()
    {
        var context = new DefaultHttpContext();

        await _handler.TryHandleAsync(context, new InvalidOperationException("строка подключения: секрет"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("Внутренняя ошибка сервера", _written!.ProblemDetails.Title);
        Assert.Equal("Попробуйте повторить действие позже", _written.ProblemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedExceptionInDevelopment_ShowsMessage()
    {
        _environment.EnvironmentName.Returns(Environments.Development);
        var context = new DefaultHttpContext();

        await _handler.TryHandleAsync(context, new InvalidOperationException("Сломалось"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("Сломалось", _written!.ProblemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_RequestAbortedByClient_Returns499WithoutBody()
    {
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        var context = new DefaultHttpContext { RequestAborted = aborted.Token };

        var handled = await _handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, context.Response.StatusCode);
        await _problemDetailsService.DidNotReceiveWithAnyArgs().TryWriteAsync(default!);
    }

    [Fact]
    public async Task TryHandleAsync_CancelledButRequestNotAborted_TreatedAsServerError()
    {
        var context = new DefaultHttpContext();

        await _handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WriterRefused_WritesProblemJsonItself()
    {
        _problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(ValueTask.FromResult(false));
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        var handled = await _handler.TryHandleAsync(context, new NotFoundException("Тег 5 не найден"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body.ToArray(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(404, problem.Status);
        Assert.Equal("Тег 5 не найден", problem.Detail);
    }
}
