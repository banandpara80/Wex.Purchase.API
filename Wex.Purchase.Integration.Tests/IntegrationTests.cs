using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Common.RateLimiter;
using Xunit;

namespace Wex.Purchase.Integration.Tests;

public class IntegrationTests
{
    // Default API key used for testing
    private const string DefaultApiKey = "secret";

    /// <summary>
    /// Helper method to add authentication header to HTTP client.
    /// </summary>
    private static void AddAuthenticationHeader(HttpClient client, string apiKey = DefaultApiKey)
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

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
            AddAuthenticationHeader(client);

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

    /// <summary>
    /// Integration test for the GET /transactions/with-conversions endpoint.
    /// Creates purchase records and verifies they can be retrieved with exchange rate conversions.
    /// </summary>
    [Fact]
    public async Task GetPurchaseTransactionsWithConversions_ReturnsConvertedTransactions()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        AddAuthenticationHeader(client);

        // Create multiple purchases
        var purchase1 = new PurchaseDTO
        {
            Description = "Conversion Test Purchase 1",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.UtcNow.AddDays(-1)
        };

        var purchase2 = new PurchaseDTO
        {
            Description = "Conversion Test Purchase 2",
            PurchaseAmount = 250.50m,
            TransactionDate = DateTime.UtcNow
        };

        // Act: Create purchases
        var postResp1 = await client.PostAsJsonAsync("/api/v1/purchase", purchase1);
        Assert.Equal(HttpStatusCode.Created, postResp1.StatusCode);

        var created1 = await postResp1.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.NotNull(created1);

        var postResp2 = await client.PostAsJsonAsync("/api/v1/purchase", purchase2);
        Assert.Equal(HttpStatusCode.Created, postResp2.StatusCode);

        var created2 = await postResp2.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.NotNull(created2);

        // Act: Request conversions for the created purchases
        var conversionRequest = new PurchaseRequestDTO
        {
            Ids = $"{created1.Id},{created2.Id}",
            Currency = "Cuba-Peso"
        };

        var convResp = await client.PostAsJsonAsync("/api/v1/purchase/transactions/with-conversions", conversionRequest);
        Assert.Equal(HttpStatusCode.OK, convResp.StatusCode);

        var convertedTransactions = await convResp.Content.ReadFromJsonAsync<IEnumerable<PurchaseWithExchangeRateDTO>>();

        // Assert
        Assert.NotNull(convertedTransactions);
        Assert.NotEmpty(convertedTransactions);

        var transactionList = convertedTransactions.ToList();
        Assert.True(transactionList.Count >= 2, "Should contain at least 2 converted transactions");

        // Verify each transaction has required fields
        foreach (var transaction in transactionList)
        {
            Assert.NotNull(transaction.Purchase);
            Assert.NotEqual(Guid.Empty, transaction.Purchase.Id);
            Assert.NotNull(transaction.Purchase.Description);
            Assert.True(transaction.Purchase.PurchaseAmount > 0);
            Assert.True(transaction.ExchangeRate > 0);
            Assert.True(transaction.ConvertedAmount > 0);
        }

        // Verify the original purchases are in the results
        var ids = transactionList.Select(t => t.Purchase.Id).ToList();
        Assert.Contains(created1.Id, ids);
        Assert.Contains(created2.Id, ids);
    }

    /// <summary>
    /// Integration test for the GET /transactions/with-conversions endpoint with empty request.
    /// </summary>
    [Fact]
    public async Task GetPurchaseTransactionsWithConversions_WithEmptyIds_ReturnsEmptyOrBadRequest()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        AddAuthenticationHeader(client);

        var conversionRequest = new PurchaseRequestDTO
        {
            Ids = string.Empty,
            Currency = "India-Rupee"
        };

        // Act
        var convResp = await client.PostAsJsonAsync("/api/v1/purchase/transactions/with-conversions", conversionRequest);

        // Assert - Should either return OK with empty results or BadRequest
        Assert.True(
            convResp.StatusCode == HttpStatusCode.OK || convResp.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK (200) or BadRequest (400), but got {convResp.StatusCode}"
        );

        if (convResp.StatusCode == HttpStatusCode.OK)
        {
            var convertedTransactions = await convResp.Content.ReadFromJsonAsync<IEnumerable<PurchaseWithExchangeRateDTO>>();
            Assert.NotNull(convertedTransactions);
            Assert.Empty(convertedTransactions);
        }
    }

    /// <summary>
    /// Integration test for rate limiting on the POST /purchase endpoint.
    /// Verifies that requests are allowed up to the configured limit.
    /// Uses a fast-execution test configuration (10 requests max per 5 seconds) for quick test runs.
    /// </summary>
    [Fact]
    public async Task AddPurchase_RateLimiting_AllowsRequestsWithinLimit()
    {
        // Arrange
        // Create factory with overridden rate limiter configuration for faster testing
        await using var factory = new FastRateLimitFactory(maxRequests: 10, windowSeconds: 5);

        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        AddAuthenticationHeader(client);

        var purchase = new PurchaseDTO
        {
            Description = "Rate Limit Test Purchase",
            PurchaseAmount = 99.99m,
            TransactionDate = DateTime.UtcNow
        };

        // Act: Send requests within the rate limit window
        int requestsToMake = 8; // Within the 10 requests per 5 seconds limit
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < requestsToMake; i++)
        {
            purchase.Description = $"Rate Limit Test Purchase {i}";
            tasks.Add(client.PostAsJsonAsync("/api/v1/purchase", purchase));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert: All requests within limit should succeed with 201 Created
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(requestsToMake, successCount);

        // Verify we got valid purchase IDs back
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdPurchase = await response.Content.ReadFromJsonAsync<PurchaseDTO>();
            Assert.NotNull(createdPurchase);
            Assert.NotEqual(Guid.Empty, createdPurchase.Id);
        }
    }

    /// <summary>
    /// Integration test for rate limiting on the GET endpoint.
    /// Verifies that rate limiting is applied consistently across different endpoint types.
    /// </summary>
    [Fact]
    public async Task GetPurchase_RateLimiting_AllowsMultipleRequests()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        AddAuthenticationHeader(client);

        var purchase = new PurchaseDTO
        {
            Description = "Get Rate Limit Test",
            PurchaseAmount = 150.00m,
            TransactionDate = DateTime.UtcNow
        };

        // Create a purchase first
        var postResp = await client.PostAsJsonAsync("/api/v1/purchase", purchase);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var createdPurchase = await postResp.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.NotNull(createdPurchase);

        // Act: Make multiple GET requests for the same purchase
        int requestsToMake = 5;
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < requestsToMake; i++)
        {
            tasks.Add(client.GetAsync($"/api/v1/purchase/{createdPurchase.Id}"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert: All requests should succeed with 200 OK
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(requestsToMake, successCount);

        // Verify response content
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fetched = await response.Content.ReadFromJsonAsync<PurchaseDTO>();
            Assert.NotNull(fetched);
            Assert.Equal(createdPurchase.Id, fetched.Id);
        }
    }

    /// <summary>
    /// Integration test for the /transactions/with-conversions endpoint.
    /// Verifies that rate limiting is applied to all endpoints in the controller.
    /// </summary>
    [Fact]
    public async Task GetPurchaseTransactionsWithConversions_RateLimiting_AllowsMultipleRequests()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        AddAuthenticationHeader(client);

        // Create test purchase
        var purchase = new PurchaseDTO
        {
            Description = "Conversion Rate Limit Test",
            PurchaseAmount = 200.00m,
            TransactionDate = DateTime.UtcNow
        };

        var postResp = await client.PostAsJsonAsync("/api/v1/purchase", purchase);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var createdPurchase = await postResp.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.NotNull(createdPurchase);

        // Act: Make multiple requests to the /transactions/with-conversions endpoint
        int requestsToMake = 5;
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < requestsToMake; i++)
        {
            var conversionRequest = new PurchaseRequestDTO
            {
                Ids = createdPurchase.Id.ToString(),
                Currency = "USA-Dollar"
            };

            tasks.Add(client.PostAsJsonAsync("/api/v1/purchase/transactions/with-conversions", conversionRequest));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert: All requests should succeed with 200 OK
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(requestsToMake, successCount);

        // Verify response content for each request
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var convertedTransactions = await response.Content.ReadFromJsonAsync<IEnumerable<PurchaseWithExchangeRateDTO>>();
            Assert.NotNull(convertedTransactions);
            Assert.NotEmpty(convertedTransactions);
        }
    }

    /// <summary>
    /// Negative test case for rate limiting.
    /// Attempts to exceed the rate limit and verifies that the rate limiter correctly 
    /// blocks/delays subsequent requests to stay within limits.
    /// Uses fast-execution test configuration (5 requests max per 3 seconds) for quick test runs.
    /// </summary>
    [Fact]
    public async Task RateLimiting_ExceedsLimit_RequestsAreDelayed()
    {
        // Arrange
        // Create factory with fast test configuration
        await using var factory = new FastRateLimitFactory(maxRequests: 5, windowSeconds: 3);

        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        AddAuthenticationHeader(client);

        var purchase = new PurchaseDTO
        {
            Description = "Exceed Rate Limit Test",
            PurchaseAmount = 500.00m,
            TransactionDate = DateTime.UtcNow
        };

        // Act: Send concurrent requests exceeding the rate limit
        // With 5 requests per 3 seconds, we'll send 10 concurrent to exceed the limit
        int requestsToMake = 10;
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < requestsToMake; i++)
        {
            purchase.Description = $"Exceed Rate Limit Test {i}";
            tasks.Add(client.PostAsJsonAsync("/api/v1/purchase", purchase));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert: All requests should eventually succeed despite hitting rate limit
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(requestsToMake, successCount);

        // With 10 requests and a 5 per 3 seconds limit, the rate limiter should delay the excess 5 requests
        // Expected minimum delay: approximately 3 seconds for the second batch
        Assert.True(stopwatch.ElapsedMilliseconds >= 3000,
            $"Expected rate limiting delays (>3 seconds), but operation completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Negative test case: Rate limiting with mixed endpoints.
    /// Verifies that the rate limit applies to ALL endpoints in the controller uniformly.
    /// Uses fast-execution test configuration for quick test runs.
    /// </summary>
    [Fact]
    public async Task RateLimiting_MixedEndpoints_SharesSameLimitPool()
    {
        // Arrange
        await using var factory = new FastRateLimitFactory(maxRequests: 6, windowSeconds: 3);


        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        AddAuthenticationHeader(client);

        var purchase = new PurchaseDTO
        {
            Description = "Mixed Endpoints Rate Limit Test",
            PurchaseAmount = 350.00m,
            TransactionDate = DateTime.UtcNow
        };

        // Create test purchase
        var createResp = await client.PostAsJsonAsync("/api/v1/purchase", purchase);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var createdPurchase = await createResp.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.NotNull(createdPurchase);

        // Act: Mix different endpoint types in rapid succession
        // Send 12 requests (exceeds 6 per 3 second limit)
        int totalRequests = 12;
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Mix POST, GET, and conversion requests
        for (int i = 0; i < totalRequests; i++)
        {
            if (i % 3 == 0)
            {
                // POST request
                purchase.Description = $"Mixed Test POST {i}";
                tasks.Add(client.PostAsJsonAsync("/api/v1/purchase", purchase));
            }
            else if (i % 3 == 1)
            {
                // GET request
                tasks.Add(client.GetAsync($"/api/v1/purchase/{createdPurchase.Id}"));
            }
            else
            {
                // Conversion request
                var conversionRequest = new PurchaseRequestDTO
                {
                    Ids = createdPurchase.Id.ToString(),
                    Currency = "EUR"
                };
                tasks.Add(client.PostAsJsonAsync("/api/v1/purchase/transactions/with-conversions", conversionRequest));
            }
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert: All requests should eventually succeed
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created || 
                                              r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(totalRequests, successCount);

        // Mixed endpoint requests should experience rate limiting delays
        // With 12 requests and 6 per 3 seconds limit, expect at least one delay cycle
        Assert.True(stopwatch.ElapsedMilliseconds >= 3000,
            $"Expected rate limiting delays across mixed endpoints, but operation completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Negative test case: Multiple concurrent clients sharing the same rate limit.
    /// Verifies that rate limiting is applied globally across different HTTP client instances.
    /// Uses fast-execution test configuration for quick test runs.
    /// </summary>
    [Fact]
    public async Task RateLimiting_MultipleClients_SharesSameLimits()
    {
        // Arrange
        await using var factory = new FastRateLimitFactory(maxRequests: 8, windowSeconds: 3);

        await factory.InitializeAsync();

        // Create two separate client instances
        using var client1 = factory.CreateClient();
        using var client2 = factory.CreateClient();

        AddAuthenticationHeader(client1);
        AddAuthenticationHeader(client2);

        var purchase = new PurchaseDTO
        {
            Description = "Multi-Client Rate Limit Test",
            PurchaseAmount = 250.00m,
            TransactionDate = DateTime.UtcNow
        };

        // Act: Send requests from both clients concurrently
        int requestsPerClient = 6; // Total 12 requests (exceeds 8 limit)
        var stopwatch = Stopwatch.StartNew();
        var client1Tasks = new List<Task<HttpResponseMessage>>();
        var client2Tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < requestsPerClient; i++)
        {
            purchase.Description = $"Client1 Request {i}";
            client1Tasks.Add(client1.PostAsJsonAsync("/api/v1/purchase", purchase));

            purchase.Description = $"Client2 Request {i}";
            client2Tasks.Add(client2.PostAsJsonAsync("/api/v1/purchase", purchase));
        }

        var allTasks = client1Tasks.Concat(client2Tasks).ToList();
        var responses = await Task.WhenAll(allTasks);
        stopwatch.Stop();

        // Assert: All requests should eventually succeed
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(requestsPerClient * 2, successCount);

        // The combined requests should trigger rate limiting
        Assert.True(stopwatch.ElapsedMilliseconds >= 3000,
            $"Expected rate limiting delays due to shared limits, but operation completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Tests that requests without Authorization header are rejected with 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task ApiKeyAuthentication_MissingAuthorizationHeader_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        var purchase = new PurchaseDTO
        {
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.UtcNow
        };

        // Act: POST without Authorization header
        var response = await client.PostAsJsonAsync("/api/v1/purchase", purchase);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests that requests with invalid Authorization header format are rejected with 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task ApiKeyAuthentication_InvalidHeaderFormat_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        var purchase = new PurchaseDTO
        {
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.UtcNow
        };

        // Act: POST with invalid header format (missing Bearer prefix)
        client.DefaultRequestHeaders.Add("Authorization", "InvalidFormat");
        var response = await client.PostAsJsonAsync("/api/v1/purchase", purchase);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests that requests with incorrect API key are rejected with 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task ApiKeyAuthentication_IncorrectApiKey_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        var purchase = new PurchaseDTO
        {
            Description = "Test Purchase",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.UtcNow
        };

        // Act: POST with incorrect API key
        client.DefaultRequestHeaders.Add("Authorization", "Bearer incorrect-api-key");
        var response = await client.PostAsJsonAsync("/api/v1/purchase", purchase);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests that requests with correct API key are authorized and processed successfully.
    /// </summary>
    [Fact]
    public async Task ApiKeyAuthentication_CorrectApiKey_ReturnsSuccess()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        var purchase = new PurchaseDTO
        {
            Description = "Test Purchase with Auth",
            PurchaseAmount = 100.00m,
            TransactionDate = DateTime.UtcNow
        };

        // Act: POST with correct API key
        client.DefaultRequestHeaders.Add("Authorization", "Bearer secret");
        var response = await client.PostAsJsonAsync("/api/v1/purchase", purchase);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify we got back a valid purchase DTO with ID
        var responseContent = await response.Content.ReadFromJsonAsync<PurchaseDTO>();
        Assert.NotNull(responseContent);
        Assert.NotEqual(Guid.Empty, responseContent.Id);
        Assert.Equal(purchase.Description, responseContent.Description);
    }

    /// <summary>
    /// Tests that health check endpoints are excluded from authentication.
    /// </summary>
    [Fact]
    public async Task ApiKeyAuthentication_HealthCheckEndpoint_BypassesAuthentication()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        // Act: GET health check without Authorization header
        var response = await client.GetAsync("/health");

        // Assert: Should succeed without authentication
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
