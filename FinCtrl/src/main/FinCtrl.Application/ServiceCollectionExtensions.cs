using Microsoft.Extensions.DependencyInjection;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Application.Statistic;
using FinCtrl.Application.Dashboard;
using FinCtrl.Application.Accounts;
using FinCtrl.Application.Tags;

namespace FinCtrl.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);

            services.AddScoped<IPaymentSourceService, PaymentSourceService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ITagService, TagService>();

            services.AddScoped<IPaymentsImportService, PaymentsImportService>();

            services.AddScoped<IMainStatisticService, MainStatisticService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}
