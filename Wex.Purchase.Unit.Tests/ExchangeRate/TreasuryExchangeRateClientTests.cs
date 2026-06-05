using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Wex.Purchase.Manager.ExchangeRate;
using Xunit;
using Serilog.Core;

namespace Wex.Purchase.Unit.Tests.ExchangeRate;

public class TreasuryExchangeRateClientTests
{
    [Fact]
    public async Task GetExchangeRateAsync_ReturnsRate_AndCachesResult()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var responseObj = new
        {
            Data = new[]
            {
                new { exchange_rate = 1.23m, CountryCurrencyDesc = "Cubo-Peso", EffectiveDate = date.ToString("yyyy-MM-dd") }
            }
        }; 

        string json = JsonSerializer.Serialize(responseObj);

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        int callCount = 0;
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                Interlocked.Increment(ref callCount);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.fiscaldata.treasury.gov/") };
        var client = new TreasuryExchangeRateClient(httpClient, Logger.None);

        // Act
        var rate1 = await client.GetExchangeRateAsync("Cubo-Peso", date);
        var rate2 = await client.GetExchangeRateAsync("Cubo-Peso", date);

        // Assert
        Assert.True(rate1.HasValue);
        Assert.Equal(1.23m, rate1.Value);
        Assert.Equal(rate1, rate2);

        // Verify underlying HTTP was only called once due to caching
        handlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetExchangeRateAsync_NoData_ReturnsNull()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var responseObj = new { Data = Array.Empty<object>() };
        string json = JsonSerializer.Serialize(responseObj);

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new TreasuryExchangeRateClient(httpClient, Logger.None);

        var currency = "NODATA_" + Guid.NewGuid().ToString("N").Substring(0, 8);

        // Act
        var rate = await client.GetExchangeRateAsync(currency, date);

        // Assert
        Assert.False(rate.HasValue);
    }

    [Fact]
    public async Task GetExchangeRateAsync_HttpError_ThrowsInvalidOperationException()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new TreasuryExchangeRateClient(httpClient, Logger.None);

        var currency = "ERR_" + Guid.NewGuid().ToString("N").Substring(0, 8);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetExchangeRateAsync(currency, date));
    }
}
