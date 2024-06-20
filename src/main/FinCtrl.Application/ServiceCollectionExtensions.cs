using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;

namespace FinCtrl.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IPaymentSourceService, PaymentSourceService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IPaymentService, PaymentService>();

            services.AddScoped<IPaymentsImportService, PaymentsImportService>();

            return services;
        }
    }
}
