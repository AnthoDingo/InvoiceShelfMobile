using InvoiceShelf.ViewModels.Estimates;

namespace InvoiceShelf.Views.Estimates;

public partial class EstimateDetailPage : ContentPage
{
    private readonly EstimateDetailViewModel _viewModel;

    public EstimateDetailPage(EstimateDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Rafraîchit le devis après un retour depuis CreateEstimatePage en mode
        // édition (ou tout autre changement pouvant l'affecter pendant l'absence).
        _ = _viewModel.RefreshAsync();
    }
}
