using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Rating
{
    public class UpdateRatingDto
    {
        [Required]
        [Range(0,5, ErrorMessage ="Rating must be between 0 and 5")]
        public int Score { get; set; }
    }
}