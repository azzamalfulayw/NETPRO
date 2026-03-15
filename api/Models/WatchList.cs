using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    [Table("WatchLists")]
    public class WatchList
    {
        public string AppUserId { get; set; } = string.Empty;
        public int StockId { get; set; }
        public DateTime AddedOn { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public AppUser AppUser { get; set; } = null!;
        public Stock Stock { get; set; } = null!;
    }
}