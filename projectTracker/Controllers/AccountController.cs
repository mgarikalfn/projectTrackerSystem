using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Domain.Entities;
using projectTracker.Application.Interfaces;
using projectTracker.Application.Dto.Account;
using Microsoft.AspNetCore.Authorization;
using Azure.Core;
using projectTracker.Application.Features.Role.Command;
using MediatR;

namespace projectTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IMediator _mediator;

        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, SignInManager<AppUser> signInManager, IMediator mediator)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] RegisterUserDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
                return BadRequest("Email is already registered.");

            var user = new AppUser
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                UserName = registerDto.Email
            };

            // Create user with password
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Optionally add roles/claims here

            var token = await _tokenService.GenerateToken(user);
            return Ok(new { token });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
                return BadRequest("Invalid email or password.");

            // Fix: Add the required 'lockoutOnFailure' parameter with a value (e.g., false)
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
                return BadRequest("unauthorized access");

            var token = await _tokenService.GenerateToken(user);
            return Ok(new { token });
        }

        [HttpPost("Assign-Role")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleCommand request)
        {
            var result = _mediator.Send(request);
            return result.IsCompletedSuccessfully
                ? Ok(result.Result)
                : BadRequest(result.Result.Errors.FirstOrDefault()?.Message);
        }
    }
}
