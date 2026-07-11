using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Estimates;

[QueryProperty(nameof(EstimateIdParam), "estimateId")]
public partial class EstimateDetailViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public EstimateDetailViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private int _currentId;

    public string EstimateIdParam
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
            {
                _currentId = id;
                Task.Run(() => LoadEstimate(id));
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    [NotifyPropertyChangedFor(nameof(HasNotes))]
    [NotifyPropertyChangedFor(nameof(IsDraft))]
    private Estimate? _estimate;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasItems => Estimate?.Items?.Count > 0;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Estimate?.Notes);

    // Le bouton "Modifier" n'est proposé que sur un devis encore en brouillon :
    // au-delà (envoyé, accepté...), on considère qu'il ne doit plus être modifié
    // depuis ce formulaire simplifié.
    public bool IsDraft => Estimate?.Status == "DRAFT";

    private async Task LoadEstimate(int id)
    {
        IsLoading = true;
        try   { Estimate = await _apiService.GetEstimate(id); }
        catch (Exception ex) { Console.WriteLine($"Erreur chargement devis {id} : {ex.Message}"); }
        finally { IsLoading = false; }
    }

    /// <summary>Recharge le devis courant, ex. au retour de la page de modification.</summary>
    public async Task RefreshAsync()
    {
        if (_currentId > 0)
            await LoadEstimate(_currentId);
    }

    [RelayCommand]
    private async Task GoBack() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task EditEstimate()
    {
        if (Estimate is null) return;
        await Shell.Current.GoToAsync($"CreateEstimatePage?estimateId={Estimate.Id}");
    }
}
