using Serilog;
using Wex.Purchase.Service;
using Wex.Purchase.API.Extensions;
using Wex.Purchase.Service.Exceptions;

/// <summary>
/// Application entry point for the Wex Purchase API.
/// Configures dependency injection, middleware, and logging infrastructure.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// Configures the web application, registers services, and starts the server.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        // Add services to the container with JSON options for DateOnly
        builder.Services.AddControllers()
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(new Wex.Purchase.API.Json.DateOnlyJsonConverter());
            })
            .AddNewtonsoftJson(opts =>
            {
                // Register a simple converter for DateOnly when using Newtonsoft
                opts.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter());
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        //Register services
        builder.Services.AddInfrastructure();

        // Register service-level exception handler for DI
        builder.Services.AddScoped<IServiceExceptionHandler, ServiceExceptionHandler>();

        // Ensure concrete PurchaseService is registered
        builder.Services.AddScoped<PurchaseService>();

        // Register IPurchaseService via decorator that wraps calls with the exception handler
        builder.Services.AddScoped<IPurchaseService>(sp =>
        {
            var real = sp.GetRequiredService<PurchaseService>();
            var handler = sp.GetRequiredService<IServiceExceptionHandler>();
            return new PurchaseServiceDecorator(real, handler);
        });

        // Register exception handling services
        builder.Services.AddExceptionHandling();

        //Register Serilog to work on top of Microsoft Logging
        builder.Host.UseSerilog((context, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration)
        );

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        // Register global exception handling middleware
        app.UseGlobalExceptionHandling();

        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            // Notice the missing leading slash or use of relative notation
            c.SwaggerEndpoint("v1/swagger.json", "My API V1");
        });

        app.UseHttpsRedirection();

        app.UseAuthorization();

        // Apply EF Core migrations at startup to ensure database schema is created
        app.CreateDB();

        app.MapControllers();

        await app.RunAsync();
    }
}
