using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class AppUser
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public List<Portfolio> Portfolios {get; set;} = new List<Portfolio>();
        public List<WatchList> Watchlists { get; set; } = new List<WatchList>();
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}