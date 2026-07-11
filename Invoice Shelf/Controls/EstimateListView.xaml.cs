using InvoiceShelf.Models.Admin;
using System.Windows.Input;

namespace InvoiceShelf.Controls;

public partial class EstimateListView : ContentView
{
    public EstimateListView() => InitializeComponent();

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<Estimate>), typeof(EstimateListView), null,
            propertyChanged: (b, o, n) => ((EstimateListView)b).OnItemsSourceChanged());

    public IEnumerable<Estimate> ItemsSource
    {
        get => (IEnumerable<Estimate>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty IsRefreshingProperty =
        BindableProperty.Create(nameof(IsRefreshing), typeof(bool), typeof(EstimateListView), false);

    public bool IsRefreshing
    {
        get => (bool)GetValue(IsRefreshingProperty);
        set => SetValue(IsRefreshingProperty, value);
    }

    public static readonly BindableProperty RefreshCommandProperty =
        BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(EstimateListView));

    public ICommand RefreshCommand
    {
        get => (ICommand)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public static readonly BindableProperty SelectionCommandProperty =
        BindableProperty.Create(nameof(SelectionCommand), typeof(ICommand), typeof(EstimateListView));

    public ICommand SelectionCommand
    {
        get => (ICommand)GetValue(SelectionCommandProperty);
        set => SetValue(SelectionCommandProperty, value);
    }

    public static readonly BindableProperty EmptyMessageProperty =
        BindableProperty.Create(nameof(EmptyMessage), typeof(string), typeof(EstimateListView), string.Empty);

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    public static readonly BindableProperty EmptySubMessageProperty =
        BindableProperty.Create(nameof(EmptySubMessage), typeof(string), typeof(EstimateListView), string.Empty);

    public string EmptySubMessage
    {
        get => (string)GetValue(EmptySubMessageProperty);
        set => SetValue(EmptySubMessageProperty, value);
    }

    public static readonly BindableProperty IsEmptyProperty =
        BindableProperty.Create(nameof(IsEmpty), typeof(bool), typeof(EstimateListView), true);

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
