using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace projectTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   

    public class ProjectController : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult GetProjectById(int id)
        {
            return Ok(new { ProjectId = id, Name = "Test Project" });
        }

        [AllowAnonymous]
        [HttpGet("public")]
        public IActionResult PublicEndpoint()
        {
            return Ok("This is public");
        }
    }

}
