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
        [MinLength(0, ErrorMessage ="Rating must between 0 - 5")]
        [MaxLength(5,ErrorMessage ="Rating must between 0 - 5")]
        public int Score { get; set; }
    }
}