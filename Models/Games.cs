namespace StadiaUI.Models;

public class Game
{
    public int appid { get; set; }
    public string name { get; set; }
    public int playtime_forever { get; set; }
    public string img_icon_url { get; set; }
    public string img_logo_url { get; set; }
    public bool has_community_visible_stats { get; set; }
    public int playtime_windows_forever { get; set; }
    public int playtime_mac_forever { get; set; }
    public int playtime_linux_forever { get; set; }
    public int playtime_deck_forever { get; set; }
    public int rtime_last_played { get; set; }
    public int[] content_descriptorids { get; set; }
    public int playtime_disconnected { get; set; }
}