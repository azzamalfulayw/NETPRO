using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace api.Service
{
    public class UserResolverService : IUserResolverService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDBContext _context;

        public UserResolverService(IHttpContextAccessor httpContextAccessor, ApplicationDBContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<AppUser?> GetUserAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var user = context.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                return null;
            }

            var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(subClaim))
            {
                return null;
            }

            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == subClaim);

            // Auto-provision if missing
            if (appUser == null)
            {
                var username = user.FindFirst("preferred_username")?.Value ?? subClaim;
                var email = user.FindFirst(ClaimTypes.Email)?.Value ?? "";

                appUser = new AppUser
                {
                    Id = subClaim,
                    UserName = username,
                    Email = email
                };

                _context.AppUsers.Add(appUser);
                await _context.SaveChangesAsync();
            }

            return appUser;
        }
    }
}
