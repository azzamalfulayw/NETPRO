using System;
using System.Threading.Tasks;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [Route("api/recommendations")]
    [ApiController]
    public class AiRecommendationController : ControllerBase
    {
        private readonly IAiRecommendationService _recommendationService;
        private readonly ILogger<AiRecommendationController> _logger;

        public AiRecommendationController(
            IAiRecommendationService recommendationService,
            ILogger<AiRecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        [HttpGet("ai/{symbol}")]
        [Authorize]
        public async Task<IActionResult> GetAiRecommendation([FromRoute] string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Stock symbol is required.");
            }

            try
            {
                _logger.LogInformation("Requesting AI recommendation for {Symbol}", symbol);
                
                var recommendation = await _recommendationService.GetRecommendationAsync(symbol);

                if (recommendation == null)
                {
                    return NotFound($"Unable to generate recommendation for symbol {symbol}. Verify the symbol is correct and has sufficient market data.");
                }

                return Ok(recommendation);
            }
            catch (System.Net.Http.HttpRequestException hex)
            {
                _logger.LogWarning("External API rate limit or outage for {Symbol}. Status: {StatusCode}", symbol, hex.StatusCode);
                
                int statusCode = hex.StatusCode.HasValue ? (int)hex.StatusCode.Value : 503;
                string message = statusCode == 429 
                    ? "External API rate limit exceeded. Please wait and try again shortly."
                    : "The AI recommendation service is currently busy or unavailable. Please try again later.";
                
                return StatusCode(statusCode, new { error = message, status = statusCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle AI recommendation request for {Symbol}", symbol);
                return StatusCode(500, new { error = "An unexpected error occurred while processing your request." });
            }
        }
    }
}
