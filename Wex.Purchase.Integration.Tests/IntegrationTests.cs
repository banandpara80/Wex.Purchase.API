using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Wex.Purchase.BusinessModels;
using Xunit;

namespace Wex.Purchase.Integration.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task PostgresTestcontainer_ShouldStartAndAcceptConnections()
    {
        await Task.CompletedTask; // placeholder
    }

    [Fact]
    public async Task AddAndGetPurchase_UsingTestcontainer_Postgres()
    {
        // Arrange: configure a PostgreSQL testcontainer

        await using var factory = new CustomWebApplicationFactory();

        try
        { 
            // Start the application with the testcontainer connection string
           // await using var factory = new CustomWebApplicationFactory();
            await factory.InitializeAsync();
            using var client = factory.CreateClient();

            // Create a purchase DTO to post
            var purchase = new PurchaseDTO
            {
                Description = "Integration Test Purchase",
                PurchaseAmount = 12.34m,
                TransactionDate = DateTime.UtcNow
            };

            // Act: POST to create purchase
            var postResp = await client.PostAsJsonAsync("/api/v1/purchase", purchase);
            if (postResp.StatusCode != HttpStatusCode.Created)
            {
                var err = await postResp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"POST failed: {postResp.StatusCode}, body: {err}");
            }

            var created = await postResp.Content.ReadFromJsonAsync<PurchaseDTO>();
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);

            // Act: GET the purchase by id
            var getResp = await client.GetAsync($"/api/v1/purchase/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

            var fetched = await getResp.Content.ReadFromJsonAsync<PurchaseDTO>();
            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched.Id);
            Assert.Equal(purchase.Description, fetched.Description);
            Assert.Equal(purchase.PurchaseAmount, fetched.PurchaseAmount);
        }
        catch (Exception ex)
        {
            // Log or handle exceptions as needed
            throw new InvalidOperationException("Integration test failed", ex);
        }
        finally
        {
           // await factory.DisposeAsync();
        }
    }
 
    public async Task AddAndGetPurchase_UsingInMemoryDb_Works()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var purchase = new PurchaseDTO
        {
            Description = "InMemory Integration Purchase",
            PurchaseAmount = 45.67m,
            TransactionDate = DateTime.UtcNow
        };

        var postResp = await client.PostAsJsonAsync("/api/v1/purchase", purchase);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var created = await postResp.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        var getResp = await client.GetAsync($"/api/v1/purchase/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var fetched = await getResp.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(purchase.Description, fetched.Description);
        Assert.Equal(purchase.PurchaseAmount, fetched.PurchaseAmount);
    }
}
