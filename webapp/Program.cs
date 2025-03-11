using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using webapp;
using webapp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration from appsettings.json files
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true);

// Listening on the server port (5027)
builder.Services.AddScoped(sp =>
{
    string baseAddress;
    if (builder.HostEnvironment.IsDevelopment())
    {
        baseAddress = "http://localhost:5027/";
    }
    else
    {
        baseAddress = "https://tingstedet-api-ddbbcxhhc7ebhzcj.swedencentral-01.azurewebsites.net/"
                      ?? throw new InvalidOperationException("API_BASE_URL environment variable not configured");
    }
    Console.WriteLine($"API_BASE_URL: {baseAddress}");
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

// Register services
builder.Services.AddScoped<IPostService, PostService>();

// Add BlazorBootstrap
builder.Services.AddBlazorBootstrap();

await builder.Build().RunAsync();
