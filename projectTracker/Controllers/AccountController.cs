
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Domain.Entities;
using projectTracker.Application.Interfaces;
using projectTracker.Application.Dto.Account;
using Microsoft.AspNetCore.Authorization;
using Azure.Core;
using projectTracker.Application.Features.Role.Command;
using MediatR;
using System.ComponentModel.DataAnnotations;
using projectTracker.Application.Dto.User;

namespace projectTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IMediator _mediator;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, SignInManager<AppUser> signInManager, IMediator mediator, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _mediator = mediator;
            _logger = logger;
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

        // Conceptual Login API Endpoint (e.g., in AccountController)
        [HttpPost("api/account/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, request.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user.MustChangePassword)
                {
                    // Indicate to the frontend that password change is required
                    return Ok(new LoginResponseDto
                    {
                        Success = true,
                        RequiresPasswordChange = true, // Frontend reads this
                        UserId = user.Id,
                        Email = user.Email
                    });
                }
                var token = await _tokenService.GenerateToken(user);
                // Standard successful login response
                return Ok(new LoginResponseDto { Success = true, UserId = user.Id, Email = user.Email , Token = token });
            }
            // ... handle other login failures (locked out, invalid credentials) ...
            return Unauthorized(new { message = "Invalid login attempt." });
        }
        public class AssignRoleResultDto
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public IEnumerable<string> Errors { get; set; }
        }

        // Then in your controller:
        [HttpPost("assign-roles")]
        [AllowAnonymous]
        public async Task<ActionResult<AssignRoleResultDto>> AssignRoles([FromBody] AssignRoleCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new AssignRoleResultDto
            {
                Success = result.IsSuccess,
                Message = result.IsSuccess ? result.Value : null,
                Errors = result.IsFailed ? result.Errors.Select(e => e.Message) : null
            });
        }

        
        [HttpPost("api/account/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId); 
            if (user == null) return NotFound("User not found.");

            if (!user.MustChangePassword && request.IsFirstLoginChange == true) 
            {
                // Log suspicious activity or return error if a normal user tries to use the 'forced change' flow
                // For now, allow it to proceed if not explicitly a first-login change.
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                _logger.LogError("Failed to change password for user {UserId}: {Errors}", user.Id, string.Join(", ", changePasswordResult.Errors.Select(e => e.Description)));
                return BadRequest(changePasswordResult.Errors);
            }

            // Set MustChangePassword to false upon successful change
            user.MustChangePassword = false;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to update MustChangePassword flag for user {UserId}: {Errors}", user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                // Decide how to handle this critical failure; may require manual intervention
            }

            // Optionally, re-sign the user in if needed (e.g., if security stamp changed)
            await _signInManager.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("Password changed successfully for user {Email}", user.Email);
            return Ok(new { message = "Password changed successfully." });
        }
    }

    
}
