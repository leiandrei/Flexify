using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrudApp.Models
{
    public class Payments : IValidatableObject
    {
        // primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int paymentId { get; set; }

        // foreign key linking to Members table
        [Required(ErrorMessage = "This field is required. Please select a member.")]
        public int memberId { get; set; }

        [ForeignKey("memberId")]
        public Members? Member { get; set; }

        // foreign key linking to Memberships table
        [Required(ErrorMessage = "This field is required. Please select a membership.")]
        public int membershipId { get; set; }

        [ForeignKey("membershipId")]
        public Memberships? Membership { get; set; }

        // payment amount
        [Required(ErrorMessage = "This field is required. Please provide a payment amount.")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999,999.99.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal amount { get; set; }

        // payment method
        [Required(ErrorMessage = "This field is required. Please select a payment type.")]
        [StringLength(30, ErrorMessage = "Payment type cannot exceed 30 characters.")]
        [RegularExpression("^(Cash|GCash|Maya|Bank Transfer|Card)$",
            ErrorMessage = "Payment type must be Cash, GCash, Maya, Bank Transfer, or Card.")]
        public string paymentType { get; set; } = string.Empty;

        // nullable: only required for non-cash transactions
        [StringLength(100, ErrorMessage = "Reference number cannot exceed 100 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "Reference number can only contain letters, numbers, and hyphens.")]
        public string? referenceNumber { get; set; }

        // nullable: optional for cash payments or when no receipt is uploaded
        [StringLength(255, ErrorMessage = "Receipt image path cannot exceed 255 characters.")]
        public string? receiptImagePath { get; set; }

        // nullable: populated automatically when OCR scans an uploaded receipt
        public string? ocrRawText { get; set; }

        public bool isVoided { get; set; } = false;
        
        [Required(ErrorMessage = "This field is required. Please provide a payment date.")]
        [DataType(DataType.DateTime)]
        public DateTime paymentDate { get; set; } = DateTime.Now;

        // foreign key linking to Staff table
        [Required(ErrorMessage = "This field is required. Please specify the recording staff member.")]
        public int recordedByStaffId { get; set; }

        [ForeignKey("recordedByStaffId")]
        public Staff? RecordedByStaff { get; set; }

        // conditional validation: require referenceNumber for digital/non-cash payments
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.Equals(paymentType, "Cash", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(referenceNumber))
            {
                yield return new ValidationResult(
                    "Reference number is required for non-cash payments.",
                    new[] { nameof(referenceNumber) });
            }
        }
        
        [StringLength(260)]
        public string? ReceiptImagePath { get; set; }
        public string? OcrRawText { get; set; }
        
        
    }
}