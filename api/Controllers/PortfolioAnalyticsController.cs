using api.Extensions;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/portfolioanalytics")]
    [ApiController]
    [Authorize]
    public class PortfolioAnalyticsController : ControllerBase
    {
        private readonly IPortfolioAnalyticsService _portfolioAnalyticsService;
        private readonly UserManager<AppUser> _userManager;

        public PortfolioAnalyticsController(
            IPortfolioAnalyticsService portfolioAnalyticsService,
            UserManager<AppUser> userManager)
        {
            _portfolioAnalyticsService = portfolioAnalyticsService;
            _userManager = userManager;
        }

        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformance()
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var result = await _portfolioAnalyticsService.GetPerformanceAsync(appUser);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int days = 30)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var result = await _portfolioAnalyticsService.GetPortfolioHistoryAsync(appUser, days);
            return Ok(result);
        }

        [HttpGet("diversification")]
        public async Task<IActionResult> GetDiversification()
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var result = await _portfolioAnalyticsService.GetDiversificationAsync(appUser);
            return Ok(result);
        }
    }
}