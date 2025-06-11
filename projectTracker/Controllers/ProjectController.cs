using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Common;
using projectTracker.Application.Dto;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Features.Project.Query;
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
        public ProjectController(IMediator mediator, AppDbContext dbContext, IMapper mapper,IRepository<ProjectTask> repository)
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

    }
}
