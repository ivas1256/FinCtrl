using FinCtrl.Common.Infrastructure;
using FinCtrl.Infrastructure.Accounts;
using FinCtrl.Infrastructure.Categories;
using FinCtrl.Infrastructure.Payments;
using FinCtrl.Infrastructure.PaymentSources;
using FinCtrl.Infrastructure.Tags;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

            services.AddScoped<IPaymentSourceRepository, PaymentSourceRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITagRepository, TagRepository>();

            return services;
        }
    }
}
