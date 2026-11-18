using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GymCrudApp.ViewModels.Payments
{
    public class PaymentEditViewModel : IValidatableObject
    {
        public int PaymentId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a payment amount.")]
        [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "Amount must be between 0.01 and 999,999.99.")]
        [Display(Name = "Amount Paid")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Please select a payment type.")]
        [RegularExpression("^(Cash|GCash|Maya|Bank Transfer|Card)$",
            ErrorMessage = "Payment type must be Cash, GCash, Maya, Bank Transfer, or Card.")]
        [Display(Name = "Payment Method")]
        public string PaymentType { get; set; } = "Cash";

        [StringLength(100, ErrorMessage = "Reference number cannot exceed 100 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "Reference number can only contain letters, numbers, and hyphens.")]
        [Display(Name = "Reference Number")]
        public string? ReferenceNumber { get; set; }

        [Required(ErrorMessage = "Please provide a payment date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.Equals(PaymentType, "Cash", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(ReferenceNumber))
            {
                yield return new ValidationResult(
                    "Reference number is required for non-cash payments.",
                    new[] { nameof(ReferenceNumber) });
            }
        }
    }
}