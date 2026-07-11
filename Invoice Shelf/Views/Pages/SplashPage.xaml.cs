using InvoiceShelf.ViewModels.Pages;

namespace InvoiceShelf.Views.Pages;

public partial class SplashPage : ContentPage
{
	public SplashPage(SplashViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		Loaded += viewModel.Loaded;
    }
}