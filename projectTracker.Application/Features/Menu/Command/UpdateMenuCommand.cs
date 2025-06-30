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
    
    public class UpdateMenuCommand : IRequest<Result<MenuItem>>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public string? RequiredPrivilege { get; set; }
        public int? ParentId { get; set; }
        public int? Order { get; set; }
        public bool? IsActive { get; set; }
    }

    
    public class UpdateMenuCommandHandler : IRequestHandler<UpdateMenuCommand, Result<MenuItem>>
    {
        private readonly IRepository<MenuItem> _menuRepository;

        public UpdateMenuCommandHandler(IRepository<MenuItem> menuRepository)
            => _menuRepository = menuRepository;

        public async Task<Result<MenuItem>> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
        {
            var menuItem = await _menuRepository.FindAsync(m => m.Id == request.Id);
            if (menuItem == null)
                return Result.Fail("Menu item not found");

            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(request.Name) && menuItem.Name != request.Name)
            {
                hasChanges = true;
                menuItem.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Url) && menuItem.Url != request.Url)
            {
                hasChanges = true;
                menuItem.Url = request.Url;
            }

            if (!string.IsNullOrEmpty(request.Icon) && menuItem.Icon != request.Icon)
            {
                hasChanges = true;
                menuItem.Icon = request.Icon;
            }

            if (!string.IsNullOrWhiteSpace(request.RequiredPrivilege) && menuItem.RequiredPrivilege != request.RequiredPrivilege)
            {
                hasChanges = true;
                menuItem.RequiredPrivilege = request.RequiredPrivilege;
            }

            // ParentId change check
            if ( menuItem.ParentId != request.ParentId)
            {
                hasChanges = true;
                menuItem.ParentId = request.ParentId;
            }

            // Order change check and must be positive
            if (request.Order != null && menuItem.Order != request.Order)
            {
                if (request.Order <= 0)
                    return Result.Fail("Order must be a positive number");
                hasChanges = true;
                menuItem.Order = request.Order.Value;
            }

            // IsActive change check
            if (request.IsActive != null && menuItem.IsActive != request.IsActive)
            {
                hasChanges = true;
                menuItem.IsActive = request.IsActive.Value;
            }

            if (hasChanges)
                await _menuRepository.UpdateAsync(menuItem);

            return Result.Ok(menuItem);
        }
    }
}
