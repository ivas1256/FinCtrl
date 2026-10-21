namespace FinCtrl.Common;

/// <summary>Граница транзакции для сценариев, которые пишут в несколько таблиц/репозиториев.</summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Начинает транзакцию: все операции репозиториев в этом scope выполняются в ней до <see cref="ITransaction.Commit"/>
    /// или освобождения. Если транзакция уже открыта, возвращает вложенную: её <c>Commit</c> ничего не делает,
    /// фиксирует внешняя.
    /// </summary>
    Task<ITransaction> BeginTransaction(CancellationToken cancellationToken = default);
}
