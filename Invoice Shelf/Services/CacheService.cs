using System.Text.Json;

namespace InvoiceShelf.Services;

/// <summary>
/// Résultat d'une lecture de cache : indique si une valeur existe, si elle est
/// périmée (au-delà du TTL) et la date à laquelle elle a été écrite.
/// </summary>
public readonly record struct CacheResult<T>(bool HasValue, bool IsExpired, T? Value, DateTimeOffset? CachedAt)
{
    /// <summary>Vrai si une valeur exploitable est disponible, même périmée.</summary>
    public bool CanServeStale => HasValue;

    /// <summary>Vrai si une valeur fraîche (non périmée) est disponible.</summary>
    public bool IsFresh => HasValue && !IsExpired;
}

/// <summary>
/// Cache disque simple, clé/valeur, sérialisé en JSON, avec une durée de vie (TTL) par entrée.
/// Utilisé pour éviter de recharger les données réseau (factures, devis, etc.) à chaque
/// changement de page. L'utilisateur peut forcer un rafraîchissement via pull-to-refresh,
/// ce qui contourne la lecture du cache et réécrit une entrée fraîche.
/// </summary>
public interface ICacheService
{
    /// <summary>Durée de validité par défaut d'une entrée de cache.</summary>
    static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);

    Task<CacheResult<T>> GetAsync<T>(string key, TimeSpan? ttl = null);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);

    /// <summary>Taille totale actuelle du cache disque, en octets.</summary>
    Task<long> GetTotalSizeAsync();

    /// <summary>Supprime toutes les entrées de cache disque.</summary>
    Task ClearAllAsync();
}

public class CacheService : ICacheService
{
    private readonly string _cacheDirectory;

    // Un verrou par clé pour éviter les lectures/écritures concurrentes sur le même fichier
    // (par ex. Loaded et pull-to-refresh déclenchés à quelques millisecondes d'intervalle).
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CacheService()
    {
        _cacheDirectory = Path.Combine(FileSystem.AppDataDirectory, "cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    private record CacheEnvelope<T>(DateTimeOffset CachedAt, T? Data);

    private string PathFor(string key)
    {
        // Les clés peuvent contenir des caractères non valides pour un nom de fichier (ex: "invoices:1").
        string safeKey = string.Concat(key.Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_'));
        return Path.Combine(_cacheDirectory, $"{safeKey}.json");
    }

    private SemaphoreSlim LockFor(string key) => _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    public async Task<CacheResult<T>> GetAsync<T>(string key, TimeSpan? ttl = null)
    {
        string path = PathFor(key);
        var effectiveTtl = ttl ?? ICacheService.DefaultTtl;

        SemaphoreSlim sem = LockFor(key);
        await sem.WaitAsync();
        try
        {
            if (!File.Exists(path))
                return new CacheResult<T>(false, true, default, null);

            string json = await File.ReadAllTextAsync(path);
            var envelope = JsonSerializer.Deserialize<CacheEnvelope<T>>(json, JsonOptions);

            if (envelope is null)
                return new CacheResult<T>(false, true, default, null);

            bool expired = DateTimeOffset.UtcNow - envelope.CachedAt > effectiveTtl;
            return new CacheResult<T>(true, expired, envelope.Data, envelope.CachedAt);
        }
        catch
        {
            // Fichier corrompu / JSON invalide : on considère qu'il n'y a pas de cache exploitable.
            return new CacheResult<T>(false, true, default, null);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        string path = PathFor(key);
        var envelope = new CacheEnvelope<T>(DateTimeOffset.UtcNow, value);
        string json = JsonSerializer.Serialize(envelope, JsonOptions);

        SemaphoreSlim sem = LockFor(key);
        await sem.WaitAsync();
        try
        {
            // Écriture sur fichier temporaire puis remplacement atomique pour éviter
            // un fichier tronqué si l'appli est tuée en plein milieu de l'écriture.
            string tempPath = path + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task RemoveAsync(string key)
    {
        string path = PathFor(key);
        SemaphoreSlim sem = LockFor(key);
        await sem.WaitAsync();
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        finally
        {
            sem.Release();
        }
    }

    public Task<long> GetTotalSizeAsync()
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(_cacheDirectory))
                return 0L;

            long total = 0;
            foreach (string file in Directory.EnumerateFiles(_cacheDirectory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // Fichier supprimé entre l'énumération et la lecture : on l'ignore.
                }
            }
            return total;
        });
    }

    public async Task ClearAllAsync()
    {
        if (!Directory.Exists(_cacheDirectory))
            return;

        // On verrouille chaque clé connue le temps de vider le dossier pour éviter
        // qu'une écriture concurrente (SetAsync) ne recrée un fichier pendant le nettoyage.
        var semaphores = _locks.Values.ToArray();
        foreach (var sem in semaphores)
            await sem.WaitAsync();

        try
        {
            foreach (string file in Directory.EnumerateFiles(_cacheDirectory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // On continue même si un fichier ne peut pas être supprimé.
                }
            }
        }
        finally
        {
            foreach (var sem in semaphores)
                sem.Release();
        }
    }
}
