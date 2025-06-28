using Sannel.Arcade.Metadata.Client.Pages;
using Sannel.Arcade.Metadata.Components;

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

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveWebAssemblyComponents();

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


app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
	.AddInteractiveWebAssemblyRenderMode()
	.AddAdditionalAssemblies(typeof(Sannel.Arcade.Metadata.Client._Imports).Assembly);

app.Run();
