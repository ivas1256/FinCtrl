using FinCtrl.Application;
using FinCtrl.Application.PaymentSources.Classification;
using FinCtrl.Infrastructure;
using FinCtrl.Infrastructure.AiAssistant;
using FinCtrl.Website.ErrorHandling;
using Microsoft.OpenApi.Models;
using NLog.Extensions.Logging;

namespace FinCtrl.Website
{
    public class Startup
    {
        private const string CorsPolicyName = "AllowVueApp";

        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                // Nullable в Website включён, но контракт API не меняем: обязательность полей задаётся явно
                // атрибутами ([Required]), а не неявно по non-nullable ссылочным типам
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            });
            // Ошибки API — в формате ProblemDetails (RFC 7807): исключения приложения → 400/404/409, прочие → 500
            services.AddProblemDetails();
            services.AddExceptionHandler<ApiExceptionHandler>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinCtrl API", Version = "v1" });
            });

            // Адреса dev-серверов SPA, которым разрешены запросы к API (секция Cors:AllowedOrigins в appsettings)
            var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            services.AddLogging(c =>
            {
                c.ClearProviders();
                c.AddNLog();
            });

            // Секция AiAssistant: размер пачки нужен Application, адрес и таймаут — HTTP-клиенту Infrastructure.
            var aiAssistantSection = _configuration.GetSection(PaymentSourceClassificationOptions.SectionName);
            services.Configure<PaymentSourceClassificationOptions>(aiAssistantSection);
            services.Configure<AiAssistantOptions>(aiAssistantSection);

            services.AddApplication();
            services.AddInfrastructure();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Вместо страницы исключения — ProblemDetails и в Development (фронтенд показывает detail пользователю).
            app.UseExceptionHandler();
            // Пустые 4xx/5xx (неизвестный маршрут, 405) — тоже ProblemDetails.
            app.UseStatusCodePages();

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinCtrl API V1");
                });
            }

            app.UseRouting();
            app.UseCors(CorsPolicyName);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
