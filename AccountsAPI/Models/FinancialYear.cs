using System;
using System.Collections.Generic;

#nullable disable

namespace AccountsAPI.Models
{
    public partial class FinancialYear
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool? Status { get; set; }
    }
}
