// Controllers/MenuController.cs
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost]

    public async Task<IActionResult> Create([FromBody] CreateMenuCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(result.Errors);
    }

    [HttpPut("{id}")]

    public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuCommand command)
    {
        command.Id = id; // Ensure ID consistency
        return HandleResult(await _mediator.Send(command));
    }

    // DELETE
    [HttpDelete("{id}")]

    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _mediator.Send(new DeleteMenuCommand { Id = id }));

    // GET BY ID
    [HttpGet("{id}")]

    public async Task<IActionResult> GetById(int id)
        => HandleResult(await _mediator.Send(new GetMenuByIdQuery { Id = id }));

    // Helper for consistent API responses
    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(result.Errors);
    }
}