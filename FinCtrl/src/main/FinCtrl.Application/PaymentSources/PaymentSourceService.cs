using FinCtrl.Application.Categories;
using FinCtrl.Common.Application;
using FinCtrl.Domain;

namespace FinCtrl.Application.PaymentSources
{
    internal class PaymentSourceService : IPaymentSourceService
    {
        private readonly IPaymentSourceRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly TimeProvider _timeProvider;

        public PaymentSourceService(IPaymentSourceRepository repository, ICategoryRepository categoryRepository, TimeProvider timeProvider)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _timeProvider = timeProvider;
        }

        // Категории источников репозиторий загружает сам (JOIN), догружать их здесь не нужно.
        public async Task<List<PaymentSource>> GetAll()
        {
            return (await _repository.GetAll())
                .OrderByDescending(x => x.PaymentSourceId)
                .ToList();
        }

        public async Task<PaymentSource> GetById(int paymentSourceId)
        {
            return await _repository.GetById(paymentSourceId)
                ?? throw new NotFoundException($"Источник платежа {paymentSourceId} не найден");
        }

        public async Task<PaymentSource?> GetByName(string paymentSourceName)
        {
            return await _repository.GetByName(paymentSourceName);
        }

        public async Task Edit(int paymentSourceId, string paymentSourceName, int? categoryId, string? description, string? classification)
        {
            if (string.IsNullOrWhiteSpace(paymentSourceName))
                throw new ValidationException("Название источника обязательно");

            // Правим загруженный источник, а не собранный заново: иначе потеряется признак ИИ-классификации.
            var toUpdate = await GetById(paymentSourceId);

            if (categoryId is int id && await _categoryRepository.GetById(id) is null)
                throw new ValidationException($"Категория {id} не найдена");

            toUpdate.PaymentSourceName = paymentSourceName;
            // Для UPDATE репозиторию нужен только id категории — загружать её целиком незачем.
            toUpdate.PaymentSourceCategory = categoryId.HasValue ? new Category(categoryId.Value) : null;
            toUpdate.Description = description;
            toUpdate.SetClassificationByUser(classification);
            toUpdate.Updated(_timeProvider.GetLocalNow().DateTime);

            await _repository.Update(toUpdate);
        }

        public async Task<int> Create(PaymentSource paymentSource)
        {
            return await _repository.Create(paymentSource);
        }

        public async Task<(List<PaymentSource> Items, int TotalCount)> GetListPaged(
            string? search, bool noCategoryOnly, int page, int? pageSize)
        {
            Paging.Validate(page, pageSize);

            return await _repository.GetListPaged(search, noCategoryOnly, page, pageSize);
        }
    }
}
