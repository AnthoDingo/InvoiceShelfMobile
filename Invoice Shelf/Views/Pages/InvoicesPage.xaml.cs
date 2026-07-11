using InvoiceShelf.ViewModels.Pages;

namespace InvoiceShelf.Views.Pages;

public partial class InvoicesPage : ContentPage
{
    public InvoicesViewModel ViewModel { get; }

    public InvoicesPage(InvoicesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // Loaded se déclenche une seule fois sur un Singleton, contrairement à NavigatedTo
        // qui se déclencherait à chaque retour sur l'onglet et provoquerait des appels API dupliqués.
        Loaded += viewModel.Loaded;
    }
}