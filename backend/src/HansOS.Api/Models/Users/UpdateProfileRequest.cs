using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Users;

public record UpdateProfileRequest(
    [Required][StringLength(100)] string RealName,
    [EmailAddress][StringLength(256)] string? Email,
    [Phone][StringLength(30)] string? Phone,
    [Url][StringLength(500)] string? Avatar,
    [StringLength(500)] string? Desc);
