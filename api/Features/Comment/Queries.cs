using MediatR;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace api.Features.Comment.Queries
{
    public class GetAllCommentsQuery : IRequest<List<api.Models.Comment>> { }
    
    public class GetAllCommentsHandler : IRequestHandler<GetAllCommentsQuery, List<api.Models.Comment>>
    {
        private readonly ApplicationDBContext _context;
        public GetAllCommentsHandler(ApplicationDBContext context) => _context = context;
        public async Task<List<api.Models.Comment>> Handle(GetAllCommentsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Comments.Include(a => a.AppUser).ToListAsync(cancellationToken);
        }
    }

    public class GetCommentByIdQuery : IRequest<api.Models.Comment?>
    {
        public int Id { get; set; }
    }

    public class GetCommentByIdHandler : IRequestHandler<GetCommentByIdQuery, api.Models.Comment?>
    {
        private readonly ApplicationDBContext _context;
        public GetCommentByIdHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Comment?> Handle(GetCommentByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Comments.Include(a => a.AppUser).FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        }
    }
}
