// Controllers/MenuController.cs
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Common;
using projectTracker.Application.Features.Menu.Command;
using projectTracker.Application.Features.Menu.Query;
using projectTracker.Infrastructure.Services;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class MenuController : ControllerBase
{
    private readonly MenuService _menuService;
    private readonly IMediator _mediator;


    public MenuController(MenuService menuService, IMediator mediator)
    {
        _menuService = menuService;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMenu()
    {
        var menu = await _menuService.GetFilteredMenuAsync();
        return Ok(menu);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllMenus()
    {
        var result = await _mediator.Send(new GetAllMenusQuery());
        return result.ToActionResult();
    }

    [HttpPost]

    public async Task<IActionResult> Create([FromBody] CreateMenuCommand command)
    {
        var result = await _mediator.Send(command);

        return result.ToActionResult();
    }

    [HttpPut("{id}")]

    public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? Ok(new { success = true })
            : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
    }

    // DELETE
    [HttpDelete("{id}")]

   public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteMenuCommand { Id = id });
        return result.IsSuccess
            ? Ok(new { success = true })
            : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
    }

    // GET BY ID
    [HttpGet("{id}")]

    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetMenuByIdQuery { Id = id });
        return result.ToActionResult();
    }
       

    
}