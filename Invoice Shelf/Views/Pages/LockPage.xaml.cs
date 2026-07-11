using InvoiceShelf.ViewModels.Pages;

namespace InvoiceShelf.Views.Pages;

public partial class LockPage : ContentPage
{
    private readonly LockViewModel _viewModel;

    public LockPage(LockViewModel viewModel)
    {
        InitializeComponent();
        _viewModel   = viewModel;
        BindingContext = viewModel;
        Loaded += _viewModel.Loaded;
    }
}
