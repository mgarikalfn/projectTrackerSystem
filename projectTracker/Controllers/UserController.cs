using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Domain.Entities; 
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Dto;
using projectTracker.Application.Dto.User;
using Microsoft.AspNetCore.Authorization;
using projectTracker.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class UserController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<UserRole> _roleManager;
    private readonly ILogger<UserController> _logger;
    private readonly IRepository<AppUser> _userRepository;

    public UserController(
        UserManager<AppUser> userManager,
        RoleManager<UserRole> roleManager,
        ILogger<UserController> logger,
        IRepository<AppUser> useRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _userRepository = useRepository;
    }

    
    [HttpPost("local")]
   
    public async Task<IActionResult> CreateLocalUser([FromBody] CreateLocalUserRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        
        if (await _userManager.FindByEmailAsync(request.Email) != null)
        {
            return Conflict(new { message = $"User with email '{request.Email}' already exists." });
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true, 
            FirstName = request.FirstName,
            LastName = request.LastName,
            DisplayName = $"{request.FirstName} {request.LastName}".Trim(),
            IsActive = true,
            Source = UserSource.Local,
            MustChangePassword = true 
        };

        string generatedPassword = GenerateRandomPassword(12); 
        var createResult = await _userManager.CreateAsync(user, generatedPassword);

        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create local user {Email}: {Errors}", request.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return BadRequest(createResult.Errors);
        }

        // Assign default/initial roles
        var rolesToAssign = request.Roles.Any() ? request.Roles : new List<string> { "Team Member" }; // Default role

        foreach (var roleName in rolesToAssign)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
            else
            {
                _logger.LogWarning("Attempted to assign non-existent role '{RoleName}' to user {Email}. Please ensure this role exists in Identity.", roleName, user.Email);
            }
        }

        _logger.LogInformation("Created new local user {Email} with temporary password. Assigned roles: {Roles}", user.Email, string.Join(", ", rolesToAssign));

        return Ok(new UserCreationResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Source = user.Source.ToString(),
            IsActive = user.IsActive,
            Roles = rolesToAssign,
            GeneratedPassword = generatedPassword 
        });
    }

    [HttpGet]
   
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
    {

        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserResponseDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Source = user.Source.ToString(),
                IsActive = user.IsActive,
                FirstName = user.FirstName, 
                LastName = user.LastName,   
                Roles = roles.ToList()
            });
        }

        _logger.LogInformation("Retrieved {UserCount} users.", userDtos.Count);
        return Ok(userDtos);
    }

   
    [HttpGet("{id}")]
   
    public async Task<ActionResult<UserResponseDto>> GetUserById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Attempted to retrieve non-existent user with ID: {UserId}", id);
            return NotFound($"User with ID '{id}' not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Source = user.Source.ToString(),
            IsActive = user.IsActive,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AccountId = user.AccountId, 
            AvatarUrl = user.AvatarUrl,
            TimeZone = user.TimeZone,
            Location = user.Location,
            Roles = roles.ToList()
        };

        _logger.LogInformation("Retrieved user {Email} (ID: {Id}).", user.Email, user.Id);
        return Ok(userDto);
    }

   
   
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Attempted to update non-existent user with ID: {UserId}", id);
            return NotFound($"User with ID '{id}' not found.");
        }

        // === Apply updates based on user source ===
        if (user.Source == UserSource.Local)
        {
            // For local users, allow updating of all basic profile fields
            user.FirstName = !string.IsNullOrEmpty(request.FirstName) ? request.FirstName : user.FirstName;
            user.LastName = !string.IsNullOrEmpty(request.LastName) ? request.LastName : user.LastName;
            user.DisplayName = !string.IsNullOrEmpty(request.DisplayName) ? request.DisplayName : $"{user.FirstName} {user.LastName}".Trim();

            // Email/UserName change for local users
            if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email)
            {
                // Check if the new email is already taken by another user
                var existingUserWithNewEmail = await _userManager.FindByEmailAsync(request.Email);
                if (existingUserWithNewEmail != null && existingUserWithNewEmail.Id != user.Id)
                {
                    _logger.LogWarning("Attempted to change email for user {UserId} to already taken email: {NewEmail}", user.Id, request.Email);
                    ModelState.AddModelError("Email", "The new email address is already in use by another user.");
                    return BadRequest(ModelState);
                }

                var setEmailResult = await _userManager.SetEmailAsync(user, request.Email);
                if (!setEmailResult.Succeeded)
                {
                    _logger.LogError("Failed to update email for user {UserId}: {Errors}", user.Id, string.Join(", ", setEmailResult.Errors.Select(e => e.Description)));
                    return BadRequest(setEmailResult.Errors);
                }
                // UserName usually follows Email in Identity
                var setUserNameResult = await _userManager.SetUserNameAsync(user, request.Email);
                if (!setUserNameResult.Succeeded)
                {
                    _logger.LogError("Failed to update UserName for user {UserId} after email change: {Errors}", user.Id, string.Join(", ", setUserNameResult.Errors.Select(e => e.Description)));
                    return BadRequest(setUserNameResult.Errors);
                }
            }

            user.TimeZone = !string.IsNullOrEmpty(request.TimeZone) ? request.TimeZone : user.TimeZone;
            user.Location = !string.IsNullOrEmpty(request.Location) ? request.Location : user.Location;
        }
        else if (user.Source == UserSource.Jira)
        {
            // For Jira-synced users, typically only 'IsActive' and 'Roles' are locally mutable.
            // Other fields (DisplayName, Email, AccountId, AvatarUrl, FirstName, LastName) are
            // considered authoritative from Jira and would be overwritten by sync.
            _logger.LogInformation("Updating Jira-synced user {Email} (ID: {Id}). Only 'IsActive', 'TimeZone', and 'Location' (if provided) are directly mutable via local API.", user.Email, user.Id);

            // TimeZone and Location could be allowed to be overridden locally,
            // but be aware that if Jira also provides these, the next sync might revert them.
            user.TimeZone = !string.IsNullOrEmpty(request.TimeZone) ? request.TimeZone : user.TimeZone;
            user.Location = !string.IsNullOrEmpty(request.Location) ? request.Location : user.Location;

            // Explicitly prevent updating other Jira-managed fields if they are sent in the request
            // For example, if you explicitly want to ignore request.DisplayName or request.Email for Jira users
            if (!string.IsNullOrEmpty(request.FirstName)) _logger.LogWarning("Attempted to update FirstName for Jira-synced user {Email}. This field is managed by Jira sync.", user.Email);
            if (!string.IsNullOrEmpty(request.LastName)) _logger.LogWarning("Attempted to update LastName for Jira-synced user {Email}. This field is managed by Jira sync.", user.Email);
            if (!string.IsNullOrEmpty(request.DisplayName)) _logger.LogWarning("Attempted to update DisplayName for Jira-synced user {Email}. This field is managed by Jira sync.", user.Email);
            if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email) _logger.LogWarning("Attempted to update Email for Jira-synced user {Email}. This field is managed by Jira sync.", user.Email);
        }

        // IsActive can be managed locally for all user types
        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        // Apply changes to the user object itself (persists FirstName, LastName, DisplayName, TimeZone, Location, IsActive)
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _logger.LogError("Failed to update user {UserId} profile fields: {Errors}", user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            return BadRequest(updateResult.Errors);
        }

        // === Update roles for all user types ===
        if (request.Roles != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(request.Roles).ToList();
            var rolesToAdd = request.Roles.Except(currentRoles).ToList();

            var invalidRolesFound = new List<string>();

            // Validate roles to be added
            foreach (var roleName in rolesToAdd)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    invalidRolesFound.Add(roleName);
                    _logger.LogWarning("Requested role '{RoleName}' to add for user {Email} does not exist.", roleName, user.Email);
                }
            }
            // Validate roles to be removed (less critical but good for strictness)
            foreach (var roleName in rolesToRemove)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    // This scenario is less likely if currentRoles are valid, but defensive check
                    invalidRolesFound.Add(roleName);
                    _logger.LogWarning("Requested role '{RoleName}' to remove for user {Email} does not exist.", roleName, user.Email);
                }
            }

            if (invalidRolesFound.Any())
            {
                return BadRequest(new { message = $"Failed to update roles. The following roles do not exist: {string.Join(", ", invalidRolesFound)}." });
            }

            // Perform role modifications
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("Failed to remove roles {Roles} from user {Email}: {Errors}", string.Join(", ", rolesToRemove), user.Email, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return BadRequest(new { message = "Failed to remove some roles." });
                }
                _logger.LogDebug("Removed roles {Roles} from user {Email}", string.Join(", ", rolesToRemove), user.Email);
            }
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _logger.LogError("Failed to add roles {Roles} to user {Email}: {Errors}", string.Join(", ", rolesToAdd), user.Email, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    return BadRequest(new { message = "Failed to add some roles." });
                }
                _logger.LogDebug("Added roles {Roles} to user {Email}", string.Join(", ", rolesToAdd), user.Email);
            }
        }

        _logger.LogInformation("User {Email} (ID: {Id}) updated successfully.", user.Email, user.Id);
        return NoContent(); // 204 No Content for successful update
    }


    [HttpDelete("{id}")]
   
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Attempted to delete non-existent user with ID: {UserId}", id);
            return NotFound($"User with ID '{id}' not found.");
        }

        if (user.Source == UserSource.Jira)
        {
            
            user.IsActive = false;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to soft-delete Jira-synced user {UserId}: {Errors}", user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                return StatusCode(500, new { message = "Failed to deactivate Jira-synced user." });
            }
            _logger.LogInformation("Soft-deleted (deactivated) Jira-synced user {Email} (ID: {Id}).", user.Email, user.Id);
        }
        else // Local user
        {
            // For local users, a hard delete is appropriate as they are managed solely here.
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                _logger.LogError("Failed to hard-delete local user {UserId}: {Errors}", user.Id, string.Join(", ", deleteResult.Errors.Select(e => e.Description)));
                return StatusCode(500, new { message = "Failed to delete local user." });
            }
            _logger.LogInformation("Hard-deleted local user {Email} (ID: {Id}).", user.Email, user.Id);
        }

        return NoContent(); // 204 No Content for successful delete/deactivation
    }


    private string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+";
        var random = new Random(); // This is not cryptographically secure for passwords
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}