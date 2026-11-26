using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymCrudApp.ViewModels.Payments
{
    public class PaymentIndexViewModel
    {
        public List<PaymentListItemViewModel> Payments { get; set; } = new();
        public string? MemberSearch { get; set; }
        public DateTime? FilterDate { get; set; }
        public string? PaymentType { get; set; }
        public int? PlanId { get; set; }
        public decimal TotalValidAmount { get; set; }
        public IEnumerable<SelectListItem> PlanOptions { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> PaymentTypeOptions { get; set; } = Array.Empty<SelectListItem>();
    }
}
