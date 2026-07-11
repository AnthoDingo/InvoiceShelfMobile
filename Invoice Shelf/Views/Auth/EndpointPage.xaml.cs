using InvoiceShelf.ViewModels.Auth;

namespace InvoiceShelf.Views.Auth;

public partial class EndpointPage : ContentPage
{
	public EndpointPage(EndpointViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
}