using FinCtrl.Application.Accounts;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Application.Tags;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Infrastructure.Accounts;
using FinCtrl.Infrastructure.Categories;
using FinCtrl.Infrastructure.Payments;
using FinCtrl.Infrastructure.PaymentsImport;
using FinCtrl.Infrastructure.PaymentSources;
using FinCtrl.Infrastructure.Tags;
using Microsoft.Extensions.DependencyInjection;

namespace FinCtrl.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>Регистрирует реализации Infrastructure: подключение к БД, репозитории, форматы файлов импорта.</summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

            services.AddScoped<IPaymentSourceRepository, PaymentSourceRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITagRepository, TagRepository>();

            services.AddScoped<IFileFormatFactory, FileFormatFactory>();

            return services;
        }
    }
}
