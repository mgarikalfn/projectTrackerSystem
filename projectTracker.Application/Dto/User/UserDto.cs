

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; 

namespace projectTracker.Application.Dto.User {  
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } // Optional: for persistent login sessions
    }

  
    public class LoginResponseDto
    {
        public bool Success { get; set; } 
        public string Message { get; set; } = string.Empty; 
        public string? UserId { get; set; } 
        public string? Email { get; set; } 
        public string? DisplayName { get; set; } 
        public string? Token { get; set; } 
        public bool RequiresPasswordChange { get; set; } = false;

        public IEnumerable<string> Roles { get; set; } = new List<string>(); 
        public IEnumerable<string> Errors { get; set; } = new List<string>(); 
    }

   
    public class ChangePasswordRequestDto
    {
       
        [Required(ErrorMessage = "User ID is required.")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)] // Example minimum length
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        
        public bool IsFirstLoginChange { get; set; } = false;
    }

    
    public class CreateLocalUserRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

       
        public List<string> Roles { get; set; } = new List<string>();
    }

   
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; 
        public bool IsActive { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Jira-specific properties (will be empty for local users)
        public string AccountId { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string TimeZone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new List<string>();
    }

   
    public class UserCreationResponseDto : UserResponseDto
    {
        public string GeneratedPassword { get; set; } = string.Empty; 
    }

   
    public class UpdateUserRequestDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DisplayName { get; set; }
        public bool? IsActive { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        public List<string>? Roles { get; set; }

        public string? TimeZone { get; set; }
        public string? Location { get; set; }
    }
}