using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;
using InvoiceShelf.Views.Auth;

namespace InvoiceShelf.ViewModels.Pages
{
    public partial class MoreViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly IBiometricLockService _biometricLockService;
        private readonly ICacheService _cacheService;

        // Empêche le toggle de redéclencher la logique d'activation/désactivation
        // quand on met à jour IsBiometricLockEnabled depuis le code (chargement initial,
        // ou remise à false après un refus d'authentification).
        private bool _suppressBiometricToggleHandling;

        public MoreViewModel(ApiService apiService, IBiometricLockService biometricLockService, ICacheService cacheService)
        {
            _apiService = apiService;
            _biometricLockService = biometricLockService;
            _cacheService = cacheService;
        }

        internal async void Loaded(object? sender, EventArgs e)
        {
            await LoadProfile();
            await LoadBiometricSettingsAsync();
            await LoadCacheSizeAsync();
        }

        [ObservableProperty]
        private UserProfile? _profile;

        [ObservableProperty]
        private string _serverUrl = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        /// <summary>Vrai si l'appareil dispose d'un capteur biométrique configuré.</summary>
        [ObservableProperty]
        private bool _isBiometricAvailable;

        [ObservableProperty]
        private bool _isBiometricLockEnabled;

        [ObservableProperty]
        private string _cacheSizeDisplay = string.Empty;

        [ObservableProperty]
        private bool _isClearingCache;

        private async Task LoadCacheSizeAsync()
        {
            try
            {
                long bytes = await _cacheService.GetTotalSizeAsync();
                CacheSizeDisplay = FormatSize(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cache size: {ex.Message}");
                CacheSizeDisplay = "—";
            }
        }

        private static string FormatSize(long bytes)
        {
            const double kb = 1024;
            const double mb = kb * 1024;

            if (bytes >= mb)
                return $"{bytes / mb:0.0} Mo";
            if (bytes >= kb)
                return $"{bytes / kb:0.0} Ko";
            return $"{bytes} o";
        }

        [RelayCommand]
        private async Task ClearCache()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Vider le cache",
                "Les données mises en cache (factures, devis, clients, etc.) seront rechargées depuis le serveur au prochain affichage.",
                "Vider",
                "Annuler");

            if (!confirm) return;

            IsClearingCache = true;
            try
            {
                await _cacheService.ClearAllAsync();
                await LoadCacheSizeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cache: {ex.Message}");
            }
            finally
            {
                IsClearingCache = false;
            }
        }

        private async Task LoadBiometricSettingsAsync()
        {
            IsBiometricAvailable = await _biometricLockService.IsAvailableAsync();

            _suppressBiometricToggleHandling = true;
            IsBiometricLockEnabled = IsBiometricAvailable && _biometricLockService.IsEnabled;
            _suppressBiometricToggleHandling = false;
        }

        partial void OnIsBiometricLockEnabledChanged(bool value)
        {
            if (_suppressBiometricToggleHandling) return;
            _ = HandleBiometricToggleAsync(value);
        }

        private async Task HandleBiometricToggleAsync(bool requestedValue)
        {
            if (requestedValue)
            {
                bool confirmed = await _biometricLockService.TryEnableAsync();
                if (!confirmed)
                {
                    _suppressBiometricToggleHandling = true;
                    IsBiometricLockEnabled = false;
                    _suppressBiometricToggleHandling = false;

                    await Shell.Current.DisplayAlertAsync(
                        "Verrouillage biométrique",
                        "L'authentification n'a pas pu être confirmée. Le verrouillage n'a pas été activé.",
                        "OK");
                }
            }
            else
            {
                _biometricLockService.Disable();
            }
        }

        private async Task LoadProfile()
        {
            IsLoading = true;
            try
            {
                ServerUrl = _apiService.GetBaseUrl();
                Profile = await _apiService.GetMe();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profile: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Déconnexion",
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion",
                "Annuler");

            if (!confirm) return;

            try
            {
                await _apiService.Logout();
            }
            catch { /* ignore errors on logout */ }
            finally
            {
                SecureStorage.Default.Remove("token");
                _biometricLockService.Disable();
                await Shell.Current.GoToAsync($"//{nameof(CredentialPage)}");
            }
        }

        [RelayCommand]
        private async Task ChangeServer()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Changer de serveur",
                "Cela vous déconnectera et effacera les informations de connexion actuelles.",
                "Continuer",
                "Annuler");

            if (!confirm) return;

            SecureStorage.Default.Remove("token");
            SecureStorage.Default.Remove("baseUrl");
            _biometricLockService.Disable();
            await Shell.Current.GoToAsync($"//{nameof(EndpointPage)}");
        }
    }
}
