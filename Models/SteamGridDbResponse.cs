namespace StadiaUI.Models;

public class SteamGridDbResponse<T>
{
    public bool success { get; set; }
    public T[] data { get; set; } = Array.Empty<T>();
}