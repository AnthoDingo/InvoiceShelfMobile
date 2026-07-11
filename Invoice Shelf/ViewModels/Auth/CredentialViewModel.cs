using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Services;
using InvoiceShelf.Views.Auth;

namespace InvoiceShelf.ViewModels.Auth
{
    public partial class CredentialViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        public CredentialViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [ObservableProperty]
        private string? _username;

        [ObservableProperty]
        private string? _password;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private Color _usernameBorderColor = Colors.Transparent;

        [ObservableProperty]
        private bool _usernameErrorVisible;

        [ObservableProperty]
        private Color _passwordBorderColor = Colors.Transparent;

        [ObservableProperty]
        private bool _passwordErrorVisible;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _errorMessageVisible;

        [RelayCommand]
        private async Task BackToEndpoint()
            => await Shell.Current.GoToAsync($"//{nameof(EndpointPage)}");

        [RelayCommand]
        private async Task ForgotPassword()
        {
            string? baseUrl = await SecureStorage.GetAsync("baseUrl");
            if (string.IsNullOrEmpty(baseUrl))
            {
                await Shell.Current.DisplayAlertAsync("Erreur", "Aucun serveur configuré.", "OK");
                return;
            }

            string resetUrl = $"{baseUrl}/password/reset";
            if (Uri.TryCreate(resetUrl, UriKind.Absolute, out Uri? uri))
                await Launcher.Default.OpenAsync(uri);
        }

        [RelayCommand]
        private async Task Login()
        {
            // Validation locale
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(Username))
            {
                UsernameBorderColor = Color.FromArgb("#EF4444");
                UsernameErrorVisible = true;
                hasError = true;
            }
            else
            {
                UsernameBorderColor = Colors.Transparent;
                UsernameErrorVisible = false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordBorderColor = Color.FromArgb("#EF4444");
                PasswordErrorVisible = true;
                hasError = true;
            }
            else
            {
                PasswordBorderColor = Colors.Transparent;
                PasswordErrorVisible = false;
            }

            if (hasError) return;

            ErrorMessageVisible = false;
            IsBusy = true;
            try
            {
                string? token = await _apiService.Login(Username!, Password!);
                if (string.IsNullOrEmpty(token))
                {
                    ErrorMessage = "Identifiants incorrects. Vérifiez votre email et mot de passe.";
                    ErrorMessageVisible = true;
                    return;
                }

                await SecureStorage.Default.SetAsync("token", token);
                await Shell.Current.GoToAsync("//HomePage");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur de connexion : {ex.Message}";
                ErrorMessageVisible = true;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
