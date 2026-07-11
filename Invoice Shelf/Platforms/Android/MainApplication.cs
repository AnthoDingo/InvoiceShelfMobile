using Android.App;
using Android.Runtime;
using Microsoft.Maui;

namespace InvoiceShelf
{
    [Application(
        Theme = "@style/Maui.SplashTheme.Custom",
#if DEBUG
        Debuggable = true,
#endif
        UsesCleartextTraffic = true)]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() 
        {
            try
            {
                return MauiProgram.CreateMauiApp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MauiApp creation failed: {ex}");
                throw;
            }
        }
    }
}
