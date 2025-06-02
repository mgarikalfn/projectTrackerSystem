using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Dto.Privilage;
using projectTracker.Application.Features.Privilege.Command;
using projectTracker.Application.Features.Privilege.Query;

namespace projectTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrivilageController : ControllerBase
    {
        
            private readonly IMediator _mediator;

            public PrivilageController(IMediator mediator)
            {
                _mediator = mediator;
            }

            [HttpGet("{id}")]
            public async Task<IActionResult> GetById(int id)
            {
                var result = await _mediator.Send(new GetPrivilegeByIdQuery(id));
                return Ok(result);
            }

            [HttpGet]
            public async Task<IActionResult> GetAll()
            {
                var result = await _mediator.Send(new GetAllPrivilegesQuery());
                return Ok(result);
            }

            [HttpPost]
            public async Task<IActionResult> Create([FromBody] CreatePrivilegeDto dto)
            {
                var result = await _mediator.Send(new CreatePrivilegeCommand(dto));
                return Ok(result);
            }

            [HttpPut]
            public async Task<IActionResult> Update([FromBody] UpdatePrivilegeDto dto)
            {
                var result = await _mediator.Send(new UpdatePrivilegeCommand(dto));
                return Ok(result);
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> Delete(int id)
            {
                var result = await _mediator.Send(new DeletePrivilegeCommand(id));
                return Ok(result);
            }
        
    }
}
