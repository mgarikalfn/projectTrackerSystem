using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Menu.Command
{
  
    public class DeleteMenuCommand : IRequest<Result<bool>>
    {
        public int Id { get; set; }
    }

    
    public class DeleteMenuCommandHandler : IRequestHandler<DeleteMenuCommand, Result<bool>>
    {
        private readonly IRepository<MenuItem> _menuRepository;

        public DeleteMenuCommandHandler(IRepository<MenuItem> menuRepository)
            => _menuRepository = menuRepository;

        public async Task<Result<bool>> Handle(DeleteMenuCommand request, CancellationToken cancellationToken)
        {
            var menuItem = await _menuRepository.FindAsync(m => m.Id == request.Id);
            if (menuItem == null)
                return Result.Fail("Menu item not found");

            // Soft delete (or use _menuRepository.DeleteAsync for hard delete)
            menuItem.IsActive = false;
            await _menuRepository.UpdateAsync(menuItem);

            return Result.Ok(true);
        }
    }
}
