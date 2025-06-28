namespace Sannel.Arcade.Metadata.Models;

public class LoginRequest
{
	public string Username { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
	public bool Success { get; set; }
	public string Token { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
}