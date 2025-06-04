using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Menu.Query
{
    
    public class GetMenuByIdQuery : IRequest<Result<MenuItem>>
    {
        public int Id { get; set; }
    }

    
    public class GetMenuByIdQueryHandler : IRequestHandler<GetMenuByIdQuery, Result<MenuItem>>
    {
        private readonly IRepository<MenuItem> _menuRepository;

        public GetMenuByIdQueryHandler(IRepository<MenuItem> menuRepository)
            => _menuRepository = menuRepository;

        public async Task<Result<MenuItem>> Handle(GetMenuByIdQuery request, CancellationToken cancellationToken)
        {
            var menuItem = await _menuRepository.FindAsync(m => m.Id == request.Id);
            return menuItem != null
                ? Result.Ok(menuItem)
                : Result.Fail("Menu item not found");
        }
    }
}
