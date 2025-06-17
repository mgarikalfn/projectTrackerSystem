using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Common;
using projectTracker.Application.Dto.User;
using projectTracker.Application.Features.User.Command;
using projectTracker.Application.Features.User.Query;

namespace projectTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }
        //[HttpGet]
        //public async Task<IActionResult> GetUsers()
        //{
        //    var result = await _mediator.Send(new GetUsersQuery());
        //    return result.ToActionResult();
        //}

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetUserByIdQuery { UserId = id });
            return result.ToActionResult();
        }

        //[HttpPost]
        //public async Task<IActionResult> CreateUser([FromBody] CreateUserDto userDto)
        //{
        //    var result = await _mediator.Send(new CreateUserCommand(userDto));
        //    return result.ToActionResult();
        //}

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto userDto)
        {
            var command = new UpdateUserCommand
            {
                UserId = id,
                AvatarUrl = userDto.AvatarUrl,
                AccountId = userDto.AccountId,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                IsActive = userDto.IsActive,
                DisplayName = userDto.DisplayName,
                TimeZone = userDto.TimeZone,
                CurrentWorkload = userDto.CurrentWorkload,
                Location = userDto.Location,
            };
            var result = await _mediator.Send(command);

            return result.IsSuccess
             ? Ok(new { success = true })
             : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var command = new DeleteUserCommand(id);
            var result = await _mediator.Send(command);
            return result.IsSuccess
             ? Ok(new { success = true })
             : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Message) });
        }
    }
}