using InvoiceShelf.ViewModels.Pages;

namespace InvoiceShelf.Views.Pages;

public partial class CustomerDetailPage : ContentPage
{
    public CustomerDetailPage(CustomerDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
