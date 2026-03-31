using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.WatchList;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Features.WatchList.Queries;
using api.Features.WatchList.Commands;
using api.Features.Stock.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace api.Controllers
{
    [Route("api/watchlist")]
    [ApiController]
    public class WatchListController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMediator _mediator;

        public WatchListController(UserManager<AppUser> userManager, IMediator mediator)
        {
            _userManager = userManager;
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserWatchList()
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null) return Unauthorized();

            var userWatchList = await _mediator.Send(new GetUserWatchListQuery { User = appUser });

            return Ok(userWatchList);
        }

        [HttpPost("{stockId:int}")]
        [Authorize]
        public async Task<IActionResult> AddToWatchList(
            [FromRoute] int stockId,
            [FromBody] CteateWatchListRequestDto dto)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null) return Unauthorized();

            var stock = await _mediator.Send(new GetStockByIdQuery { Id = stockId });

            if (stock == null) return NotFound("Stock not found");

            var userWatchList = await _mediator.Send(new GetUserWatchListQuery { User = appUser });

            if (userWatchList.Any(w => w.StockId == stockId))
                return BadRequest("Stock already exists in watchlist");

            var watchListModel = new WatchList
            {
                AppUserId = appUser.Id,
                StockId = stockId,
                AddedOn = DateTime.UtcNow,
                Notes = dto?.Notes
            };

            await _mediator.Send(new CreateWatchListCommand { WatchList = watchListModel });

            return Ok("Stock added to watchlist successfully");
        }

        [HttpDelete]
        [Authorize]
        [Route("{stockId:int}")]
        public async Task<IActionResult> RemoveFromWatchList([FromRoute] int stockId)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null) return Unauthorized();

            var deletedWatchlist = await _mediator.Send(new DeleteWatchListCommand { AppUser = appUser, StockId = stockId });

            if (deletedWatchlist == null) return NotFound("Stock not found in watchlist");

            return Ok();
        }
    }       
}