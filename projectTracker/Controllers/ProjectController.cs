using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Common;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Features.Project.Query;

namespace projectTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   

    public class ProjectController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProjectController(IMediator mediator) {
            _mediator = mediator;
        }


        [HttpGet("{id}")]
        public IActionResult GetProjectById(int id)
        {
            return Ok(new { ProjectId = id, Name = "Test Project" });
        }

        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<IActionResult> GetProjects([FromQuery] ProjectFilterDto projectFilter)
        {
            var result = await _mediator.Send(new GetAllProjectsQuery(projectFilter));

            return result.ToActionResult();
        }
    }

}
