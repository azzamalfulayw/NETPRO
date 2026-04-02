using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace api.Extensions
{
    public static class ClaimExtensions
    {
        public static string? GetUsername(this ClaimsPrincipal user)
        {
            return user.Claims.SingleOrDefault(x => x.Type.Equals("preferred_username"))?.Value 
                   ?? user.Claims.SingleOrDefault(x => x.Type.Equals(ClaimTypes.NameIdentifier))?.Value;
        }
    }
}