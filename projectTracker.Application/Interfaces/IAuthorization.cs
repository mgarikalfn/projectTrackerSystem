using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Interfaces
{
    public interface IAuthorization
    {
       Task<bool> IsAuthorizedAsync(string username, string action);
        //bool IsAuthenticated(string token);
        IEnumerable<Claim> GetClaims(string token);
    }
}
