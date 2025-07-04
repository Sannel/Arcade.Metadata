using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using Sannel.Arcade.Metadata.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

// Add settings service
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Add scan service
builder.Services.AddScoped<IScanService, ScanService>();

// Add metadata service
builder.Services.AddScoped<IMetadataService, MetadataService>();

// Add authorization services
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState()
	.AddAuthenticationStateDeserialization();

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
