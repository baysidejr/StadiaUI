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

    public async Task<string?> GetBestImageForGame(int steamAppId, string imageType = "grid")
    {
        var game = await GetGameBySteamAppId(steamAppId);
        if (game == null) return null;

        SteamGridDbImage[] images = imageType.ToLower() switch
        {
            "hero" => await GetGameHeroes(game.id),
            "grid" => await GetGameGrids(game.id),
            _ => await GetGameGrids(game.id)
        };

        // Return the highest scored image
        var bestImage = images
            .Where(img => !img.nsfw)
            .OrderByDescending(img => img.score)
            .FirstOrDefault();

        return bestImage?.url;
    }
}