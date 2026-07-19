using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Services;
using InvoiceShelf.Views.Auth;

namespace InvoiceShelf.ViewModels.Pages;

public partial class LockViewModel : ObservableObject
{
    private readonly IBiometricLockService _biometricLockService;
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    public LockViewModel(IBiometricLockService biometricLockService, ApiService apiService, ICacheService cacheService)
    {
        _biometricLockService = biometricLockService;
        _apiService            = apiService;
        _cacheService          = cacheService;
    }

    // Se déclenche à chaque affichage de la page (contrairement aux autres pages de
    // l'app, celle-ci n'est volontairement pas un singleton : on veut redéclencher
    // le prompt biométrique à chaque fois que l'appli doit être déverrouillée).
    internal async void Loaded(object? sender, EventArgs e) => await AttemptUnlockAsync();

    [ObservableProperty]
    private bool _isAuthenticating;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [RelayCommand]
    private async Task AttemptUnlockAsync()
    {
        IsAuthenticating = true;
        ErrorMessage      = string.Empty;
        try
        {
            bool ok = await _biometricLockService.AuthenticateAsync("Déverrouillez l'application.");
            if (ok)
                await Shell.Current.GoToAsync($"//HomePage");
            else
                ErrorMessage = "Authentification impossible. Réessayez ou déconnectez-vous.";
        }
        finally
        {
            IsAuthenticating = false;
        }
    }

    [RelayCommand]
    private async Task Logout()
    {
        try { await _apiService.Logout(); }
        catch { /* on se déconnecte localement même si l'appel réseau échoue */ }
        finally
        {
            SecureStorage.Default.Remove("token");
            _biometricLockService.Disable();

            // Sécurité : purge le cache disque (factures, clients, paiements...) pour
            // qu'aucune donnée métier du compte déconnecté ne subsiste sur l'appareil.
            try { await _cacheService.ClearAllAsync(); }
            catch { /* la déconnexion doit aboutir même si la purge échoue */ }

            await Shell.Current.GoToAsync($"//{nameof(CredentialPage)}");
        }
    }
}
