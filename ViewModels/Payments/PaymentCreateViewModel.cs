using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymCrudApp.ViewModels.Payments
{
    public class PaymentCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Please select a member.")]
        [Display(Name = "Member")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Please select a membership plan.")]
        [Display(Name = "Membership Plan")]
        public int PlanId { get; set; }

        [Required(ErrorMessage = "Please provide a membership start date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Membership Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

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
        [Display(Name = "Reference Number (Required for Non-Cash)")]
        public string? ReferenceNumber { get; set; }

        [Required(ErrorMessage = "Please provide a payment date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select the recording staff member.")]
        [Display(Name = "Recorded By (Staff)")]
        public int RecordedByStaffId { get; set; }

        public SelectList? Members { get; set; }
        public SelectList? Plans { get; set; }
        public SelectList? StaffList { get; set; }

        /// <summary>Active plans with price/duration for the sell-a-plan summary.</summary>
        public List<PaymentPlanChoiceViewModel> PlanChoices { get; set; } = new();

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

    public class PaymentPlanChoiceViewModel
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public decimal PlanPrice { get; set; }
    }
}
