using InvoiceShelf.ViewModels.Estimates;

namespace InvoiceShelf.Views.Estimates;

public partial class EstimateDetailPage : ContentPage
{
    public EstimateDetailPage(EstimateDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
