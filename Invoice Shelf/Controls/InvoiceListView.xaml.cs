using InvoiceShelf.Models.Admin;
using System.Windows.Input;

namespace InvoiceShelf.Controls;

public partial class InvoiceListView : ContentView
{
    public InvoiceListView() => InitializeComponent();

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<Invoice>), typeof(InvoiceListView), null,
            propertyChanged: (b, o, n) => ((InvoiceListView)b).OnItemsSourceChanged());

    public IEnumerable<Invoice> ItemsSource
    {
        get => (IEnumerable<Invoice>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty IsRefreshingProperty =
        BindableProperty.Create(nameof(IsRefreshing), typeof(bool), typeof(InvoiceListView), false);

    public bool IsRefreshing
    {
        get => (bool)GetValue(IsRefreshingProperty);
        set => SetValue(IsRefreshingProperty, value);
    }

    public static readonly BindableProperty RefreshCommandProperty =
        BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(InvoiceListView));

    public ICommand RefreshCommand
    {
        get => (ICommand)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public static readonly BindableProperty SelectionCommandProperty =
        BindableProperty.Create(nameof(SelectionCommand), typeof(ICommand), typeof(InvoiceListView));

    public ICommand SelectionCommand
    {
        get => (ICommand)GetValue(SelectionCommandProperty);
        set => SetValue(SelectionCommandProperty, value);
    }

    public static readonly BindableProperty EmptyMessageProperty =
        BindableProperty.Create(nameof(EmptyMessage), typeof(string), typeof(InvoiceListView), string.Empty);

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    public static readonly BindableProperty EmptySubMessageProperty =
        BindableProperty.Create(nameof(EmptySubMessage), typeof(string), typeof(InvoiceListView), string.Empty);

    public string EmptySubMessage
    {
        get => (string)GetValue(EmptySubMessageProperty);
        set => SetValue(EmptySubMessageProperty, value);
    }

    public static readonly BindableProperty IsEmptyProperty =
        BindableProperty.Create(nameof(IsEmpty), typeof(bool), typeof(InvoiceListView), true);

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        private set => SetValue(IsEmptyProperty, value);
    }

    private void OnItemsSourceChanged()
    {
        IsEmpty = ItemsSource == null || !ItemsSource.Any();
    }
}
