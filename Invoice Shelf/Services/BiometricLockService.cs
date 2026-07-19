using Maui.Biometric;

namespace InvoiceShelf.Services;

/// <summary>
/// Gère le verrouillage biométrique de l'application : préférence utilisateur
/// (activé/désactivé), vérification de la disponibilité du capteur sur l'appareil,
/// et déclenchement du prompt d'authentification (empreinte / reconnaissance faciale).
/// </summary>
public interface IBiometricLockService
{
    /// <summary>Vrai si l'utilisateur a activé le verrouillage biométrique dans les paramètres.</summary>
    Task<bool> IsEnabledAsync();

    /// <summary>Vérifie si un capteur biométrique est disponible et configuré sur l'appareil.</summary>
    Task<bool> IsAvailableAsync();

    /// <summary>Déclenche le prompt biométrique natif et retourne si l'authentification a réussi.</summary>
    Task<bool> AuthenticateAsync(string reason);

    /// <summary>
    /// Demande une confirmation biométrique puis, si elle réussit, active et persiste le
    /// verrouillage. Ne persiste rien si l'authentification échoue ou est annulée.
    /// </summary>
    Task<bool> TryEnableAsync();

    /// <summary>Désactive et efface la préférence de verrouillage biométrique.</summary>
    void Disable();
}

public class BiometricLockService : IBiometricLockService
{
    // Sécurité : la préférence est stockée dans SecureStorage (adossé au Keystore
    // Android) et non dans Preferences (SharedPreferences en clair), afin qu'elle ne
    // soit pas trivialement modifiable sur un appareil compromis. Cela reste une
    // protection "d'épaule" : les données elles-mêmes sont protégées par ailleurs
    // (token en SecureStorage, purge du cache à la déconnexion).
    private const string StorageKey = "biometric_lock_enabled";

    /// <summary>Ancienne clé Preferences (versions ≤ 1.4.0), migrée vers SecureStorage.</summary>
    private const string LegacyPreferenceKey = "biometric_lock_enabled";

    private readonly IBiometricAuthentication _biometric;

    // Cache mémoire pour éviter un aller-retour SecureStorage (opération async
    // potentiellement coûteuse) à chaque OnResume.
    private bool? _cachedEnabled;

    public BiometricLockService(IBiometricAuthentication biometric)
    {
        _biometric = biometric ?? throw new ArgumentNullException(nameof(biometric));
    }

    public async Task<bool> IsEnabledAsync()
    {
        if (_cachedEnabled.HasValue)
            return _cachedEnabled.Value;

        try
        {
            string? stored = await SecureStorage.Default.GetAsync(StorageKey);

            if (stored is null && Preferences.Default.ContainsKey(LegacyPreferenceKey))
            {
                // Migration depuis l'ancien stockage Preferences (non chiffré).
                bool legacyValue = Preferences.Default.Get(LegacyPreferenceKey, false);
                if (legacyValue)
                    await SecureStorage.Default.SetAsync(StorageKey, "true");
                Preferences.Default.Remove(LegacyPreferenceKey);

                _cachedEnabled = legacyValue;
                return legacyValue;
            }

            _cachedEnabled = stored == "true";
            return _cachedEnabled.Value;
        }
        catch
        {
            // SecureStorage indisponible (Keystore corrompu, plateforme non supportée...) :
            // on considère le verrou désactivé plutôt que de bloquer l'utilisateur,
            // le token d'API restant la vraie barrière d'accès aux données.
            return false;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            AvailabilityResult availability = await _biometric.CheckAvailabilityAsync();
            return availability.IsAvailable;
        }
        catch
        {
            // Capteur absent, permission refusée, ou plateforme non supportée : on considère
            // simplement que la biométrie n'est pas disponible plutôt que de planter l'appli.
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        try
        {
            AuthenticationRequest request = new AuthenticationRequest("Authentification requise", reason)
            {
                CancelTitle = "Annuler"
            };

            AuthenticationResult result = await _biometric.AuthenticateAsync(request);
            return result.IsSuccessful;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TryEnableAsync()
    {
        bool confirmed = await AuthenticateAsync(
            "Confirmez votre identité pour activer le verrouillage biométrique.");

        if (confirmed)
        {
            try
            {
                await SecureStorage.Default.SetAsync(StorageKey, "true");
                _cachedEnabled = true;
            }
            catch
            {
                // Impossible de persister (Keystore indisponible) : on n'active pas
                // le verrou pour rester cohérent entre l'UI et l'état réel.
                return false;
            }
        }

        return confirmed;
    }

    public void Disable()
    {
        SecureStorage.Default.Remove(StorageKey);
        Preferences.Default.Remove(LegacyPreferenceKey);
        _cachedEnabled = false;
    }
}
