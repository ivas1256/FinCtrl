using FinCtrl.Application.Accounts;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Application.PaymentSources.Classification;
using FinCtrl.Application.Receipts;
using FinCtrl.Application.Tags;
using FinCtrl.Application.Transfers;
using FinCtrl.Common;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Infrastructure.Accounts;
using FinCtrl.Infrastructure.AiAssistant;
using FinCtrl.Infrastructure.Categories;
using FinCtrl.Infrastructure.Payments;
using FinCtrl.Infrastructure.PaymentsImport;
using FinCtrl.Infrastructure.PaymentSources;
using FinCtrl.Infrastructure.Receipts;
using FinCtrl.Infrastructure.Tags;
using FinCtrl.Infrastructure.Transfers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FinCtrl.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует реализации Infrastructure: подключение к БД, репозитории, форматы файлов импорта,
        /// HTTP-клиент FinCtrl.AiAssistant (настройки <see cref="AiAssistantOptions"/> привязывает вызывающий).
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
            // Транзакция живёт в соединении scope, поэтому IUnitOfWork — та же фабрика, а не отдельный экземпляр.
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IDbConnectionFactory>());

            services.AddScoped<IPaymentSourceRepository, PaymentSourceRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<ITransferRepository, TransferRepository>();
            services.AddScoped<IReceiptItemRepository, ReceiptItemRepository>();

            services.AddScoped<IFileFormatFactory, FileFormatFactory>();

            services.AddHttpClient<IPaymentSourceClassifier, AiAssistantClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<AiAssistantOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

            return services;
        }
    }
}
