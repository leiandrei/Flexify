using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymCrudApp.ViewModels.Renewals
{
    public class ExpiringMemberItemViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;

        public int? CurrentMembershipId { get; set; }
        public int? CurrentPlanId { get; set; }
        public string CurrentPlanName { get; set; } = string.Empty;
        public decimal CurrentPlanPrice { get; set; }
        public DateTime CurrentEndDate { get; set; }
        public int DaysRemaining { get; set; }
        public string Status { get; set; } = string.Empty; // Expiring or Expired
    }

    public class OneClickRenewalViewModel
    {
        [Required]
        public int MemberId { get; set; }

        public string MemberName { get; set; } = string.Empty;
        public string? CurrentPlanName { get; set; }
        public DateTime? CurrentEndDate { get; set; }
        public string CurrentStatus { get; set; } = "No Membership";

        [Required(ErrorMessage = "Please select a membership plan.")]
        [Display(Name = "Membership Plan")]
        public int PlanId { get; set; }

        [Required]
        [Range(0.01, 1000000, ErrorMessage = "Payment amount must be greater than zero.")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public string PaymentType { get; set; } = "Cash"; // Cash, GCash, Maya, Bank Transfer, Card

        [Display(Name = "Reference Number")]
        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        public DateTime ProjectedStartDate { get; set; }
        public DateTime ProjectedEndDate { get; set; }

        public IEnumerable<SelectListItem> AvailablePlans { get; set; } = new List<SelectListItem>();
    }

    public class RenewalResult
    {
        public bool Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
        public string? MemberName { get; set; }
        public int? NewMembershipId { get; set; }
        public int? NewPaymentId { get; set; }
        public DateTime? NewStartDate { get; set; }
        public DateTime? NewEndDate { get; set; }
    }
}