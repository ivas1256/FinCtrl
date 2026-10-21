using FinCtrl.Application.Accounts;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Tags;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;

        public PaymentService(IPaymentRepository paymentRepository,
                              ICategoryRepository categoryRepository,
                              IAccountRepository accountRepository,
                              ITagRepository tagRepository,
                              IUnitOfWork unitOfWork,
                              TimeProvider timeProvider)
        {
            _paymentRepository = paymentRepository;
            _categoryRepository = categoryRepository;
            _accountRepository = accountRepository;
            _tagRepository = tagRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
        }

        public async Task Edit(PaymentEdit edit)
        {
            var payment = await Get(edit.PaymentId);
            Apply(payment, edit);

            // Все проверки — до записи: иначе платёж сохранился бы, а теги — нет (и ответ был бы ошибкой).
            await EnsureReferencesExist([payment]);
            if (edit.TagIds is not null)
            {
                await EnsureTagsExist(edit.TagIds);
            }

            payment.Updated(_timeProvider.GetLocalNow().DateTime);

            await using var transaction = await _unitOfWork.BeginTransaction();
            await _paymentRepository.Update(payment);
            if (edit.TagIds is not null)
            {
                await _paymentRepository.SetPaymentTags(payment.PaymentId, edit.TagIds);
            }
            await transaction.Commit();
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

        public async Task<Payment?> FindDuplicate(decimal paymentSum, DateTime paymentDate)
        {
            return await _paymentRepository.FindDuplicate(paymentSum, paymentDate);
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
            if (categoryIds.Count == 0)
                throw new ValidationException("Не выбраны категории");
            if (tagIds.Count == 0)
                throw new ValidationException("Не выбраны теги");
            if (dateRange.From >= dateRange.To)
                throw new ValidationException("Начало периода должно быть раньше его конца");

            await EnsureTagsExist(tagIds);

            var payments = await _paymentRepository.GetByDateRange(dateRange);
            var matchingIds = payments
                .Where(p => p.CalculatedCategory != null && categoryIds.Contains(p.CalculatedCategory.CategoryId))
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
