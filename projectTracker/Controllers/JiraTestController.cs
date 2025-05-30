using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Interfaces;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JiraTestController : ControllerBase
    {
        private readonly IProjectManegementAdapter _jiraAdapter;
        private readonly IRiskCalculatorService _riskCalculator;
        private readonly AppDbContext _context;
        private readonly ISyncManager _syncManager;

        public JiraTestController(IProjectManegementAdapter jiraAdapter,IRiskCalculatorService riskCalculator,AppDbContext context , ISyncManager syncManager)
        {
            _jiraAdapter = jiraAdapter;
            _riskCalculator = riskCalculator;
            _context = context;
            _syncManager = syncManager;
        }


        [HttpGet("fetch")]
        public async Task<IActionResult> GetAllProjects()
        {
            var projects = await _jiraAdapter.GetProjectsAsync(CancellationToken.None);
            return Ok(projects);
        }
        [HttpGet("metrics/{projectKey}")]
        public async Task<IActionResult> GetProjectMetrics(string projectKey)
        {
            var metrics = await _jiraAdapter.GetProjectMetricsAsync(projectKey, CancellationToken.None);
            var calculatedRisk = _riskCalculator.Calculate(metrics); 
            return Ok(calculatedRisk);
        }

        [AllowAnonymous]
        [HttpGet("GetProjects")]
        public async Task<IActionResult> GetProjects()
        {
        // await  _syncManager.SyncAsync(CancellationToken.None);
            var projects = await _context.Projects.ToListAsync(CancellationToken.None);
            return Ok(projects);
        }
    }
}
