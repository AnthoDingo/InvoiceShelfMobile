using InvoiceShelf.ViewModels.Invoices;

namespace InvoiceShelf.Views.Invoices;

public partial class RecordPaymentPage : ContentPage
{
    public RecordPaymentPage(RecordPaymentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
