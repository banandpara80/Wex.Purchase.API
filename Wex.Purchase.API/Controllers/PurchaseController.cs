using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Service;

namespace Wex.Purchase.API.Controllers;

/// <summary>
/// API controller for managing purchase operations.
/// Provides endpoints for retrieving, adding, and querying purchase transactions.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class PurchaseController : ControllerBase
{
    private readonly ILogger<PurchaseController> logger;
    private readonly IPurchaseService purchaseService;

    /// <summary>
    /// Initializes a new instance of the PurchaseController class.
    /// </summary>
    /// <param name="logger">Logger instance for logging controller operations.</param>
    /// <param name="purchaseService">Service instance for managing purchase operations.</param>
    public PurchaseController(ILogger<PurchaseController> logger, IPurchaseService purchaseService)
    {
        this.logger = logger;
        this.purchaseService = purchaseService;
    }

    /// <summary>
    /// Retrieves a list of purchase transactions.
    /// </summary>
    /// <returns>A collection of purchase DTOs.</returns>
    [HttpGet(Name = "purchase")]
    public Task<ActionResult<IEnumerable<PurchaseDTO>>> Get()
    {
        this.logger.LogInformation("Getting purchases");

        var list = Enumerable.Range(1, 1).Select(index => new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            PurchaseAmount = 1.0M,
            Description = "Test Purchase",
            TransactionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(index))
        });

        return Task.FromResult(new ActionResult<IEnumerable<PurchaseDTO>>(list));
    }

    /// <summary>
    /// Adds a new purchase transaction.
    /// </summary>
    /// <param name="purchaseDTO">The purchase data to add.</param>
    /// <returns>The added purchase with generated ID and metadata.</returns>
    [HttpPost(Name = "purchase")]
    public async Task<ActionResult<PurchaseDTO>> AddPurchase([FromBody] PurchaseDTO purchaseDTO)
    {
        this.logger.LogInformation("Adding purchase");

        purchaseDTO = new PurchaseDTO
        {
            Id = Guid.NewGuid(),
            PurchaseAmount = 1.0M,
            Description = "Test Purchase",
            TransactionDate = DateOnly.FromDateTime(DateTime.Now)
        };

        purchaseDTO = await purchaseService.AddPurchase(purchaseDTO);

        return Ok(purchaseDTO);
    }
        

    /// <summary>
    /// Retrieves purchase transactions based on specified criteria.
    /// </summary>
    /// <param name="purchaseRequestDTO">The criteria for filtering purchases.</param>
    /// <returns>A collection of purchase DTOs matching the criteria.</returns>
    [HttpPost("purchasetransactions")]
    public async Task<ActionResult<IEnumerable<PurchaseDTO>>> GetPurchaseTransactionss([FromBody] PurchaseRequestDTO purchaseRequestDTO)
    {
        var purchaseTransactions = await purchaseService.GetPurchaseTransactions(purchaseRequestDTO);
        return new ActionResult<IEnumerable<PurchaseDTO>>(purchaseTransactions);
    }
}
