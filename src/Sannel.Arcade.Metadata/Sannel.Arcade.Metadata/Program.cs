using Sannel.Arcade.Metadata.Client.Pages;
using Sannel.Arcade.Metadata.Components;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using MudBlazor.Services;

using System.Text;
using Sannel.Arcade.Metadata.Auth.v1.Models;
using Sannel.Arcade.Metadata.Settings.v1;

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

SettingsSliceSetup.Setup(
	builder.Environment,
	builder.Configuration,
	builder.Services);

builder.Services.AddCascadingAuthenticationState();

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();

// Add controllers for API endpoints
builder.Services.AddControllers();

// Add API Explorer and Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
	{
		Title = "Sannel Arcade Metadata API",
		Version = "v1",
		Description = "API for managing arcade metadata and settings"
	});

	// Add JWT authentication to Swagger
	options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
		Name = "Authorization",
		In = Microsoft.OpenApi.Models.ParameterLocation.Header,
		Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT"
	});

	options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
	{
		{
			new Microsoft.OpenApi.Models.OpenApiSecurityScheme
			{
				Reference = new Microsoft.OpenApi.Models.OpenApiReference
				{
					Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

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
// Enable Swagger in development
app.UseSwagger();
app.UseSwaggerUI(options =>
{
	options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sannel Arcade Metadata API v1");
	options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseStaticFiles();

// Map API controllers
app.MapControllers();
app.MapStaticAssets();

app.MapRazorComponents<App>()
	.AddInteractiveWebAssemblyRenderMode()
	.AddAdditionalAssemblies(typeof(Sannel.Arcade.Metadata.Client._Imports).Assembly);

app.Run();
