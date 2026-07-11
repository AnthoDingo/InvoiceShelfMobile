using InvoiceShelf.ViewModels.Pages;

namespace InvoiceShelf.Views.Pages;

public partial class EstimatesPage : ContentPage
{
    public EstimatesViewModel ViewModel { get; }

    public EstimatesPage(EstimatesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        ViewModel = viewModel;
    }

    // OnAppearing (plutôt que Loaded, qui ne se déclenche qu'une fois sur un
    // Singleton) : se déclenche à chaque retour sur cette page, y compris après
    // la création d'un devis (retour depuis CreateEstimatePage). LoadAsync sert
    // le cache tant qu'il est frais (< 7 jours) et ne fait un appel réseau que
    // s'il a été invalidé (ex. après création) ou expiré : pas d'appels API en
    // double lors des simples changements d'onglet du Shell.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.Loaded(this, EventArgs.Empty);
    }
}
