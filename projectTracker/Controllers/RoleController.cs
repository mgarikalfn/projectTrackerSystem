using System.Linq;
using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using projectTracker.Application.Dto.Role;
using projectTracker.Application.Features.Role.Command;
using projectTracker.Application.Features.Role.Query;

//using projectTracker.Application.Features.Role.Query;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

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
        public async Task<ActionResult> UpdateRole(string id, [FromBody] UpdateRoleDto updateRole)
        {
            var command = _mapper.Map<UpdateRoleCommand>(updateRole);
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
        public async Task<IActionResult> AssignPrivileges([FromBody] AssignPrivilegeCommand request)
        {
            var result = _mediator.Send(request);
            return result.IsCompletedSuccessfully
                ? Ok(result.Result)
                : BadRequest(result.Result.Errors.FirstOrDefault()?.Message);
        }
    }
}
