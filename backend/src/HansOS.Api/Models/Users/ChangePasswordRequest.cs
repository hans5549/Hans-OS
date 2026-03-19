using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Users;

public record ChangePasswordRequest(
    [Required][MaxLength(128)] string OldPassword,
    [Required][MinLength(8)][MaxLength(128)] string NewPassword);
