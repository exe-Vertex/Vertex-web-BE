using System;
using System.ComponentModel.DataAnnotations;

namespace Vertex_web_BE.Models
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Tên không được để trống.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên phải từ 2 đến 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải chứa ít nhất 8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$", 
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ cái và 1 chữ số.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Reset token is required.")]
        [StringLength(200)]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must contain between 8 and 128 characters.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*d).{8,}$",
            ErrorMessage = "Password must contain at least one letter and one number.")]
        public string NewPassword { get; set; } = string.Empty;
    }
    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    }

    public class MeResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ExternalLoginRequest
    {
        [Required(ErrorMessage = "Provider không được để trống.")]
        public string Provider { get; set; } = string.Empty; // "google" | "github"

        [Required(ErrorMessage = "Token không được để trống.")]
        public string Token { get; set; } = string.Empty;
    }
}
