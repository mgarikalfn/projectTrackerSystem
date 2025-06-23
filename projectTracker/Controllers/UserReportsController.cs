using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Dto.Report;
using projectTracker.Infrastructure.Services;

namespace projectTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class UserReportsController : ControllerBase
    {

            private readonly UserReportService _userReportService;
            private readonly ILogger<UserReportsController> _logger;

            public UserReportsController(UserReportService userReportService, ILogger<UserReportsController> logger)
            {
                _userReportService = userReportService;
                _logger = logger;
            }

            /// <summary>
            /// Gets a summary of all projects a specific user has been involved in.
            /// </summary>
            /// <param name="userId">The ID of the user (AppUser.Id).</param>
            /// <param name="ct">Cancellation token.</param>
            /// <returns>A list of UserProjectSummaryDto.</returns>
            // GET /api/UserReports/{userId}/projects
            [HttpGet("{userId}/projects")]
            public async Task<ActionResult<List<UserProjectSummaryDto>>> GetUserProjectsSummary(string userId, CancellationToken ct)
            {
                _logger.LogInformation("API: Request to get project summaries for user ID: {UserId}", userId);
                var summaries = await _userReportService.GetUserProjectsSummaryAsync(userId, ct);
                return Ok(summaries);
            }

            /// <summary>
            /// Gets a detailed contribution report for a specific user within a specific project.
            /// </summary>
            /// <param name="userId">The ID of the user (AppUser.Id).</param>
            /// <param name="projectId">The GUID ID of the project.</param>
            /// <param name="ct">Cancellation token.</param>
            /// <returns>A UserProjectContributionDetailDto.</returns>
            // GET /api/UserReports/{userId}/projects/{projectId}/contributions
            [HttpGet("{userId}/projects/{projectId}/contributions")]
            public async Task<ActionResult<UserProjectContributionDetailDto>> GetUserProjectContributionDetail(string userId, string projectId, CancellationToken ct)
            {
                _logger.LogInformation("API: Request to get detailed contribution for user ID: {UserId} in project ID: {ProjectId}", userId, projectId);
                var detail = await _userReportService.GetUserProjectContributionDetailAsync(userId, projectId, ct);

                if (detail == null)
                {
                    return NotFound($"User '{userId}' or Project '{projectId}' not found, or no contributions found.");
                }

                return Ok(detail);
            }
        
    }
}

