using System.ComponentModel.DataAnnotations;

namespace Wex.Purchase.BusinessModels.Validators;

/// <summary>
/// Custom validation attribute that ensures a DateTime value is not in the future.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class NotFutureDateAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFutureDateAttribute"/> class.
    /// </summary>
    public NotFutureDateAttribute() : base("The {0} cannot be in the future.")
    {
    }

    /// <summary>
    /// Validates that the provided value is not a future date.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>ValidationResult.Success if valid; ValidationResult with error message if invalid.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is not DateTime dateTime)
        {
            return new ValidationResult("The value must be a DateTime.");
        }

        // Compare only dates (ignore time component)
        if (dateTime.ToUniversalTime().Date > DateTime.UtcNow.Date)
        {
            var errorMessage = FormatErrorMessage(validationContext.DisplayName);
            return new ValidationResult(errorMessage, new[] { validationContext.MemberName ?? nameof(value) });
        }

        return ValidationResult.Success;
    }
}
