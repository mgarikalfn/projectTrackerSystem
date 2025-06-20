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
  
    public class DeleteMenuCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }

    
    public class DeleteMenuCommandHandler : IRequestHandler<DeleteMenuCommand, Result>
    {
        private readonly IRepository<MenuItem> _menuRepository;

        public DeleteMenuCommandHandler(IRepository<MenuItem> menuRepository)
            => _menuRepository = menuRepository;

        public async Task<Result> Handle(DeleteMenuCommand request, CancellationToken cancellationToken)
        {
            // Step 1: Find the parent menu item
            var menuItem = await _menuRepository.FindAsync(m => m.Id == request.Id);
            if (menuItem == null)
                return Result.Fail("Menu item not found");

            // Step 2: Find child menu items
            var childMenus = await _menuRepository.GetWhereAsync(m => m.ParentId == request.Id);

            // Step 3: If children exist, delete or deactivate them
            foreach (var child in childMenus)
            {
                child.IsActive = false;
                await _menuRepository.DeleteAsync(child);
            }

            // Step 4: Delete or deactivate the parent item
            menuItem.IsActive = false;
            await _menuRepository.DeleteAsync(menuItem);

            return Result.Ok();
        }

    }
}
