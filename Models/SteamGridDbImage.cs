namespace StadiaUI.Models;

public class SteamGridDbImage
{
    public int id { get; set; }
    public int score { get; set; }
    public string style { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string thumb { get; set; } = string.Empty;
    public string[] tags { get; set; } = Array.Empty<string>();
    public bool nsfw { get; set; }
}