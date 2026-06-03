using Microsoft.AspNetCore.Mvc;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Common.Exceptions;
using Wex.Purchase.Service;
using ILogger = Serilog.ILogger;

namespace Wex.Purchase.API.Controllers;

/// <summary>
/// API controller for managing purchase operations.
/// Provides endpoints for retrieving, adding, and querying purchase transactions.
/// </summary>
[ApiController]
[Route("api/v1/purchase")]
[Consumes("application/json")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(PurchaseValidationException), StatusCodes.Status400BadRequest)]
public class PurchaseController : ControllerBase
{
    private readonly ILogger Logger;
    private readonly IPurchaseService purchaseService;

    /// <summary>
    /// Initializes a new instance of the PurchaseController class.
    /// </summary>
    /// <param name="logger">Logger instance for logging controller operations.</param>
    /// <param name="purchaseService">Service instance for managing purchase operations.</param>
    public PurchaseController(ILogger logger, IPurchaseService purchaseService)
    {
        Logger = logger;
        this.purchaseService = purchaseService;
    }

    /// <summary>
    /// Retrieves purchase transaction based on order id.
    /// </summary>
    /// <param name="id">Purchase Id</param>
    /// <returns>Purchase DTO matching the criteria.</returns>
    [HttpGet(Name = "purchase/{id:guid}")]
    public async Task<ActionResult<PurchaseDTO>> Get(Guid id)
    {
        Logger.Information("Getting purchases");

        PurchaseDTO purchaseDTO = await purchaseService.GetPurchaseOrderById(id);

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
        Logger.Information("Adding purchase");
        if (purchaseDTO == null)
            return BadRequest();

        purchaseDTO = await purchaseService.AddPurchase(purchaseDTO);

        return CreatedAtAction(nameof(AddPurchase), purchaseDTO);
    }
        
    /// <summary>
    /// Retrieves purchase transactions with exchange rate conversions to specified currencies.
    /// Uses Treasury Reporting Rates of Exchange API for current conversion rates.
    /// </summary>
    /// <param name="purchaseRequestDTO">The criteria for filtering purchases, including target currency codes.</param>
    /// <returns>A collection of purchase DTOs with exchange rate conversion information for each currency.</returns>
    /// <remarks>
    /// The Currency field in PurchaseRequestDTO should contain ISO 4217 currency codes (e.g., EUR, GBP, JPY, CAD).
    /// Results include one entry per purchase-currency combination with converted amounts.
    /// </remarks>
    [HttpPost("transactions/with-conversions")]
    [ProducesResponseType(typeof(IEnumerable<PurchaseWithExchangeRateDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseWithExchangeRateDTO>>> GetPurchaseTransactionsWithConversions([FromBody] PurchaseRequestDTO purchaseRequestDTO)
    {
        Logger.Information("Getting purchase transactions with exchange rate conversions");

        var convertedTransactions = await purchaseService.GetPurchaseTransactionsWithConversions(purchaseRequestDTO);
        return new ActionResult<IEnumerable<PurchaseWithExchangeRateDTO>>(convertedTransactions);
    }
}
