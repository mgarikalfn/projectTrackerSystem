using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Common;
using projectTracker.Application.Dto.Role;
using projectTracker.Application.Features.Role.Command;
using projectTracker.Application.Features.Role.Query;

//using projectTracker.Application.Features.Role.Query;

namespace projectTracker.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public RoleController( IMediator mediator, IMapper mapper)
        { 
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _mediator.Send(new GetAllRolesQuery());
            return result.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var result = await _mediator.Send(new GetRolesByIdQuery { Id = id });
           return result.ToActionResult();
        }


        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand createRole)
        {
            var result = await _mediator.Send(createRole);
            return result.ToActionResult();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(string id , [FromBody] RoleUpdateDto roleDto)
        {
            var command = _mapper.Map<UpdateRoleCommand>(roleDto);
            command.Id = id;
            var result = await _mediator.Send(command);
            return result.IsSuccess
            ? Ok(new { success = true })
            : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var result = _mediator.Send(new DeleteRoleCommand { Id = id });
            return Ok(result);
        }
        [HttpPost("assign-privileges")]
        public async Task<IActionResult> AssignPrivileges([FromBody] AssignPermissionCommand request)
        {
            var result = await _mediator.Send(request);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors.FirstOrDefault()?.Message);
        }
    }
}
