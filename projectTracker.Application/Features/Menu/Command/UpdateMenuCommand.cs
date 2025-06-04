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

            // Partial update (only modify provided fields)
            if (request.Name != null) menuItem.Name = request.Name;
            if (request.Url != null) menuItem.Url = request.Url;
            if (request.Icon != null) menuItem.Icon = request.Icon;
            if (request.RequiredPrivilege != null) menuItem.RequiredPrivilege = request.RequiredPrivilege;
            if (request.ParentId.HasValue) menuItem.ParentId = request.ParentId;
            if (request.Order.HasValue) menuItem.Order = request.Order.Value;
            if (request.IsActive.HasValue) menuItem.IsActive = request.IsActive.Value;

            await _menuRepository.UpdateAsync(menuItem);
            return Result.Ok(menuItem);
        }
    }
}
