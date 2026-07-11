using InvoiceShelf.ViewModels.Pages;

namespace InvoiceShelf.Views.Pages;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += viewModel.Loaded;
    }
}
