using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymCrudApp.ViewModels.Memberships
{
    public class MembershipEditViewModel : IValidatableObject
    {
        public int MembershipId { get; set; }

        [Required(ErrorMessage = "Please select a member.")]
        [Display(Name = "Member")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Please select a membership plan.")]
        [Display(Name = "Membership Plan")]
        public int PlanId { get; set; }

        [Required(ErrorMessage = "Please provide a start date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Please provide an end date.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Active Subscription")]
        public bool IsActive { get; set; } = true;

        public SelectList? Members { get; set; }
        public SelectList? Plans { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate.Date <= StartDate.Date)
            {
                yield return new ValidationResult(
                    "End date must be later than the start date.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}