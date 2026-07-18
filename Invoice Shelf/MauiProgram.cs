using CommunityToolkit.Maui;
using Maui.Biometric;
using InvoiceShelf.Services;
using InvoiceShelf.Views.Pages;
using InvoiceShelf.Views.Auth;
using InvoiceShelf.Views.Invoices;
using InvoiceShelf.ViewModels.Pages;
using InvoiceShelf.ViewModels.Auth;
using InvoiceShelf.ViewModels.Invoices;
using InvoiceShelf.ViewModels.Estimates;
using InvoiceShelf.Views.Estimates;
using InvoiceShelf.ViewModels.Expenses;
using InvoiceShelf.Views.Expenses;
using MauiIcons.FontAwesome;
using MauiIcons.Material;

#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace InvoiceShelf
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMaterialMauiIcons()
                .UseFontAwesomeMauiIcons()
                .UseBiometricAuthentication()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf",    "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf",   "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIconsRegular");
                });
#if ALPHA || BETA || DEBUG
            builder
                .UseSentry(options =>
                {
                    // The DSN is the only required setting.
                    options.Dsn = "https://2bf09f435b640f74030a3358649e6bf8@o4511373115981824.ingest.de.sentry.io/4511373118210128";

                    // Use debug mode if you want to see what the SDK is doing.
                    // Debug messages are written to stdout with Console.Writeline,
                    // and are viewable in your IDE's debug console or with 'adb logcat', etc.
                    // This option is not recommended when deploying your application.
#if DEBUG
                    options.Debug = true;
#endif

                    // Other Sentry options can be set here.
                    options.CaptureFailedRequests = true;
                });
#endif

            var services = builder.Services;

            // Service central
            services.AddSingleton<ApiService>();
            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<IBiometricLockService, BiometricLockService>();

            // Auth
            services.AddSingleton<SplashPage>();
            services.AddSingleton<SplashViewModel>();
            services.AddSingleton<EndpointPage>();
            services.AddSingleton<EndpointViewModel>();
            services.AddSingleton<CredentialPage>();
            services.AddSingleton<CredentialViewModel>();
            services.AddTransient<LockPage>();
            services.AddTransient<LockViewModel>();

            // Onglets principaux
            services.AddSingleton<DashboardPage>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<InvoicesPage>();
            services.AddSingleton<InvoicesViewModel>();
            services.AddSingleton<EstimatesPage>();
            services.AddSingleton<EstimatesViewModel>();
            services.AddSingleton<CustomersPage>();
            services.AddSingleton<CustomersViewModel>();
            services.AddSingleton<PaymentsPage>();
            services.AddSingleton<PaymentsViewModel>();
            services.AddSingleton<ExpensesPage>();
            services.AddSingleton<ExpensesViewModel>();
            services.AddSingleton<MorePage>();
            services.AddSingleton<MoreViewModel>();

            // Pages détail (transient = nouvelle instance à chaque navigation)
            services.AddTransient<InvoiceDetailPage>();
            services.AddTransient<InvoiceDetailViewModel>();
            services.AddTransient<RecordPaymentPage>();
            services.AddTransient<RecordPaymentViewModel>();
            services.AddTransient<CreateInvoicePage>();
            services.AddTransient<CreateInvoiceViewModel>();
            services.AddTransient<EstimateDetailPage>();
            services.AddTransient<EstimateDetailViewModel>();
            services.AddTransient<CreateEstimatePage>();
            services.AddTransient<CreateEstimateViewModel>();
            services.AddTransient<CustomerDetailPage>();
            services.AddTransient<CustomerDetailViewModel>();
            services.AddTransient<CreateExpensePage>();
            services.AddTransient<CreateExpenseViewModel>();

            return builder.Build();
        }
    }
}
