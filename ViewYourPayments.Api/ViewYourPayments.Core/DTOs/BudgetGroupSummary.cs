using System;
using System.Collections.Generic;
using System.Text;

namespace ViewYourPayments.Core.DTOs
{
    public class BudgetGroupSummary
    {
        public decimal TotalAmount { get; set; }
        public BudgetGroupItem[] BudgetGroups { get; set; }
    }
}
