using InvoiceShelf.Services;
using InvoiceShelf.Views.Pages;

namespace InvoiceShelf
{
    public partial class App : Application
    {
        private readonly IBiometricLockService _biometricLockService;

        // Vrai si l'appli a été mise en arrière-plan au moins une fois depuis son lancement ;
        // on ne veut pas re-verrouiller sur le tout premier OnResume (il n'y en a pas au
        // démarrage à froid, mais on reste défensif en cas d'appel inattendu de la plateforme).
        private bool _wasSentToBackground;

        public App(IBiometricLockService biometricLockService)
        {
            InitializeComponent();

            _biometricLockService = biometricLockService;

            MainPage = new AppShell();
        }

        protected override void OnSleep() => _wasSentToBackground = true;

        protected override void OnResume()
        {
            if (!_wasSentToBackground) return;

            // On ne verrouille que si l'option est activée et qu'une session est en cours
            // (sinon on est déjà sur SplashPage/EndpointPage/CredentialPage, où un verrou
            // n'aurait pas de sens).
            if (_biometricLockService.IsEnabled && Shell.Current is not null)
                _ = Shell.Current.GoToAsync($"//LockPage");
        }
    }
}
