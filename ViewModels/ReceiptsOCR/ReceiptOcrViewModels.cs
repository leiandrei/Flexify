using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GymCrudApp.ViewModels.ReceiptOcr
{
    public class ReceiptUploadViewModel
    {
        [Required(ErrorMessage = "Please select a receipt image or PDF to upload.")]
        public IFormFile? ReceiptFile { get; set; }
    }

    public class ParsedReceiptCandidates
    {
        public decimal? Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? PaymentType { get; set; }
        public string RawText { get; set; } = string.Empty;

        public int ExtractedFieldCount =>
            (Amount.HasValue ? 1 : 0) +
            (PaymentDate.HasValue ? 1 : 0) +
            (!string.IsNullOrWhiteSpace(ReferenceNumber) ? 1 : 0);
    }

    public class MembershipOptionViewModel
    {
        public int MembershipId { get; set; }
        public string DisplayLabel { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
    }

    public class ReceiptConfirmViewModel
    {
        [Required(ErrorMessage = "Uploaded receipt file path is missing.")]
        [StringLength(255)]
        public string ReceiptImagePath { get; set; } = string.Empty;

        public string? OcrRawText { get; set; }

        [Required(ErrorMessage = "Please select a member and membership.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid membership.")]
        public int MembershipId { get; set; }

        [Required(ErrorMessage = "Payment amount is required.")]
        [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than zero.")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [StringLength(50)]
        public string PaymentType { get; set; } = "GCash";

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [Required(ErrorMessage = "Payment date is required.")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        public int ExtractedFieldCount { get; set; }
        public bool OcrFoundNothing { get; set; }

        public List<MembershipOptionViewModel> AvailableMemberships { get; set; } = new();
    }
}