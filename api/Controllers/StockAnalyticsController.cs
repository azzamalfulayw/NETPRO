using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/stockanalytics")]
    [ApiController]
    public class StockAnalyticsController : ControllerBase
    {
        private readonly IPortfolioAnalyticsService _portfolioAnalyticsService;

        public StockAnalyticsController(IPortfolioAnalyticsService portfolioAnalyticsService)
        {
            _portfolioAnalyticsService = portfolioAnalyticsService;
        }

        [HttpGet("{id:int}/performance")]
        public async Task<IActionResult> GetStockPerformance([FromRoute] int id)
        {
            var result = await _portfolioAnalyticsService.GetStockPerformanceAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}