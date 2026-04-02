using MediatR;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace api.Features.Comment.Commands
{
    public class CreateCommentCommand : IRequest<api.Models.Comment>
    {
        public required api.Models.Comment Comment { get; set; }
    }

    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, api.Models.Comment>
    {
        private readonly ApplicationDBContext _context;
        public CreateCommentHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Comment> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            await _context.Comments.AddAsync(request.Comment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return request.Comment;
        }
    }

    public class UpdateCommentCommand : IRequest<api.Models.Comment?>
    {
        public int Id { get; set; }
        public required api.Models.Comment CommentModel { get; set; }
    }

    public class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, api.Models.Comment?>
    {
        private readonly ApplicationDBContext _context;
        public UpdateCommentHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Comment?> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
        {
            var existingComment = await _context.Comments.FindAsync(request.Id);
            if (existingComment == null) return null;

            existingComment.Title = request.CommentModel.Title;
            existingComment.Content = request.CommentModel.Content;

            await _context.SaveChangesAsync(cancellationToken);
            return existingComment;
        }
    }

    public class DeleteCommentCommand : IRequest<api.Models.Comment?>
    {
        public int Id { get; set; }
    }

    public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, api.Models.Comment?>
    {
        private readonly ApplicationDBContext _context;
        public DeleteCommentHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Comment?> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            var commentModel = await _context.Comments.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (commentModel == null) return null;

            _context.Comments.Remove(commentModel);
            await _context.SaveChangesAsync(cancellationToken);
            return commentModel;
        }
    }
}
