using FinCtrl.Application;
using FinCtrl.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NLog.Extensions.Logging;

namespace FinCtrl.Website
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });

            var allowedOrigins = new[] {
                "http://localhost:3000",  // Vite
                "http://localhost:5173",  // Vite default
                "http://localhost:8080",  // Vue CLI
                "http://localhost:5174"   // Vite alternate
            };

            services.AddCors(options =>
            {
                options.AddPolicy("AllowVueApp", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            services.AddLogging(c =>
            {
                c.AddConsole();
                c.AddDebug();

                c.ClearProviders();
                c.AddNLog();
            });

            services.AddApplication();
            services.AddInfrastructure();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });
            }

            //app.UseHttpsRedirection();
            app.UseRouting();
            //app.UseAuthorization();

            // Then in the middleware pipeline
            app.UseCors("AllowVueApp");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
