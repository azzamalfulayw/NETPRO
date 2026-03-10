using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Rating
{
    public class RatingDto
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        public int StockId { get; set; }
    }
}