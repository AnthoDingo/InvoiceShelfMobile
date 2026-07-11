using InvoiceShelf.ViewModels.Invoices;

namespace InvoiceShelf.Views.Invoices;

public partial class InvoiceDetailPage : ContentPage
{
    private readonly InvoiceDetailViewModel _viewModel;

    public InvoiceDetailPage(InvoiceDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Rafraîchit le solde dû/statut après un retour depuis RecordPaymentPage
        // (ou tout autre changement pouvant affecter la facture pendant l'absence).
        _ = _viewModel.RefreshAsync();
    }
}
