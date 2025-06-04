//using System;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using projectTracker.Application.Interfaces;
//using projectTracker.Domain.Entities;
//using ProjectTracker.Infrastructure.Data;

//namespace projectTracker.Infrastructure.Services
//{
//    public class PrivilegeRepository : Repository<Privilege>, IPrivilegeRepository
//    {
//        private readonly AppDbContext _context;

//        public PrivilegeRepository(AppDbContext context) : base(context)
//        {
//            _context = context ?? throw new ArgumentNullException(nameof(context));
//        }

//        public Task<Privilege> FindByIdAsync(int id)
//        {
//            throw new NotImplementedException();
//        }

//        //public async Task<Privilege> FindByIdAsync(int id)
//        //{
//        //    return await _context.Set<Privilege>()
//        //        .FirstOrDefaultAsync(p => p.Id == id)
//        //        ?? throw new KeyNotFoundException($"Privilege with ID {id} not found.");
//        //}


//    }
//}