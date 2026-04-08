using System;
using System.Linq;
using Mapster;
using api.Models;
using api.Dtos.Comment;
using api.Dtos.Rating;
using api.Dtos.Stock;

namespace api.Mappings
{
    public class MapsterConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Comment Mappings
            config.NewConfig<Comment, CommentDto>()
                .Map(dest => dest.CreatedOn, src => src.CtreatedOn)
                .Map(dest => dest.CreatedBy, src => src.AppUser != null ? src.AppUser.UserName : string.Empty);

            config.NewConfig<CreateCommentDto, Comment>();
            config.NewConfig<UpdateCommentRequestDto, Comment>();

            // Rating Mappings
            config.NewConfig<Rating, RatingDto>()
                .Map(dest => dest.CreatedBy, src => src.AppUser != null ? src.AppUser.UserName : string.Empty);

            config.NewConfig<CreateRatingDto, Rating>();
            config.NewConfig<UpdateRatingDto, Rating>();

            // Stock Mappings
            config.NewConfig<Stock, StockDto>()
                .Map(dest => dest.AverageRating, src => src.Ratings != null && src.Ratings.Any() ? Math.Round(src.Ratings.Average(r => r.Score), 2) : 0)
                .Map(dest => dest.RatingCount, src => src.Ratings != null ? src.Ratings.Count : 0);

            config.NewConfig<CreateStockRequestDto, Stock>();
            config.NewConfig<UpdateStockRequestDto, Stock>();
        }
    }
}
