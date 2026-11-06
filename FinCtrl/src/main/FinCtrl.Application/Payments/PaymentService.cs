using FinCtrl.Application.Accounts;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Tags;
using FinCtrl.Application.Transfers;
using FinCtrl.Common;
using FinCtrl.Common.Application;
using FinCtrl.Domain;

namespace FinCtrl.Application.Payments
{
    internal class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ITransferRepository _transferRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;

        public PaymentService(IPaymentRepository paymentRepository,
                              ICategoryRepository categoryRepository,
                              IAccountRepository accountRepository,
                              ITagRepository tagRepository,
                              ITransferRepository transferRepository,
                              IUnitOfWork unitOfWork,
                              TimeProvider timeProvider)
        {
            _paymentRepository = paymentRepository;
            _categoryRepository = categoryRepository;
            _accountRepository = accountRepository;
            _tagRepository = tagRepository;
            _transferRepository = transferRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
        }

        public async Task Edit(PaymentEdit edit)
        {
            var payment = await Get(edit.PaymentId);
            Apply(payment, edit);

            // Все проверки — до записи: иначе платёж сохранился бы, а теги — нет (и ответ был бы ошибкой).
            var transferChange = await PlanTransferChange(payment, edit);
            await EnsureReferencesExist([payment]);
            if (edit.TagIds is not null)
            {
                await EnsureTagsExist(edit.TagIds);
            }

            var now = _timeProvider.GetLocalNow().DateTime;
            payment.Updated(now);

            await using var transaction = await _unitOfWork.BeginTransaction();
            await _paymentRepository.Update(payment);
            if (edit.TagIds is not null)
            {
                await _paymentRepository.SetPaymentTags(payment.PaymentId, edit.TagIds);
            }
            await ApplyTransferChange(payment, transferChange, now);
            await transaction.Commit();
        }

        public async Task<List<Payment>> GetTransferCandidates(int paymentId)
        {
            var payment = await Get(paymentId);
            if (payment.PaymentSum == 0)
                return [];

            var day = payment.PaymentDate.Date;
            var candidates = await _paymentRepository.GetTransferCandidates(
                paymentId, Math.Abs(payment.PaymentSum), day.AddDays(-TransferCandidateDays), day.AddDays(TransferCandidateDays + 1),
                payment.Account?.AccountId);

            // Знак — у нормализованного платежа: в БД есть расходы, сохранённые с положительной суммой.
            return candidates
                .Where(c => Math.Sign(c.PaymentSum) == -Math.Sign(payment.PaymentSum))
                .OrderBy(c => Math.Abs((c.PaymentDate - payment.PaymentDate).Ticks))
                .ThenBy(c => c.PaymentId)
                .ToList();
        }

        /// <summary>На сколько дней в обе стороны от даты платежа ищется вторая сторона перевода.</summary>
        private const int TransferCandidateDays = 5;

        private enum TransferAction
        {
            /// <summary>Связи не меняются.</summary>
            None,
            /// <summary>Платёж стал переводом без второй стороны.</summary>
            MarkWithoutPair,
            /// <summary>Платёж перестал быть переводом: связь удаляется.</summary>
            Remove,
            /// <summary>Вторая сторона отвязана, платёж остаётся переводом.</summary>
            Unlink,
            /// <summary>Платёж связывается со второй стороной.</summary>
            Link
        }

        private sealed record TransferChange(TransferAction Action, Transfer? CurrentRow = null, Payment? Pair = null, Transfer? PairRow = null);

        /// <summary>
        /// Меняет тип платежа по <see cref="PaymentEdit.IsTransfer"/> и решает, что сделать со связями. Только чтение и проверки —
        /// запись в <see cref="ApplyTransferChange"/>.
        /// </summary>
        private async Task<TransferChange> PlanTransferChange(Payment payment, PaymentEdit edit)
        {
            var wasTransfer = payment.PaymentType == PaymentTypeEnum.Transfer;
            var isTransfer = edit.IsTransfer ?? wasTransfer;

            if (edit.TransferPairPaymentId is not null && !isTransfer)
                throw new ValidationException("Вторую сторону можно указать только у перевода");
            if (edit.IsTransfer is null && edit.TransferPairPaymentId is null)
                return new TransferChange(TransferAction.None);

            var currentRow = await _transferRepository.GetByPaymentId(payment.PaymentId);

            if (!isTransfer)
            {
                if (!wasTransfer)
                    return new TransferChange(TransferAction.None);

                payment.PaymentType = payment.PaymentSum <= 0 ? PaymentTypeEnum.Spending : PaymentTypeEnum.Income;
                return new TransferChange(TransferAction.Remove, currentRow);
            }

            payment.PaymentType = PaymentTypeEnum.Transfer;

            switch (edit.TransferPairPaymentId)
            {
                case null:
                    return new TransferChange(wasTransfer ? TransferAction.None : TransferAction.MarkWithoutPair, currentRow);
                case 0:
                    return new TransferChange(TransferAction.Unlink, currentRow);
            }

            var pairId = edit.TransferPairPaymentId.Value;
            if (pairId == payment.PaymentId)
                throw new ValidationException("Платёж не может быть второй стороной своего перевода");

            var pair = await _paymentRepository.Get(pairId)
                ?? throw new ValidationException($"Вторая сторона перевода — операция {pairId} — не найдена");
            if (payment.PaymentSum == 0 || Math.Sign(pair.PaymentSum) != -Math.Sign(payment.PaymentSum))
                throw new ValidationException("У сторон перевода должны быть суммы противоположного знака");

            var pairRow = await _transferRepository.GetByPaymentId(pairId);
            if (pairRow is not null && pairRow.TransferId == currentRow?.TransferId)
                return new TransferChange(TransferAction.None); // уже связаны друг с другом

            if (pairRow?.InPaymentId is not null)
                throw new ConflictException($"Операция {pairId} уже связана с другой операцией перевода");

            return new TransferChange(TransferAction.Link, currentRow, pair, pairRow);
        }

        private async Task ApplyTransferChange(Payment payment, TransferChange change, DateTime now)
        {
            switch (change.Action)
            {
                case TransferAction.MarkWithoutPair:
                    if (change.CurrentRow is null)
                        await CreateWithoutPairIfOutgoing(payment, now);
                    break;

                case TransferAction.Remove:
                    await Detach(payment.PaymentId, change.CurrentRow, now);
                    break;

                case TransferAction.Unlink:
                    await Detach(payment.PaymentId, change.CurrentRow, now);
                    await CreateWithoutPairIfOutgoing(payment, now);
                    break;

                case TransferAction.Link:
                    var pair = change.Pair!;
                    await Detach(payment.PaymentId, change.CurrentRow, now);
                    await Detach(pair.PaymentId, change.PairRow, now);

                    var (outgoing, incoming) = payment.PaymentSum < 0 ? (payment, pair) : (pair, payment);
                    await _transferRepository.Create(new Transfer
                    {
                        OutPaymentId = outgoing.PaymentId,
                        InPaymentId = incoming.PaymentId,
                        CreatedAt = now
                    });

                    pair.PaymentType = PaymentTypeEnum.Transfer;
                    pair.Updated(now);
                    await _paymentRepository.Update(pair);
                    break;
            }
        }

        // Строка Transfers без поступления (InPaymentId = NULL) — перевод-списание без второй стороны.
        // Поступление без второй стороны строки не имеет: OutPaymentId обязателен; оно — перевод только по типу.
        private async Task CreateWithoutPairIfOutgoing(Payment payment, DateTime now)
        {
            if (payment.PaymentSum < 0)
            {
                await _transferRepository.Create(new Transfer { OutPaymentId = payment.PaymentId, InPaymentId = null, CreatedAt = now });
            }
        }

        /// <summary>
        /// Удаляет связь платежа. Бывшая вторая сторона остаётся переводом без пары: если это списание — с собственной строкой.
        /// </summary>
        private async Task Detach(int paymentId, Transfer? row, DateTime now)
        {
            if (row is null)
                return;

            await _transferRepository.Delete(row.TransferId);

            var other = row.GetPairPaymentId(paymentId);
            if (other is not null && other == row.OutPaymentId)
            {
                await _transferRepository.Create(new Transfer { OutPaymentId = other.Value, InPaymentId = null, CreatedAt = now });
            }
        }

        private static void Apply(Payment payment, PaymentEdit edit)
        {
            if (edit.Description is not null)
            {
                payment.PaymentDescription = string.IsNullOrWhiteSpace(edit.Description) ? null : edit.Description;
            }

            if (edit.ExcludeFromStats is bool excludeFromStats)
            {
                payment.ExcludeFromStats = excludeFromStats;
            }

            // Категория источника в платёж не пишется: итоговая категория и так возьмётся из источника.
            if (edit.CategoryId is int categoryId
                && categoryId != payment.PaymentSource?.PaymentSourceCategory?.CategoryId)
            {
                payment.PaymentCategory = new Category(categoryId);
            }

            if (edit.AccountId is int accountId)
            {
                payment.Account = accountId == 0 ? null : new Account { AccountId = accountId };
            }
        }

        public async Task<Payment> Get(int id)
        {
            return await _paymentRepository.Get(id)
                ?? throw new NotFoundException($"Операция {id} не найдена");
        }

        public async Task<int> Create(Payment payment)
        {
            return await _paymentRepository.Create(payment);
        }

        public async Task Update(params Payment[] payments)
        {
            await EnsureReferencesExist(payments);

            var now = _timeProvider.GetLocalNow().DateTime;
            foreach (var payment in payments)
            {
                payment.Updated(now);
            }

            await _paymentRepository.Update(payments);
        }

        public async Task<Payment?> FindDuplicate(decimal paymentSum, DateTime paymentDate, int? accountId, PaymentTypeEnum paymentType)
        {
            // Расход с положительной суммой хранится как в файле, пока его не поправят: Update пишет нормализованную сумму
            IReadOnlyCollection<decimal> sums = paymentType == PaymentTypeEnum.Spending && paymentSum > 0
                ? [paymentSum, -paymentSum]
                : [paymentSum];
            var candidates = await _paymentRepository.GetByDateAndSums(sums, paymentDate);

            if (accountId is null)
                return candidates.FirstOrDefault();

            // Совпадение на том же счёте проверяется первым: иначе дозаполнение счёта у платежа без счёта
            // создало бы второй платёж с тем же ключом «счёт + дата + сумма» (UQ_Payments_Account_Date_Sum)
            return candidates.FirstOrDefault(p => p.Account?.AccountId == accountId)
                ?? candidates.FirstOrDefault(p => p.Account is null);
        }

        public async Task<List<Payment>> GetByDateRange(DateRange dateRange, int[]? tagIds = null)
        {
            return await _paymentRepository.GetByDateRange(dateRange, tagIds);
        }

        public async Task<List<Payment>> GetBySource(int PaymentSourceId)
        {
            return await _paymentRepository.GetBySource(PaymentSourceId);
        }

        public async Task<(List<Payment> Items, int TotalCount)> GetListPaged(
            DateTime? from, DateTime? to, int? sourceId, bool noCategoryOnly,
            string sortBy, string sortDir, int page, int? pageSize, int[]? tagIds = null)
        {
            Paging.Validate(page, pageSize);

            return await _paymentRepository.GetListPaged(from, to, sourceId, noCategoryOnly, sortBy, sortDir, page, pageSize, tagIds);
        }

        public async Task SetPaymentTags(int paymentId, IReadOnlyCollection<int> tagIds)
        {
            await EnsureTagsExist(tagIds);

            await _paymentRepository.SetPaymentTags(paymentId, tagIds);
        }

        public async Task<int> BulkAssignTags(DateRange dateRange, List<int> categoryIds, List<int> tagIds)
        {
            if (tagIds.Count == 0)
                throw new ValidationException("Не выбраны теги");
            if (dateRange.From >= dateRange.To)
                throw new ValidationException("Начало периода должно быть раньше его конца");

            await EnsureTagsExist(tagIds);

            var payments = await _paymentRepository.GetByDateRange(dateRange);
            // Пустой список категорий — фильтра нет: все платежи периода, в т.ч. без категории.
            var matchingIds = payments
                .Where(p => categoryIds.Count == 0
                    || (p.CalculatedCategory != null && categoryIds.Contains(p.CalculatedCategory.CategoryId)))
                .Select(p => p.PaymentId)
                .ToList();

            await _paymentRepository.AddTagsToPayments(matchingIds, tagIds);

            return matchingIds.Count;
        }

        // Проверка до записи: иначе несуществующий id упадёт на внешнем ключе в БД (500 вместо понятной ошибки).
        private async Task EnsureReferencesExist(IEnumerable<Payment> payments)
        {
            var categoryIds = payments.Select(p => p.PaymentCategory?.CategoryId).OfType<int>().Distinct();
            foreach (var categoryId in categoryIds)
            {
                if (await _categoryRepository.GetById(categoryId) is null)
                    throw new ValidationException($"Категория {categoryId} не найдена");
            }

            var accountIds = payments.Select(p => p.Account?.AccountId).OfType<int>().Distinct();
            foreach (var accountId in accountIds)
            {
                if (await _accountRepository.GetById(accountId) is null)
                    throw new ValidationException($"Счёт {accountId} не найден");
            }
        }

        private async Task EnsureTagsExist(IEnumerable<int> tagIds)
        {
            var requested = tagIds.Distinct().ToList();
            if (requested.Count == 0)
                return;

            // Тегов единицы — проще взять все, чем заводить отдельный запрос по списку id.
            var existing = (await _tagRepository.GetAll()).Select(t => t.TagId).ToHashSet();
            var missing = requested.Where(id => !existing.Contains(id)).ToList();
            if (missing.Count > 0)
                throw new ValidationException($"Не найдены теги: {string.Join(", ", missing)}");
        }
    }
}
