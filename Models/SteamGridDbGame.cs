namespace StadiaUI.Models;

public class SteamGridDbGame
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string[] types { get; set; } = Array.Empty<string>();
    public bool verified { get; set; }
}