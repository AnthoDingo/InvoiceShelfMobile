namespace InvoiceShelf.Controls;

public partial class CustomEntry : ContentView
{

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CustomEntry), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(CustomEntry), string.Empty);

    public Color BorderStrokeColor => IsError ? Colors.Red : Colors.Transparent;

    public static readonly BindableProperty IsErrorProperty =
        BindableProperty.Create(nameof(IsError), typeof(bool), typeof(CustomEntry), false,
            propertyChanged: (bindable, oldVal, newVal) =>
            {
                var control = (CustomEntry)bindable;
                control.OnPropertyChanged(nameof(BorderStrokeColor));
            });

    public string Text
    {   
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool IsError
    {
        get => (bool)GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }



    public CustomEntry()
	{
		InitializeComponent();
	}
}