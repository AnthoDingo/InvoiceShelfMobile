using InvoiceShelf.ViewModels.Pages;

namespace InvoiceShelf.Views.Pages;

public partial class PaymentsPage : ContentPage
{
    public PaymentsPage(PaymentsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += viewModel.Loaded;
    }
}
