using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using StadiaUI.Models;
using StadiaUI.Services;

namespace StadiaUI.Api;

[ApiController]
[Route("api/[controller]")]
public class SteamProxyController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly SteamConfig _config;
    private readonly SteamGridDbService _steamGridDbService;

    public SteamProxyController(
        IHttpClientFactory httpClientFactory, 
        IConfiguration configuration,
        SteamGridDbService steamGridDbService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = configuration.GetSection("Steam").Get<SteamConfig>();
        _steamGridDbService = steamGridDbService;
    }

    [HttpGet("ownedgames")]
    public async Task<IActionResult> GetOwnedGames()
    {
        Console.WriteLine("Fetching owned games from Steam url");
        var url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?steamid={_config.SteamId}&include_appinfo=true&key={_config.ApiKey}";
        var response = await _httpClient.GetAsync(url);
        Console.WriteLine("Response: " + response.IsSuccessStatusCode + " " + response.StatusCode + " " + await response.Content.ReadAsStringAsync());
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    [HttpGet("game/{steamAppId}/image")]
    public async Task<IActionResult> GetGameImage(int steamAppId, [FromQuery] string type = "grid")
    {
        var imageUrl = await _steamGridDbService.GetBestImageForGame(steamAppId, type);
        
        if (string.IsNullOrEmpty(imageUrl))
            return NotFound($"No {type} image found for Steam app {steamAppId}");

        return Ok(new { imageUrl, type, steamAppId });
    }

    [HttpGet("games/images")]
    public async Task<IActionResult> GetMultipleGameImages([FromQuery] int[] steamAppIds, [FromQuery] string type = "grid")
    {
        var tasks = steamAppIds.Select(async appId => new
        {
            steamAppId = appId,
            imageUrl = await _steamGridDbService.GetBestImageForGame(appId, type)
        });

        var results = await Task.WhenAll(tasks);
        return Ok(results.Where(r => !string.IsNullOrEmpty(r.imageUrl)));
    }
}