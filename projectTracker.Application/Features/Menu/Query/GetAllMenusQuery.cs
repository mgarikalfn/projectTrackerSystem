using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Menu.Query
{
    public class GetAllMenusQuery : IRequest<Result<List<MenuItemDto>>>
    {
    }


    public class GetAllMenuQueryHandler : IRequestHandler<GetAllMenusQuery, Result<List<MenuItemDto>>>
    {
        private readonly IRepository<MenuItem> _menuItemRepository;

        public GetAllMenuQueryHandler(IRepository<MenuItem> menuItemRepository)
        {
            _menuItemRepository = menuItemRepository;
        }
        public async Task<Result<List<MenuItemDto>>> Handle(GetAllMenusQuery request, CancellationToken cancellationToken)
        {
            var menuItems = await _menuItemRepository.GetAllAsync();
            if (menuItems == null || !menuItems.Any())
            {
                return Result.Fail("No menu items found.");
            }
            var menuItemDtos = menuItems.Select(m => new MenuItemDto
            {
                Id = m.Id,
                Name = m.Name,
                Url = m.Url,
                ParentId = m.ParentId,
                Icon = m.Icon,
                Children = m.Children.Select(c => new MenuItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Url = c.Url,
                    ParentId = c.ParentId,
                    Icon = c.Icon
                }).ToList(),
                RequiredPermission = m.RequiredPrivilege,
            }).ToList();
            return Result.Ok(menuItemDtos);
        }
    }
}
