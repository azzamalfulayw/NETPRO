using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using api.Dtos.Rating;
using api.Extensions;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;


namespace api.Controllers
{
    [Route ("api/rating")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly IRatingRepository _ratingRepo;
        private readonly IStockRepository _stockRepo;
        private readonly UserManager<AppUser> _userManager;
        public RatingController(IRatingRepository ratingRepo, IStockRepository stockRepo, UserManager<AppUser> userManager)
        {
            _ratingRepo = ratingRepo;
            _stockRepo = stockRepo;
            _userManager = userManager;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ratings = await _ratingRepo.GetAllAsync();

            var ratingDto = ratings.Select(s => s.ToRatingDto());

            return Ok(ratingDto);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);

            var rating = await _ratingRepo.GetByIdAsync(id);

            if (rating == null)
            {
                return NotFound();
            }

            return Ok(rating.ToRatingDto());
        }

        [HttpPost("{stockId:int}")]
        [Authorize]
        public async Task<IActionResult> Create([FromRoute] int stockId, [FromBody] CreateRatingDto ratingDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var stock = await _stockRepo.GetByIdAsync(stockId);

            if (stock == null)
                return NotFound("Stock not found");

            var existingRating = await _ratingRepo.GetUserRatingForStockAsync(appUser.Id, stockId);

            if (existingRating != null)
                return BadRequest("You already rated this stock");

            var ratingModel = ratingDto.ToRatingCreate(stockId);
            ratingModel.AppUserId = appUser.Id;

            await _ratingRepo.CreateAsync(ratingModel);

            return Ok(ratingModel.ToRatingDto());
        }
        [HttpPut]
        [Route("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateRatingDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var existingRating = await _ratingRepo.GetByIdAsync(id);

            if (existingRating == null)
                return NotFound("Rating not found");

            if (existingRating.AppUserId != appUser.Id)
                return Forbid();

            var rating = await _ratingRepo.UpdateAsync(id, updateDto.ToRatingUpdate());

            if (rating == null)
                return NotFound("Rating not found");

            return Ok(rating.ToRatingDto());
        }
        [HttpDelete]
        [Route ("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);

            var ratingModel = await _ratingRepo.DeleteAsync(id);

            if(ratingModel == null)
            {
                return NotFound("Rating does not exist");
            }

            return Ok(ratingModel.ToRatingDto());
        }
    }
}