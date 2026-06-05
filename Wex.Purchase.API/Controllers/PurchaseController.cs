using Microsoft.AspNetCore.Mvc;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.API.Models;
using Wex.Purchase.Service;
using ILogger = Serilog.ILogger;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Wex.Purchase.API.Controllers;

/// <summary>
/// API controller for managing purchase operations.
/// Provides endpoints for retrieving, adding, and querying purchase transactions.
/// </summary>
[ApiController]
[Route("api/v1/purchase")]
[Consumes("application/json")]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
public class PurchaseController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IPurchaseService _purchaseService;

    // Controller-level rate limiting (sliding window)
    private static readonly object _rateLimitLock = new();
    private static readonly Queue<DateTime> _requestTimestamps = new();
    private const int _maxRequestsPerWindow = 60; // requests per minute
    private static readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initializes a new instance of the PurchaseController class.
    /// </summary>
    /// <param name="logger">Logger instance for logging controller operations.</param>
    /// <param name="purchaseService">Service instance for managing purchase operations.</param>
    public PurchaseController(ILogger logger, IPurchaseService purchaseService)
    {
        _logger = logger;
        this._purchaseService = purchaseService;
    }

    private async Task EnsureRateLimitAsync(CancellationToken ct)
    {
        DateTime now = DateTime.UtcNow;
        while (true)
        {
            TimeSpan wait = TimeSpan.Zero;
            lock (_rateLimitLock)
            {
                while (_requestTimestamps.Count > 0 && (now - _requestTimestamps.Peek()) >= _rateLimitWindow)
                {
                    _requestTimestamps.Dequeue();
                }

                if (_requestTimestamps.Count < _maxRequestsPerWindow)
                {
                    _requestTimestamps.Enqueue(now);
                    return;
                }

                var oldest = _requestTimestamps.Peek();
                wait = _rateLimitWindow - (now - oldest);
                if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
            }

            if (wait > TimeSpan.Zero)
            {
                _logger.Information("Rate limit reached. Waiting {Delay} before retrying.", wait);
                try
                {
                    await Task.Delay(wait, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }

            now = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Retrieves purchase transaction based on order id.
    /// </summary>
    /// <param name="id">Purchase Id</param>
    /// <returns>Purchase DTO matching the criteria.</returns>
    [HttpGet(Name = "purchase/{id:guid}")]
    public async Task<ActionResult<PurchaseDTO>> Get(Guid id)
    {
        _logger.Information("Getting purchases");

        await EnsureRateLimitAsync(HttpContext?.RequestAborted ?? CancellationToken.None);

        PurchaseDTO purchaseDTO = await _purchaseService.GetPurchaseOrderById(id);

        return Ok(purchaseDTO);
    }

    /// <summary>
    /// Adds a new purchase transaction.
    /// </summary>
    /// <param name="purchaseDTO">The purchase data to add.</param>
    /// <returns>The added purchase with generated ID and metadata.</returns>
    [HttpPost(Name = "purchase")]
    [ProducesResponseType(typeof(PurchaseDTO), StatusCodes.Status201Created)]
    public async Task<ActionResult<PurchaseDTO>> AddPurchase([FromBody] PurchaseDTO purchaseDTO)
    {
        _logger.Information("Adding purchase");
        await EnsureRateLimitAsync(HttpContext?.RequestAborted ?? CancellationToken.None);
        if (purchaseDTO == null)
            return BadRequest();

        purchaseDTO = await _purchaseService.AddPurchase(purchaseDTO);

        return CreatedAtAction(nameof(AddPurchase), purchaseDTO);
    }
        
    /// <summary>
    /// Retrieves purchase transactions with exchange rate conversions to specified currencies.
    /// Uses Treasury Reporting Rates of Exchange API for current conversion rates.
    /// </summary>
    /// <param name="purchaseRequestDTO">The criteria for filtering purchases, including target currency codes.</param>
    /// <returns>A collection of purchase DTOs with exchange rate conversion information for each currency.</returns>
    /// <remarks>
    /// The Currency field in PurchaseRequestDTO
    /// Results include one entry per purchase-currency combination with converted amounts.
    /// </remarks>
    [HttpPost("transactions/with-conversions")]
    [ProducesResponseType(typeof(IEnumerable<PurchaseWithExchangeRateDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseWithExchangeRateDTO>>> GetPurchaseTransactionsWithConversions([FromBody] PurchaseRequestDTO purchaseRequestDTO)
    {
        _logger.Information("Getting purchase transactions with exchange rate conversions");

        var convertedTransactions = await _purchaseService.GetPurchaseTransactionsWithConversions(purchaseRequestDTO);
        return new ActionResult<IEnumerable<PurchaseWithExchangeRateDTO>>(convertedTransactions);
    }
}
