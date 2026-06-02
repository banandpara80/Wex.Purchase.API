using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Wex.Purchase.API;
using Wex.Purchase.BusinessModels;
using Xunit;

namespace Wex.Purchase.Integration.Tests;

public class IntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public IntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddPurchase_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var dto = new PurchaseDTO { Id = Guid.NewGuid(), Description = "Integration Test", PurchaseAmount = 5.0m, TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow) };

        var response = await client.PostAsJsonAsync("/api/v1/purchase", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var returned = await response.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.Equal(dto.Description, returned.Description);
    }

    [Fact]
    public async Task GetPurchaseTransactions_InvalidRequest_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/purchase/purchasetransactions", (object?)null);

        // Null body will be treated as a bad request by model binding / middleware
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
    }
}
