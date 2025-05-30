using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateToken(AppUser appUser);
    }
}
