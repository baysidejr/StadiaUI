using StadiaUI.Models;
using StadiaUI.Services;
using StadiaUI.Data;
using Microsoft.EntityFrameworkCore;
using StadiaUI.Data;
using StadiaUI.Models;

namespace StadiaUI.Services;

public class GameCacheService
{
    private readonly GameDbContext _context;
    private readonly SteamService _steamService;
    private readonly SteamGridDbService _steamGridService;
    private readonly IWebHostEnvironment _environment;
    private readonly string _imagesCachePath;

    public GameCacheService(
        GameDbContext context, 
        SteamService steamService,
        SteamGridDbService steamGridService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _steamService = steamService;
        _steamGridService = steamGridService;
        _environment = environment;
        _imagesCachePath = Path.Combine(_environment.WebRootPath, "cached-images");
        
        // Ensure cache directory exists
        Directory.CreateDirectory(_imagesCachePath);
    }

    public async Task<List<CachedGame>> GetGamesAsync()
    {
        return await _context.Games
            .OrderByDescending(g => g.PlaytimeForever)
            .ToListAsync();
    }

    public async Task RefreshGameLibraryAsync()
    {
        Console.WriteLine("Refreshing game library from Steam...");
        
        var ownedGames = await _steamService.GetOwnedGamesAsync();

        Console.WriteLine($"Owned games: {ownedGames?.response?.games.Count}");
        if (ownedGames?.response?.games == null) return;

        foreach (var steamGame in ownedGames.response.games)
        {
            var existingGame = await _context.Games
                .FirstOrDefaultAsync(g => g.SteamAppId == steamGame.appid);

            if (existingGame == null)
            {
                // New game - add it
                existingGame = new CachedGame
                {
                    SteamAppId = steamGame.appid,
                    Name = steamGame.name,
                    SteamIconUrl = steamGame.img_icon_url,
                    PlaytimeForever = steamGame.playtime_forever,
                    LastUpdated = DateTime.Now
                };
                _context.Games.Add(existingGame);
            }
            else
            {
                // Update existing game
                existingGame.Name = steamGame.name;
                existingGame.PlaytimeForever = steamGame.playtime_forever;
                existingGame.LastUpdated = DateTime.Now;
            }

            // Download and cache high-quality image if we don't have one
            if (string.IsNullOrEmpty(existingGame.LocalImagePath))
            {
                await CacheGameImageAsync(existingGame);
                // Add delay to avoid SteamGridDB rate limit
                await Task.Delay(1500);
            }
        }

        // Update cache info
        var cacheInfo = await _context.LibraryCache.FirstOrDefaultAsync();
        if (cacheInfo == null)
        {
            cacheInfo = new GameLibraryCache();
            _context.LibraryCache.Add(cacheInfo);
        }
        
        cacheInfo.LastSteamSync = DateTime.Now;
        cacheInfo.TotalGames = ownedGames.response.games.Count;

        await _context.SaveChangesAsync();
        Console.WriteLine($"Library refresh complete. {ownedGames.response.games.Count} games cached.");
    }

    public async Task CacheGameImageAsync(CachedGame game)
    {
        try
        {
            // Make the HTTP request manually to capture the JSON
            var httpClient = new HttpClient();
            var apiKey = _steamGridService.ApiKey;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var response = await httpClient.GetAsync($"https://www.steamgriddb.com/api/v2/search/autocomplete/{game.SteamAppId}");
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[DEBUG] SteamGridDB JSON for {game.Name} ({game.SteamAppId}): {json}");

            // Now continue as before using your model
            var imageUrl = await _steamGridService.GetBestImageForGame(game.SteamAppId);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var fileName = $"{game.SteamAppId}.jpg";
                var localPath = Path.Combine(_imagesCachePath, fileName);

                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                await File.WriteAllBytesAsync(localPath, imageBytes);

                game.SteamGridImageUrl = imageUrl;
                game.LocalImagePath = $"/cached-images/{fileName}";

                Console.WriteLine($"[SUCCESS] Cached image for {game.Name} ({game.SteamAppId})");
                Console.WriteLine($"[INFO] SteamGridImageUrl: {game.SteamGridImageUrl}");
                Console.WriteLine($"[INFO] LocalImagePath: {game.LocalImagePath}");
            }
            else
            {
                Console.WriteLine($"[WARNING] No image found for {game.Name} ({game.SteamAppId})");
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to cache image for {game.Name} ({game.SteamAppId}): {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetGameImageUrlAsync(int steamAppId)
    {
        var game = await _context.Games
            .FirstOrDefaultAsync(g => g.SteamAppId == steamAppId);

        return game?.LocalImagePath ?? 
               game?.SteamGridImageUrl ?? 
               $"https://steamcdn-a.akamaihd.net/steam/apps/{steamAppId}/header.jpg";
    }

    public async Task ForceImageRefreshAsync(int steamAppId)
    {
        var game = await _context.Games
            .FirstOrDefaultAsync(g => g.SteamAppId == steamAppId);
            
        if (game != null)
        {
            // Delete old cached image
            if (!string.IsNullOrEmpty(game.LocalImagePath))
            {
                var oldPath = Path.Combine(_environment.WebRootPath, game.LocalImagePath.TrimStart('/'));
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                }
            }

            game.LocalImagePath = null;
            game.SteamGridImageUrl = null;
            
            await CacheGameImageAsync(game);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<GameLibraryCache>> GetLibraryCacheAsync()
    {
        return await _context.LibraryCache.ToListAsync();
    }

    public async Task<List<CachedGame>> GetGamesTableAsync()
    {
        return await _context.Games
            .OrderByDescending(g => g.PlaytimeForever)
            .ToListAsync();
    }

    public async Task SeedSampleGamesAsync()
    {
        var samples = new List<CachedGame>
        {
            new CachedGame { SteamAppId = 220, Name = "Half-Life 2", PlaytimeForever = 120, LastUpdated = DateTime.Now, IsInstalled = false },
            new CachedGame { SteamAppId = 620, Name = "Portal 2", PlaytimeForever = 45, LastUpdated = DateTime.Now, IsInstalled = true },
            new CachedGame { SteamAppId = 730, Name = "Counter-Strike: Global Offensive", PlaytimeForever = 300, LastUpdated = DateTime.Now, IsInstalled = false },
            new CachedGame { SteamAppId = 440, Name = "Team Fortress 2", PlaytimeForever = 200, LastUpdated = DateTime.Now, IsInstalled = true },
            new CachedGame { SteamAppId = 72850, Name = "The Elder Scrolls V: Skyrim", PlaytimeForever = 500, LastUpdated = DateTime.Now, IsInstalled = false }
        };
        foreach (var game in samples)
        {
            if (!_context.Games.Any(g => g.SteamAppId == game.SteamAppId))
                _context.Games.Add(game);
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSampleGamesAsync()
    {
        var sampleIds = new[] { 220, 620, 730, 440, 72850 };
        var toDelete = _context.Games.Where(g => sampleIds.Contains(g.SteamAppId));
        _context.Games.RemoveRange(toDelete);
        await _context.SaveChangesAsync();
    }
}