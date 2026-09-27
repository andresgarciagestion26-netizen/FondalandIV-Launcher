using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using FondalandIVLauncher.Models;

namespace FondalandIVLauncher.Services;

public sealed class ModManager
{
    public async Task<ServerManifest> LoadManifestAsync(string url, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        await using var stream = await client.GetStreamAsync(url, ct);
        return await JsonSerializer.DeserializeAsync<ServerManifest>(stream, cancellationToken: ct)
               ?? throw new InvalidDataException("El manifest está vacío o no es válido.");
    }

    public async Task<(int ok, int missing, int outdated)> CheckAsync(string modsDir, ServerManifest manifest)
    {
        Directory.CreateDirectory(modsDir);
        int ok = 0, missing = 0, outdated = 0;
        foreach (var mod in manifest.Mods)
        {
            var path = Path.Combine(modsDir, mod.File);
            if (!File.Exists(path)) { missing++; continue; }
            if (string.IsNullOrWhiteSpace(mod.Sha256)) { ok++; continue; }
            var hash = await Sha256Async(path);
            if (hash.Equals(mod.Sha256, StringComparison.OrdinalIgnoreCase)) ok++; else outdated++;
        }
        return (ok, missing, outdated);
    }

    public static async Task<string> Sha256Async(string file)
    {
        await using var stream = File.OpenRead(file);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
