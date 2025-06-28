using System.ComponentModel.DataAnnotations;

namespace Sannel.Arcade.Metadata.Client.Models;

public class LoginRequest
{
	[Required]
	public string Username { get; set; } = string.Empty;
	
	[Required]
	public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
	public bool Success { get; set; }
	public string Token { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
}