using InvoiceShelf.ViewModels.Invoices;

namespace InvoiceShelf.Views.Invoices;

public partial class CreateInvoicePage : ContentPage
{
    public CreateInvoicePage(CreateInvoiceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
