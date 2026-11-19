using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrudApp.Models
{
    public class Memberships : IValidatableObject
    {
        // primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int membershipId { get; set; }

        // foreign key linking to Members table
        [Required(ErrorMessage = "This field is required. Please select a member.")]
        public int memberId { get; set; }

        [ForeignKey("memberId")]
        public Members? Member { get; set; }

        // foreign key linking to MembershipPlans table
        [Required(ErrorMessage = "This field is required. Please select a membership plan.")]
        public int planId { get; set; }

        [ForeignKey("planId")]
        public MembershipPlans? Plan { get; set; }

        // subscription start and end dates
        [Required(ErrorMessage = "This field is required. Please provide a start date.")]
        [DataType(DataType.Date)]
        public DateTime startDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "This field is required. Please provide an end date.")]
        [DataType(DataType.Date)]
        public DateTime endDate { get; set; }

        public bool isActive { get; set; } = true;

        // nullable because no reminder has been sent when a membership is first created
        [DataType(DataType.DateTime)]
        public DateTime? lastReminderSent { get; set; }

        // cross-property validation to ensure endDate comes after startDate
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (endDate.Date <= startDate.Date)
            {
                yield return new ValidationResult(
                    "End date must be later than the start date.",
                    new[] { nameof(endDate) });
            }
        }
    }
}