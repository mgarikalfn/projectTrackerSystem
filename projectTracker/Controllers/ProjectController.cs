using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Common;
using projectTracker.Application.Dto;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Dto.Project.MileStone;
using projectTracker.Application.Features.Project.Command;
using projectTracker.Application.Features.Project.Query;
using projectTracker.Application.Features.Projects.Command;
using projectTracker.Application.Interfaces;
using projectTracker.Application.Mapping;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;
namespace projectTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]


    public class ProjectController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IRepository<ProjectTask> _repository;
        public ProjectController(IMediator mediator, AppDbContext dbContext, IMapper mapper, IRepository<ProjectTask> repository)
        {
            _mediator = mediator;
            _dbContext = dbContext;
            _repository = repository;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectResponseDto>> GetProjectById(string id)
        {
            var project = await _dbContext.Projects
                .Include(p => p.Tasks)
                .Include(p => p.SyncHistory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }



            return Ok(project.ToDto());

        }


        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<IActionResult> GetProjects([FromQuery] ProjectFilterDto projectFilter)
        {
            var result = await _mediator.Send(new GetAllProjectsQuery(projectFilter));

            return result.ToActionResult();
        }


        [AllowAnonymous]
        [HttpGet("task/{id}")]
        public async Task<IActionResult> GetProjectTasks(string id)
        {
            var query = _repository.GetQueryable()
                .Where(ts => ts.ProjectId == id);
            // Add as needed

            var tasks = await query.ToListAsync();

            return Ok(tasks.ToDtoList());
        }

        [AllowAnonymous]
        [HttpGet("Detail/{projectId}")]
        public async Task<IActionResult> GetProjectDetails(string projectId)
        {
            var query = new GetProjectDetailsQuery { ProjectId = projectId };
            var result = await _mediator.Send(query);

            

            return Ok(result);
        }


        [AllowAnonymous]
        [HttpPut("Detail/{projectId}")]
        public async Task<IActionResult> UpdateProjectStrategicDetails(string projectId, [FromBody] StrategicProjectDetailDto request)
        {
            var command = new UpdateProjectStrategicDetailsCommand
            {
                Id = projectId,
                OverallStatus = request.OverallStatus,
                ExecutiveSummary = request.ExecutiveSummary,
                OwnerName = request.OwnerName,
                OwnerContact = request.OwnerContact,
                ProjectStartDate = request.ProjectStartDate,
                TargetEndDate = request.TargetEndDate
            };

            var result = await _mediator.Send(command);
            return result.IsSuccess
          ? Ok(new { success = true })
          : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
        }


        // --- Milestone Endpoints ---
        [HttpPost("{projectId}/milestones")]
        [AllowAnonymous]
        public async Task<IActionResult> AddMilestone(string projectId, [FromBody] AddMilestoneCommand command)
        {
            if (projectId != command.ProjectId) return BadRequest("Project ID mismatch.");
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPut("milestones/{milestoneId}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateMilestone(string milestoneId, [FromBody] UpdateMilestoneCommand request)
        {
            var command = new UpdateMilestoneCommand
            {
                Id = milestoneId,
                Name = request.Name,
                DueDate = request.DueDate,
                Status = request.Status,
                Description = request.Description
            };

            var result = await _mediator.Send(command);
            return result.IsSuccess
         ? Ok(new { success = true })
         : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });

        }



        [HttpDelete("milestones/{milestoneId}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveMilestone(string milestoneId)
        {
            var result = await _mediator.Send(new RemoveMilestoneCommand { MilestoneId = milestoneId });
            return result.IsSuccess
       ? Ok(new { success = true })
       : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
        }

        // --- Risk Endpoints ---
        [HttpPost("{projectId}/risks")]
        [AllowAnonymous]
        public async Task<IActionResult> AddRisk(string projectId, [FromBody] AddRiskCommand command)
        {
            if (projectId != command.ProjectId) return BadRequest("Project ID mismatch.");
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPut("risks/{riskId}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateRisk(string riskId, [FromBody] UpdateRiskDto request)
        {
            var command = new UpdateRiskCommand
            {
                RiskId = riskId,
                Description = request.Description,
                Impact = request.Impact,
                Likelihood = request.Likelihood,
                MitigationPlan = request.MitigationPlan,
                Status = request.Status
            };

            var result = await _mediator.Send(command);
            return result.IsSuccess
       ? Ok(new { success = true })
       : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
        }

        [HttpDelete("risks/{riskId}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveRisk(string riskId)
        {
            var result = await _mediator.Send(new RemoveRiskCommand { RiskId = riskId });
            return result.IsSuccess
              ? Ok(new { success = true })
              : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
        }
    }
}

