using Microsoft.AspNetCore.Mvc;
using Wex.Purchase.BusinessModels;
using Wex.Purchase.Service;
using ILogger = Serilog.ILogger;
// Using global JSON options configured in Program.cs
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
    public async Task<ActionResult<PurchaseDTO>> AddPurchase([FromBody] PurchaseDTO purchaseDTO)
    {
        Logger.Information("Adding purchase");
        if (purchaseDTO == null)
            return BadRequest();

        if (purchaseDTO.Id == Guid.Empty)
            purchaseDTO.Id = Guid.NewGuid();

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
