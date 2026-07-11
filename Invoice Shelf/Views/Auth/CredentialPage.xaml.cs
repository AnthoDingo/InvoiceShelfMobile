using InvoiceShelf.ViewModels.Auth;

namespace InvoiceShelf.Views.Auth;

public partial class CredentialPage : ContentPage
{
	public CredentialPage(CredentialViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
}