using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using StadiaUI.Models;

namespace StadiaUI.Services;

public class SteamService
{
    private readonly HttpClient _httpClient;
    private OwnedGamesResponse _cache;

    public SteamService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OwnedGamesResponse> GetOwnedGamesAsync()
    {
        // Return cached data if available
        if (_cache != null)
            return _cache;

        Console.WriteLine("Fetching owned games from Steam...");
        var response = await _httpClient.GetAsync("/api/steamproxy/ownedgames");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        _cache = JsonSerializer.Deserialize<OwnedGamesResponse>(json);
        return _cache;
    }
}