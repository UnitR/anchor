using Anchor.Mobile.Views;

namespace Anchor.Mobile;

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
