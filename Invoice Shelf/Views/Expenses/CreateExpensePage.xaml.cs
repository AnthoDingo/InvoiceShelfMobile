using InvoiceShelf.ViewModels.Expenses;

namespace InvoiceShelf.Views.Expenses;

public partial class CreateExpensePage : ContentPage
{
    public CreateExpensePage(CreateExpenseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += viewModel.Loaded;
    }
}
