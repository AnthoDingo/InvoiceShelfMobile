using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Resources.Strings;
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
                AppStrings.Get("Endpoint_InvalidUrlTitle"),
                AppStrings.Get("Endpoint_InvalidUrlMessage"),
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
                await Shell.Current.DisplayAlertAsync(AppStrings.Get("Endpoint_ConnectionFailedTitle"), DescribeError(test), "OK");
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
        0      => string.Format(AppStrings.Get("Endpoint_ErrorUnreachable"), test.Error),
        401    => AppStrings.Get("Endpoint_Error401"),
        403    => AppStrings.Get("Endpoint_Error403"),
        404    => AppStrings.Get("Endpoint_Error404"),
        >= 500 => string.Format(AppStrings.Get("Endpoint_Error5xxFormat"), test.StatusCode),
        408    => AppStrings.Get("Endpoint_Error408"),
        _      => string.Format(AppStrings.Get("Endpoint_ErrorUnexpectedFormat"), test.StatusCode, test.Error)
    };

    private static bool IsValidUrl(string url)
        => Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
