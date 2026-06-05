using Serilog;
using Wex.Purchase.Service;
using Wex.Purchase.API.Extensions;
using Wex.Purchase.Service.Exceptions;
using Microsoft.AspNetCore.Mvc;

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

        builder.Services.AddControllers();

        // Configure custom response for invalid model state (validation errors)
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                    .ToList();

                var errorResponse = new Wex.Purchase.API.Models.ErrorResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred.",
                    Errors =  errors
                };

                return new BadRequestObjectResult(errorResponse);
            };
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        //Register services
        builder.Services.AddInfrastructure();
        builder.Services.AddGlobalExceptionHandling();

        //Register Serilog to work on top of Microsoft Logging
        builder.Host.UseSerilog((context, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration)
        );

        var app = builder.Build();

        app.MapGet("/", () => "Wex Purchase API");

        // Register global exception handling middleware so all exceptions (including from controllers/services)
        // are captured and normalized into ErrorResponse objects.
        app.UseGlobalExceptionHandling();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.

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
