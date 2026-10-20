namespace FinCtrl.Common;

/// <summary>Открытая транзакция. Освобождение без <see cref="Commit"/> откатывает изменения.</summary>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>Фиксирует изменения транзакции.</summary>
    Task Commit(CancellationToken cancellationToken = default);
}
