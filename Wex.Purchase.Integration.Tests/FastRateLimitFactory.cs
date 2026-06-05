using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wex.Purchase.Common.RateLimiter;
using Wex.Purchase.Repository;
using Xunit;

namespace Wex.Purchase.Integration.Tests
{
    /// <summary>
    /// Custom WebApplicationFactory with overridable rate limiting configuration for faster test execution.
    /// Allows tests to specify custom rate limit windows and request limits.
    /// </summary>
    public class FastRateLimitFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private PostgreSqlContainer? _postgreSqlContainer;
        private readonly int _maxRequestsPerWindow;
        private readonly int _windowSizeInSeconds;

        public FastRateLimitFactory(int maxRequests = 10, int windowSeconds = 5)
        {
            _maxRequestsPerWindow = maxRequests;
            _windowSizeInSeconds = windowSeconds;
        }

        public async Task InitializeAsync()
        {
            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithDatabase("purchasedb")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgreSqlContainer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (_postgreSqlContainer != null)
            {
                await _postgreSqlContainer.StopAsync();
                await _postgreSqlContainer.DisposeAsync();
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<PurchaseDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext with PostgreSQL testcontainer connection string
                services.AddDbContext<PurchaseDbContext>(options =>
                {
                    if (_postgreSqlContainer != null)
                    {
                        options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
                    }
                });

                // Override rate limiter with test configuration
                var rateLimiterDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IRateLimiter));

                if (rateLimiterDescriptor != null)
                {
                    services.Remove(rateLimiterDescriptor);
                }

                services.AddScoped<IRateLimiter>(sp =>
                {
                    var logger = sp.GetRequiredService<Serilog.ILogger>();
                    return new RateLimiter(logger, maxRequestsPerWindow: _maxRequestsPerWindow, windowSizeInSeconds: _windowSizeInSeconds);
                });

                // Build service provider and migrate database
                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseDbContext>();
                    dbContext.Database.EnsureCreated();
                }
            });
        }
    }
}
