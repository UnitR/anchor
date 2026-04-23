using Anchor.iOS.Views;

namespace Anchor.iOS;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(PairingPage), typeof(PairingPage));
        Routing.RegisterRoute(nameof(RegistrationWizardPage), typeof(RegistrationWizardPage));
        Routing.RegisterRoute(nameof(ChallengeCameraPage), typeof(ChallengeCameraPage));
    }
}
