using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Rating;
using api.Models;

namespace api.Mappers
{
    public static class RatingMapper
    {
        public static RatingDto ToRatingDto(this Rating ratingModel)
        {
            return new RatingDto
            {
              Id = ratingModel.Id,
              Score = ratingModel.Score,
              CreatedOn = ratingModel.CreatedOn,
              CreatedBy = ratingModel.AppUser.UserName,
              StockId = ratingModel.StockId
            };
        }

        public static Rating ToRatingCreate(this CreateRatingDto ratingDto, int stockId)
        {
            return new Rating
            {
                Score = ratingDto.Score,
                StockId = stockId
            };
        }

        public static Rating ToRatingUpdate(this UpdateRatingDto ratingDto)
        {
            return new Rating
            {
                Score = ratingDto.Score
            };
        }
    }
}