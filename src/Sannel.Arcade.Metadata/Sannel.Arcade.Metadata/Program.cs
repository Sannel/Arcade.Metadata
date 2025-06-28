using Sannel.Arcade.Metadata.Client.Pages;
using Sannel.Arcade.Metadata.Components;
using Sannel.Arcade.Metadata.Models;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using MudBlazor.Services;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add user-specific configuration file
var userConfigDirectory = Path.Combine(
	Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
	".sannel", "arcade", "metadata");
var userConfigFile = Path.Combine(userConfigDirectory, "appsettings.json");

if (File.Exists(userConfigFile))
{
	builder.Configuration.AddJsonFile(userConfigFile, optional: true, reloadOnChange: true);
}

// Configure authentication settings
builder.Services.Configure<AuthenticationConfig>(
	builder.Configuration.GetSection("Authentication"));

var authConfig = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfig>();
if (authConfig is null)
{
	throw new InvalidOperationException("Authentication configuration is missing.");
}

// Add JWT authentication
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = authConfig.JwtIssuer,
		ValidAudience = authConfig.JwtAudience,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(authConfig.JwtSecret))
	};
});

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();

// Add controllers for API endpoints
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseStaticFiles();

// Map API controllers
app.MapControllers();

app.MapRazorComponents<App>()
	.AddInteractiveWebAssemblyRenderMode()
	.AddAdditionalAssemblies(typeof(Sannel.Arcade.Metadata.Client._Imports).Assembly);

app.Run();
