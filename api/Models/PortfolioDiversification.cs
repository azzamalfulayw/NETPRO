using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class PortfolioDiversification
    {
        public List<IndustryAllocation> Industries { get; set; } = new();
    }

    public class IndustryAllocation
    {
        public string Industry { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Percentage { get; set; }
    }
}