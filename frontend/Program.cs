using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using frontend;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Models;
using frontend.Services;
using System.Net.Http.Headers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register services
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());

// Register the DelegatingHandler
builder.Services.AddScoped<AuthorizationMessageHandler>();

// Configure HttpClient to use the handler
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5088") }; // Backend URL
    return http;
});

// Add Authorization Core
builder.Services.AddAuthorizationCore();

var host = builder.Build();

// Configure HttpClient to include Authorization header
var localStorage = host.Services.GetRequiredService<LocalStorageService>();
var httpClient = host.Services.GetRequiredService<HttpClient>();

string token = await localStorage.GetItem("token");
if (!string.IsNullOrEmpty(token))
{
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

await host.RunAsync();