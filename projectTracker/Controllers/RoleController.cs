using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<List<RoleDto>>> GetRoles()
        {
            var result = await _mediator.Send(new GetAllRolesQuery());
            if (result == null || !result.Any())
            {
                return NotFound("No roles found.");
            }
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetRoleById(string id)
        {
            var result = await _mediator.Send(new GetRolesByIdQuery { Id = id });
            if (result == null)
            {
                return NotFound($"Role with ID {id} not found.");
            }
            return Ok(result);
        }


        [HttpPost]
        public async Task<ActionResult> CreateRole([FromBody] CreateRoleCommand createRole)
        {
            var result = await _mediator.Send(createRole);
            if (result == null)
            {
                return NotFound("can't create role");
            }

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateRole(string id , [FromBody] RoleUpdateDto roleDto)
        {
            var command = _mapper.Map<UpdateRoleCommand>(roleDto);
            command.Id = id;
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteRole(string id)
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
