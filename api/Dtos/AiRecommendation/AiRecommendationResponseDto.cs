using System;
using System.Collections.Generic;

namespace api.Dtos.AiRecommendation
{
    public class AiRecommendationResponseDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Rating { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}
