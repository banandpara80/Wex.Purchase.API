using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wex.Purchase.Repository;
using Xunit;

namespace Wex.Purchase.Integration.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private PostgreSqlContainer? _postgreSqlContainer;

       
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
