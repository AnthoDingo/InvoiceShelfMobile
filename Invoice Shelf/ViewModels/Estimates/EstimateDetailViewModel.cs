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

    public string EstimateIdParam
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
                Task.Run(() => LoadEstimate(id));
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    [NotifyPropertyChangedFor(nameof(HasNotes))]
    private Estimate? _estimate;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasItems => Estimate?.Items?.Count > 0;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Estimate?.Notes);

    private async Task LoadEstimate(int id)
    {
        IsLoading = true;
        try   { Estimate = await _apiService.GetEstimate(id); }
        catch (Exception ex) { Console.WriteLine($"Erreur chargement devis {id} : {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task GoBack() => await Shell.Current.GoToAsync("..");
}
