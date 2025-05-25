using System.ComponentModel.DataAnnotations;

namespace StadiaUI.Models;

public class GameLibraryCache
{
    [Key]
    public int Id { get; set; }
    public DateTime LastSteamSync { get; set; }
    public int TotalGames { get; set; }
}