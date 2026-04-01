using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Comment;
using api.Extensions;
using api.Mappers;
using api.Models;
using MediatR;
using api.Features.Comment.Commands;
using api.Features.Comment.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.Features.Stock.Queries;
using api.Interfaces;

namespace api.Controllers
{
    [Route ("api/comment")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserResolverService _userResolverService;

        public CommentController(IMediator mediator, IUserResolverService userResolverService)
        {
            _mediator = mediator;
            _userResolverService = userResolverService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var comments = await _mediator.Send(new GetAllCommentsQuery());
            var commentDto = comments.Select(s => s.ToCommentDto());
            return Ok(commentDto);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var comment = await _mediator.Send(new GetCommentByIdQuery { Id = id });

            if (comment == null) return NotFound();

            return Ok(comment.ToCommentDto());
        }

        [HttpPost("{stockId:int}")]
        [Authorize]
        public async Task<IActionResult> Create([FromRoute] int stockId, [FromBody] CreateCommentDto commentDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var stockExists = await _mediator.Send(new CheckStockExistsQuery { Id = stockId });
            if (!stockExists) return BadRequest("Stock does not exist");

            var appUser = await _userResolverService.GetUserAsync();

            var commentModel = commentDto.ToCommentFromCreate(stockId);
            commentModel.AppUserId = appUser.Id;

            await _mediator.Send(new CreateCommentCommand { Comment = commentModel });

            return CreatedAtAction(nameof(GetById), new {id = commentModel.Id}, commentModel.ToCommentDto());
        }

        [HttpPut]
        [Route("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCommentRequestDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var appUser = await _userResolverService.GetUserAsync();
            var existingComment = await _mediator.Send(new GetCommentByIdQuery { Id = id });

            if (existingComment == null) return NotFound("Comment not found");
            if (existingComment.AppUserId != appUser.Id) return Forbid();

            var comment = await _mediator.Send(new UpdateCommentCommand { Id = id, CommentModel = updateDto.ToCommentFromUpdate() });

            if (comment == null) return NotFound("Comment not found");

            return Ok(comment.ToCommentDto());
        }

        [HttpDelete]
        [Route("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var appUser = await _userResolverService.GetUserAsync();
            var existingComment = await _mediator.Send(new GetCommentByIdQuery { Id = id });

            if (existingComment == null) return NotFound("Comment does not exist");
            if (existingComment.AppUserId != appUser.Id) return Forbid();
                
            var commentModel = await _mediator.Send(new DeleteCommentCommand { Id = id });

            return Ok(commentModel.ToCommentDto());
        }
    }
}