using System.ComponentModel.DataAnnotations;

namespace StadiaUI.Models;

public class CachedGame
{
    [Key]
    public int SteamAppId { get; set; }
    public string Name { get; set; } = "";
    public string? SteamIconUrl { get; set; }
    public string? SteamGridImageUrl { get; set; }
    public string? LocalImagePath { get; set; }
    public DateTime LastUpdated { get; set; }
    public int PlaytimeForever { get; set; }
    public bool IsInstalled { get; set; }
    public string Categories { get; set; } = ""; // JSON string of categories
}