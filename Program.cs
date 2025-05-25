using StadiaUI.Components;
using StadiaUI.Services;
using StadiaUI.Data;
using StadiaUI.Models;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

// Database
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                     "Data Source=game-library.db"));

// Services
builder.Services.AddSingleton<IntroService>();
builder.Services.AddScoped<GameCacheService>();

// HTTP Clients
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<SteamService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5231"); // Use your dev port
});
builder.Services.AddHttpClient<SteamGridDbService>();

// Configuration
builder.Services.Configure<SteamConfig>(builder.Configuration.GetSection("Steam"));
builder.Services.Configure<SteamGridDbConfig>(builder.Configuration.GetSection("SteamGridDb"));

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Create database and apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        context.Database.EnsureCreated();
        Console.WriteLine("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

app.Run();