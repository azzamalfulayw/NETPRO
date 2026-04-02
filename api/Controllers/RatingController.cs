using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Rating;
using api.Extensions;
using api.Mappers;
using api.Models;
using MediatR;
using api.Features.Rating.Queries;
using api.Features.Rating.Commands;
using api.Features.Stock.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.Interfaces;

namespace api.Controllers
{
    [Route ("api/rating")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserResolverService _userResolverService;

        public RatingController(IMediator mediator, IUserResolverService userResolverService)
        {
            _mediator = mediator;
            _userResolverService = userResolverService;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ratings = await _mediator.Send(new GetAllRatingsQuery());
            var ratingDto = ratings.Select(s => s.ToRatingDto());

            return Ok(ratingDto);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var rating = await _mediator.Send(new GetRatingByIdQuery { Id = id });

            if (rating == null) return NotFound();

            return Ok(rating.ToRatingDto());
        }

        [HttpPost("{stockId:int}")]
        [Authorize]
        public async Task<IActionResult> Create([FromRoute] int stockId, [FromBody] CreateRatingDto ratingDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var appUser = await _userResolverService.GetUserAsync();

            if (appUser == null) return Unauthorized();

            var stock = await _mediator.Send(new GetStockByIdQuery { Id = stockId });
            if (stock == null) return NotFound("Stock not found");

            var existingRating = await _mediator.Send(new GetUserRatingForStockQuery { AppUserId = appUser.Id, StockId = stockId });
            if (existingRating != null) return BadRequest("You already rated this stock");

            var ratingModel = ratingDto.ToRatingCreate(stockId);
            ratingModel.AppUserId = appUser.Id;

            await _mediator.Send(new CreateRatingCommand { RatingModel = ratingModel });

            return Ok(ratingModel.ToRatingDto());
        }

        [HttpPut]
        [Route("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateRatingDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var appUser = await _userResolverService.GetUserAsync();

            if (appUser == null) return Unauthorized();

            var existingRating = await _mediator.Send(new GetRatingByIdQuery { Id = id });
            if (existingRating == null) return NotFound("Rating not found");

            if (existingRating.AppUserId != appUser.Id) return Forbid();

            var rating = await _mediator.Send(new UpdateRatingCommand { Id = id, RatingModel = updateDto.ToRatingUpdate() });

            if (rating == null) return NotFound("Rating not found");

            return Ok(rating.ToRatingDto());
        }

        [HttpDelete]
        [Route ("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var appUser = await _userResolverService.GetUserAsync();
            if (appUser == null) return Unauthorized();

            var existingRating = await _mediator.Send(new GetRatingByIdQuery { Id = id });

            if (existingRating == null) return NotFound("Rating does not exist");
            if (existingRating.AppUserId != appUser.Id) return Forbid();

            var ratingModel = await _mediator.Send(new DeleteRatingCommand { Id = id });

            return Ok(ratingModel.ToRatingDto());
        }
    }
}