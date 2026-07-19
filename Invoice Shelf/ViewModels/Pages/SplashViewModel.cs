using InvoiceShelf.Services;
using InvoiceShelf.Views.Auth;

namespace InvoiceShelf.ViewModels.Pages
{
    public class SplashViewModel
    {
        private ApiService _apiService;
        private readonly IBiometricLockService _biometricLockService;

        public SplashViewModel(ApiService apiService, IBiometricLockService biometricLockService)
        {
            _apiService = apiService;
            _biometricLockService = biometricLockService;
        }

        // Wrapper async void acceptable sur un event handler : on délègue immédiatement
        // vers une Task pour que les exceptions remontent via Sentry/AppDomain.
        public void Loaded(object? sender, EventArgs e)
            => _ = LoadedAsync();

        private async Task LoadedAsync()
        {
            string? baseUrl = await SecureStorage.GetAsync("baseUrl");

            if (string.IsNullOrEmpty(baseUrl))
            {
                await Shell.Current.GoToAsync($"//{nameof(EndpointPage)}");
                return;
            }
            else
            {
                _apiService.SetBaseUrl(baseUrl);
            }

            string? token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrEmpty(token))
            {
                await Shell.Current.GoToAsync($"//{nameof(CredentialPage)}");
                return;
            }

            if (!(await _apiService.CheckToken()))
            {
                await Shell.Current.GoToAsync($"//{nameof(CredentialPage)}");
                return;
            }

            try
            {
                if (await _biometricLockService.IsEnabledAsync())
                    await Shell.Current.GoToAsync($"//LockPage");
                else
                    await Shell.Current.GoToAsync($"//HomePage");
            }
            catch (Exception ex)
            {
                // Shell.Current est toujours disponible ici ; on évite MainPage qui peut être null.
                await Shell.Current.DisplayAlert("Error", $"Failed to navigate to HomePage: {ex.Message}", "OK");
            }
        }
    }
}
