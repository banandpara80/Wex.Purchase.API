using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Wex.Purchase.Manager;
using Wex.Purchase.Manager.ExchangeRate;
using Wex.Purchase.Manager.ExchangeRateConversion;
using Wex.Purchase.Repository;
using Wex.Purchase.Service;
using Wex.Purchase.Repository.Exceptions;
using Wex.Purchase.Common.CircuitBreaker;
using Wex.Purchase.Service.Exceptions;
using Wex.Purchase.Manager.Exceptions;
using Wex.Purchase.Common.RateLimiter;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks(HealthEndpointPath);

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }

    public static WebApplication CreateDB(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PurchaseDbContext>();
            context.Database.EnsureCreated();
        }

        return app;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register EF DbContext (Postgres). Connection string is read from configuration by the caller.
        services.AddDbContext<PurchaseDbContext>(static (provider, options) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var conn = configuration.GetConnectionString("PurchaseDb");
            options.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure());
        });

        //Register concrete classes
        services.AddScoped<PurchaseService>();
        services.AddScoped<PurchaseManager>();
        services.AddScoped<PurchaseRepository>();
        services.AddSingleton<NoopCircuitBreaker>();

        // Register RateLimiter with configuration from appsettings.json
        services.AddSingleton<IRateLimiter>(provider =>
        {
            var logger = provider.GetRequiredService<Serilog.ILogger>();
            var configuration = provider.GetRequiredService<IConfiguration>();

            // Read rate limiting configuration with defaults
            var maxRequestsPerWindow = configuration.GetValue<int>("RateLimiting:MaxRequestsPerWindow", 60);
            var windowSizeInSeconds = configuration.GetValue<int>("RateLimiting:WindowSizeInSeconds", 60);

            return new RateLimiter(logger, maxRequestsPerWindow, windowSizeInSeconds);
        });

        // Register Treasury Exchange Rate API client and conversion service
        services.AddHttpClient<ITreasuryExchangeRateClient, TreasuryExchangeRateClient>();
        services.AddScoped<IExchangeRateConversionService, ExchangeRateConversionService>();

        services.AddSingleton<ICircuitBreaker>(sp =>
            new PollyCircuitBreaker(
                exceptionsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromSeconds(30)
        ));

        // Service level
        services.AddScoped<IServiceExceptionHandler, ServiceExceptionHandler>();
        services.AddScoped<IPurchaseService>(sp =>
        {
            var real = sp.GetRequiredService<PurchaseService>();
            var handler = sp.GetRequiredService<IServiceExceptionHandler>();
            return new PurchaseServiceDecorator(real, handler);
        });

        // Repository level
        services.AddScoped<RepositoryExceptionHandler>();
        services.AddScoped<IPurchaseRepository>(sp =>
        {
            var real = sp.GetRequiredService<PurchaseRepository>();
            var handler = sp.GetRequiredService<RepositoryExceptionHandler>();
            return new PurchaseRepositoryDecorator(real, handler);
        });

        // Manager level
        services.AddScoped<IManagerExceptionHandler, ManagerExceptionHandler>();
        services.AddScoped<IPurchaseManager>(sp =>
        {
            var real = sp.GetRequiredService<PurchaseManager>();
            var handler = sp.GetRequiredService<IManagerExceptionHandler>();

            return new PurchaseManagerDecorator(real, handler);       
        });
         
        return services;
    }
}
