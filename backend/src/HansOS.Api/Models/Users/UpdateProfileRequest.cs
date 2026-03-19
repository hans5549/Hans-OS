using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Users;

public record UpdateProfileRequest(
    [Required][StringLength(100)] string RealName);
