using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Services;
using InvoiceShelf.Views.Auth;

namespace InvoiceShelf.ViewModels.Auth;

public partial class EndpointViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public EndpointViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    [ObservableProperty] private string? _baseUrl;
    [ObservableProperty] private Color   _entryBorderColor = Colors.Transparent;
    [ObservableProperty] private bool    _errorIsVisible;
    [ObservableProperty] private bool    _isBusy;

    [RelayCommand]
    private async Task CheckServer()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            EntryBorderColor = Colors.Red;
            ErrorIsVisible   = true;
            return;
        }

        if (!IsValidUrl(BaseUrl))
        {
            await Shell.Current.DisplayAlertAsync(
                "URL invalide",
                "Le format de l'URL est incorrect. Exemple : https://bills.exemple.fr",
                "OK");
            return;
        }

        EntryBorderColor = Colors.Transparent;
        ErrorIsVisible   = false;
        BaseUrl          = BaseUrl.TrimEnd('/');

        IsBusy = true;
        try
        {
            _apiService.SetBaseUrl(BaseUrl);
            ConnectionTestResult test = await _apiService.TestConnection();

            if (!test.Success)
            {
                await Shell.Current.DisplayAlertAsync("Connexion échouée", DescribeError(test), "OK");
                return;
            }

            await SecureStorage.Default.SetAsync("baseUrl", BaseUrl);
            await Shell.Current.GoToAsync($"//{nameof(CredentialPage)}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Traduit le diagnostic technique en message clair pour l'utilisateur.</summary>
    private static string DescribeError(ConnectionTestResult test) => test.StatusCode switch
    {
        0   => "Impossible de joindre le serveur.\n\n" +
               "Vérifiez :\n" +
               "• que l'URL est correcte et accessible depuis cet appareil ;\n" +
               "• votre connexion internet (Wi-Fi/données) ;\n" +
               "• que le serveur n'est pas bloqué par un VPN, un pare-feu ou un problème IPv6.\n\n" +
               $"Détail : {test.Error}",

        401 => "Le serveur a répondu mais refuse l'accès (401). " +
               "L'endpoint de version semble protégé sur cette installation.",

        403 => "Accès interdit par le serveur (403). " +
               "Un pare-feu ou un reverse-proxy bloque peut-être la requête.",

        404 => "Serveur joignable mais l'API InvoiceShelf est introuvable (404).\n" +
               "Vérifiez que l'URL pointe bien vers la racine de votre installation " +
               "(sans /admin ni autre sous-chemin).",

        >= 500 => $"Le serveur a renvoyé une erreur interne ({test.StatusCode}).\n" +
                  "Réessayez plus tard ou vérifiez les logs du serveur.",

        408 => "Le serveur a mis trop de temps à répondre (timeout).\n" +
               "Il est peut-être surchargé ou en cours de démarrage. Réessayez.",

        _   => $"Réponse inattendue du serveur (code {test.StatusCode}).\n{test.Error}"
    };

    private static bool IsValidUrl(string url)
        => Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
