using Dapper.FluentMap;
using Dapper.FluentMap.Dommel;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Infrastructure.Categories;
using FinCtrl.Infrastructure.Mapping;
using FinCtrl.Infrastructure.Payments;
using FinCtrl.Infrastructure.PaymentSources;
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
            AddMappings();

            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

            services.AddScoped<IPaymentSourceRepository, PaymentSourceRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            return services;    
        }

        private static void AddMappings()
        {
            FluentMapper.Initialize(config =>
            {
                config.AddMap(new PaymentSourceMap());
                config.AddMap(new CategoryMap());
                config.AddMap(new PaymentMap());
                config.ForDommel();
            });
        }
    }
}
