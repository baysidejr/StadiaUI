using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using StadiaUI.Models;

namespace StadiaUI.Services;

public class SteamGridDbService
{
    private readonly HttpClient _httpClient;
    private readonly SteamGridDbConfig _config;
    private const string BaseUrl = "https://www.steamgriddb.com/api/v2";

    public SteamGridDbService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _config = configuration.GetSection("SteamGridDb").Get<SteamGridDbConfig>() ?? new();
        
        // Set up the API key header
        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }
    }

    public string ApiKey => _config.ApiKey;

    public async Task<SteamGridDbGame?> SearchGameBySteamId(int steamAppId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/search/autocomplete/{steamAppId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SteamGridDbResponse<SteamGridDbGame>>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            return result?.data?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching for game {steamAppId}: {ex.Message}");
            return null;
        }
    }

    public async Task<SteamGridDbImage[]> GetGameGrids(int gameId, string dimensions = "600x900")
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/grids/game/{gameId}?dimensions={dimensions}");
            if (!response.IsSuccessStatusCode) return Array.Empty<SteamGridDbImage>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SteamGridDbResponse<SteamGridDbImage>>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            return result?.data ?? Array.Empty<SteamGridDbImage>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting grids for game {gameId}: {ex.Message}");
            return Array.Empty<SteamGridDbImage>();
        }
    }

    public async Task<SteamGridDbImage[]> GetGameHeroes(int gameId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/heroes/game/{gameId}");
            if (!response.IsSuccessStatusCode) return Array.Empty<SteamGridDbImage>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SteamGridDbResponse<SteamGridDbImage>>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            return result?.data ?? Array.Empty<SteamGridDbImage>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting heroes for game {gameId}: {ex.Message}");
            return Array.Empty<SteamGridDbImage>();
        }
    }

    public async Task<SteamGridDbGame?> GetGameBySteamAppId(int steamAppId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/games/steam/{steamAppId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                if (doc.RootElement.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object)
                {
                    var game = JsonSerializer.Deserialize<SteamGridDbGame>(dataProp.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    return game;
                }
                // If data is not an object (e.g., it's an array or null), return null
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting game by SteamAppId {steamAppId}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<SteamGridDbGame>> SearchGamesByNameAsync(string name)
    {
        try
        {
            Console.WriteLine($"Searching for games by name '{name}'");
            var response = await _httpClient.GetAsync($"{BaseUrl}/search/autocomplete/{Uri.EscapeDataString(name)}");
            if (!response.IsSuccessStatusCode) return new List<SteamGridDbGame>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SteamGridDbResponse<SteamGridDbGame>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return result?.data?.ToList() ?? new List<SteamGridDbGame>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching for games by name '{name}': {ex.Message}");
            return new List<SteamGridDbGame>();
        }
    }

    public async Task<string?> GetBestImageForGameById(int steamGridDbGameId, string imageType = "grid")
    {
        Console.WriteLine($"Getting best image for game {steamGridDbGameId} with type {imageType}");
        SteamGridDbImage[] images = imageType.ToLower() switch
        {
            "hero" => await GetGameHeroes(steamGridDbGameId),
            "grid" => await GetGameGrids(steamGridDbGameId),
            _ => await GetGameGrids(steamGridDbGameId)
        };

        var bestImage = images
            .Where(img => !img.nsfw)
            .OrderByDescending(img => img.score)
            .FirstOrDefault();

        Console.WriteLine($"Best image: {bestImage?.url}");
        return bestImage?.url;
    }

    public async Task<string?> GetBestImageForGame(int steamAppId, int? steamGridDbGameId = null, string imageType = "grid")
    {
        Console.WriteLine($"Getting best image for game {steamAppId} with type {imageType}");
        int? gameId = steamGridDbGameId;
        if (!gameId.HasValue)
        {   
            Console.WriteLine($"Getting game by SteamAppId {steamAppId}");
            var game = await GetGameBySteamAppId(steamAppId);
            if (game == null) return null;
            gameId = game.id;
        }

        Console.WriteLine($"Getting best image for game {gameId.Value} with type {imageType}");
        return await GetBestImageForGameById(gameId.Value, imageType);
    }
}