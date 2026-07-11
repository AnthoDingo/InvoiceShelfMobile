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
    bool IsEnabled { get; }

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
    private const string PreferenceKey = "biometric_lock_enabled";

    private readonly IBiometricAuthentication _biometric;

    public BiometricLockService(IBiometricAuthentication biometric)
    {
        _biometric = biometric ?? throw new ArgumentNullException(nameof(biometric));
    }

    public bool IsEnabled => Preferences.Default.Get(PreferenceKey, false);

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var availability = await _biometric.CheckAvailabilityAsync();
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
            var request = new AuthenticationRequest("Authentification requise", reason)
            {
                CancelTitle = "Annuler"
            };

            var result = await _biometric.AuthenticateAsync(request);
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
            Preferences.Default.Set(PreferenceKey, true);

        return confirmed;
    }

    public void Disable() => Preferences.Default.Set(PreferenceKey, false);
}
