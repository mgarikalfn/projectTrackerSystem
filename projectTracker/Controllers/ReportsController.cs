﻿

using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Dto.Project;
using Microsoft.AspNetCore.Authorization;

namespace projectTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ReportsController : ControllerBase
    {
        private readonly IProjectReportingService _reportingService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IProjectReportingService reportingService, ILogger<ReportsController> logger)
        {
            _reportingService = reportingService;
            _logger = logger;
        }

      
        [HttpGet("projects/{projectKey}/sprint-overview")]
       
        public async Task<IActionResult> GetProjectSprintOverview(string projectKey, CancellationToken ct)
        {
            _logger.LogInformation("Received request for sprint overview for project {ProjectKey}.", projectKey);
            try
            {
                var overview = await _reportingService.GetProjectSprintOverviewAsync(projectKey, ct);
                if (overview == null)
                {
                    return NotFound($"Project '{projectKey}' not found or no sprints associated.");
                }
                _logger.LogInformation("Successfully retrieved sprint overview for project {ProjectKey}.", projectKey);
                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sprint overview for project {ProjectKey}.", projectKey);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("sprints/{sprintId}")]
        
        public async Task<IActionResult> GetSprintReport(Guid sprintId, CancellationToken ct)
        {
            _logger.LogInformation("Received request for detailed report for sprint ID {SprintId}.", sprintId);
            try
            {
                var report = await _reportingService.GetSprintReportAsync(sprintId, ct);
                if (report == null)
                {
                    return NotFound($"Sprint with ID '{sprintId}' not found.");
                }
                _logger.LogInformation("Successfully retrieved detailed report for sprint ID {SprintId}.", sprintId);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed report for sprint ID {SprintId}.", sprintId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}