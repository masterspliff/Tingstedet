using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using webapp;
using webapp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API
builder.Services.AddScoped(sp => 
{
    // In development, we're using the local API
    var apiBaseAddress = builder.HostEnvironment.IsDevelopment() 
        ? "https://localhost:7272" // Server API address
        : builder.HostEnvironment.BaseAddress;
        
    return new HttpClient { BaseAddress = new Uri(apiBaseAddress) };
});

// Register services
builder.Services.AddScoped<IPostService, PostService>();

// Add BlazorBootstrap
builder.Services.AddBlazorBootstrap();

await builder.Build().RunAsync();
