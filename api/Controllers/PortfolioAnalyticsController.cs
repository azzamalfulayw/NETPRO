using api.Extensions;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/portfolioanalytics")]
    [ApiController]
    [Authorize]
    public class PortfolioAnalyticsController : ControllerBase
    {
        private readonly IPortfolioAnalyticsService _portfolioAnalyticsService;
        private readonly IUserResolverService _userResolverService;

        public PortfolioAnalyticsController(
            IPortfolioAnalyticsService portfolioAnalyticsService,
            IUserResolverService userResolverService)
        {
            _portfolioAnalyticsService = portfolioAnalyticsService;
            _userResolverService = userResolverService;
        }

        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformance()
        {
            var appUser = await _userResolverService.GetUserAsync();

            if (appUser == null)
                return Unauthorized();

            var result = await _portfolioAnalyticsService.GetPerformanceAsync(appUser);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int days = 30)
        {
            var appUser = await _userResolverService.GetUserAsync();

            if (appUser == null)
                return Unauthorized();

            var result = await _portfolioAnalyticsService.GetPortfolioHistoryAsync(appUser, days);
            return Ok(result);
        }

        [HttpGet("diversification")]
        public async Task<IActionResult> GetDiversification()
        {
            var appUser = await _userResolverService.GetUserAsync();

            if (appUser == null)
                return Unauthorized();

            var result = await _portfolioAnalyticsService.GetDiversificationAsync(appUser);
            return Ok(result);
        }
    }
}