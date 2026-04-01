using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IUserResolverService
    {
        Task<AppUser?> GetUserAsync();
    }
}
