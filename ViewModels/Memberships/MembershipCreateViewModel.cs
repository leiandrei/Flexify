using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymCrudApp.ViewModels.Memberships
{
    public class MembershipCreateViewModel
    {
        [Required(ErrorMessage = "Please select a member.")]
        [Display(Name = "Member")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Please select a membership plan.")]
        [Display(Name = "Membership Plan")]
        public int PlanId { get; set; }

        [Required(ErrorMessage = "Please provide a start date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        public SelectList? Members { get; set; }
        public SelectList? Plans { get; set; }
    }
}