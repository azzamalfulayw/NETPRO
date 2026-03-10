using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    [Table("Ratings")]
    public class Rating
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int StockId { get; set; }
        public Stock? Stock  { get; set; }
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}