namespace FondalandIVLauncher.Models;

public sealed class ServerManifest
{
    public string Minecraft { get; set; } = "1.20.1";
    public string Forge { get; set; } = "47.3.0";
    public string ServerName { get; set; } = "Fondaland IV";
    public List<ModEntry> Mods { get; set; } = new();
}

public sealed class ModEntry
{
    public string File { get; set; } = "";
    public string Version { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string Url { get; set; } = "";
}
