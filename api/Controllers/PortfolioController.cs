using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Features.Portfolio.Queries;
using api.Features.Portfolio.Commands;
using api.Features.Stock.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/portfolio")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly IUserResolverService _userResolverService;
        private readonly IMediator _mediator;

        public PortfolioController(IUserResolverService userResolverService, IMediator mediator)
        {
            _userResolverService = userResolverService;
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserPortfolio()
        {
            var appUser = await _userResolverService.GetUserAsync();
            var userPortfolio = await _mediator.Send(new GetUserPortfolioQuery { User = appUser });
            return Ok(userPortfolio);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddPortfolio(string symbol)
        {
            var appUser = await _userResolverService.GetUserAsync();
            var stock = await _mediator.Send(new GetStockBySymbolQuery { Symbol = symbol });

            if (stock == null) return BadRequest("Stock not found!");
            
            var userPortfolio = await _mediator.Send(new GetUserPortfolioQuery { User = appUser });

            if (userPortfolio.Any(e => e.Symbol.ToLower() == symbol.ToLower()))
                return BadRequest("Cannot add same stock to portfolio");

            var portfolioModel = new Portfolio
            {
                StockId = stock.Id,
                AppUserId = appUser.Id
            };

            await _mediator.Send(new CreatePortfolioCommand { Portfolio = portfolioModel });

            return Created();
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeletePortfolio(string symbol)
        {
            var appUser = await _userResolverService.GetUserAsync();

            var userPortfolio = await _mediator.Send(new GetUserPortfolioQuery { User = appUser });

            var filteredstock = userPortfolio.Where(s => s.Symbol.ToLower() == symbol.ToLower()).ToList();

            if (filteredstock.Count() == 1)
            {
                await _mediator.Send(new DeletePortfolioCommand { AppUser = appUser, Symbol = symbol });
            }
            else
            {
                return BadRequest("Stock not on your portfolio");
            }

            return Ok();
        }
    }
}