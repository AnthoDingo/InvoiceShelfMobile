using InvoiceShelf.ViewModels.Estimates;

namespace InvoiceShelf.Views.Estimates;

public partial class CreateEstimatePage : ContentPage
{
    public CreateEstimatePage(CreateEstimateViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
