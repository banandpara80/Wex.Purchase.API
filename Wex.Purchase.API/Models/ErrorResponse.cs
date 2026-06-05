namespace Wex.Purchase.API.Models;

/// <summary>
/// Custom error response model for API error responses.
/// Contains only essential error information without technical details like path or line numbers.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the error title/category.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the error description.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets additional error details specific to validation errors.
    /// </summary>
    public IList<string> Errors { get; set; }
}
