namespace InvoiceShelf;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Routes de détail (hors onglets)
        Routing.RegisterRoute("InvoiceDetailPage",  typeof(InvoiceShelf.Views.Invoices.InvoiceDetailPage));
        Routing.RegisterRoute("EstimateDetailPage", typeof(InvoiceShelf.Views.Estimates.EstimateDetailPage));
        Routing.RegisterRoute("CustomerDetailPage",  typeof(InvoiceShelf.Views.Pages.CustomerDetailPage));
        Routing.RegisterRoute("RecordPaymentPage",   typeof(InvoiceShelf.Views.Invoices.RecordPaymentPage));
        Routing.RegisterRoute("CreateInvoicePage",   typeof(InvoiceShelf.Views.Invoices.CreateInvoicePage));
        Routing.RegisterRoute("CreateEstimatePage",  typeof(InvoiceShelf.Views.Estimates.CreateEstimatePage));
    }
}
