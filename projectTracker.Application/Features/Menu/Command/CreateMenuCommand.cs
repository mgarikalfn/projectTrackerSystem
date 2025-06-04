// Features/Menu/Commands/CreateMenuCommand.cs
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Menu.Command
{
    public class CreateMenuCommand : IRequest<Result<MenuItem>>
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public string RequiredPrivilege { get; set; }
        public int? ParentId { get; set; }
        public int Order { get; set; } = 0;
    }

    public class CreateMenuCommandHandler : IRequestHandler<CreateMenuCommand, Result<MenuItem>>
    {
        private readonly IRepository<MenuItem> _menuRepository;

        public CreateMenuCommandHandler(IRepository<MenuItem> menuRepository)
        {
            _menuRepository = menuRepository;
        }

        public async Task<Result<MenuItem>> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
        {
            var menuItem = new MenuItem
            {
                Name = request.Name,
                Url = request.Url,
                Icon = request.Icon,
                RequiredPrivilege = request.RequiredPrivilege,
                ParentId = request.ParentId,
                Order = request.Order,
                IsActive = true
            };

            await _menuRepository.AddAsync(menuItem);
            return Result.Ok(menuItem);
        }
    }
}