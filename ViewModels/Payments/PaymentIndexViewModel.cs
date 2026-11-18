using System;
using System.Collections.Generic;

namespace GymCrudApp.ViewModels.Payments
{
    public class PaymentIndexViewModel
    {
        public List<PaymentListItemViewModel> Payments { get; set; } = new();
        public string? MemberSearch { get; set; }
        public DateTime? FilterDate { get; set; }
        public decimal TotalValidAmount { get; set; }
    }
}