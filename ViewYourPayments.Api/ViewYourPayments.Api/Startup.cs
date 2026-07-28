using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reflection;
using ViewYourPayments.Api.Extensions;
using ViewYourPayments.Api.Services;
using ViewYourPayments.Core.AppConfig;
using ViewYourPayments.Core.Interfaces;
using ViewYourPayments.Core.Interfaces.Repositories;
using ViewYourPayments.Core.Interfaces.Services;
using ViewYourPayments.Core.Services;
using ViewYourPayments.EntityFramework;
using ViewYourPayments.Infrastructure.Repositories;

namespace ViewYourPayments.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();
            var connectionString = Configuration.GetConnectionString("ViewYourPaymentsDbContext");
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.AddDbContext<ViewYourPaymentsDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IPaymentSummaryRepository,PaymentSummaryRepository>();
            services.AddScoped<IPaymentLineRepository, PaymentLineRepository>();
            services.AddScoped<IPaymentService, PaymentsService>();
            var appLogger = GetLogger(appSettings.Environment, appSettings.APPINSIGHTS_INSTRUMENTATIONKEY);
            services.AddSingleton<IApplicationLogger>(al => (appLogger));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }) //.AddAzureAdBearer
           .AddAzureAdBearer(options => Configuration.Bind("AzureAd", appSettings.AzureAdOptions));
            services.AddAuthorization();
            services.AddSwaggerGen(c=> {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c=> {
                c.SerializeAsV2 = true;
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "View your paymnts API V1");
            });

        }
        /// <summary>
        /// Get instance of Application Insight logger.
        /// </summary>
        /// <returns>Instance of ILogger.</returns>
        /// <param name="environment">current environment.</param>
        /// <param name="appInsightsKey">appInsightsKey for logging.</param>
        private ApplicationInsightsLogger GetLogger(string environment, string appInsightsKey)
        {
            var telemetryClient = new TelemetryClient(new TelemetryConfiguration(appInsightsKey));
            return new ApplicationInsightsLogger(telemetryClient, environment,"ViewYourPayments.Api");
        }
    }
}
