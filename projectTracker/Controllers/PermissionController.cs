using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Dto.Privilage;
using projectTracker.Application.Features.Privilege.Command;
using projectTracker.Application.Features.Privilege.Query;

namespace projectTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PermissionController : ControllerBase
    {

        private readonly IMediator _mediator;

        public PermissionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetPermissionByIdQuery(id));
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllPermissionsQuery());
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePermissionDto dto)
        {
            var result = await _mediator.Send(new CreatePermissionCommand(dto));
            return Ok(result);
        }

        [HttpPut ("{id}")]
        public async Task<IActionResult> Update(string id,[FromBody] UpdatePermissionDto dto)
        {
            var command = new UpdatePermissionCommand
            {
                Id = id,
                PermissionName = dto.PermissionName,
                Action = dto.Action,
                Description = dto.Description,
            };
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeletePermissionCommand(id));
            return Ok(result);
        }

    }
}
