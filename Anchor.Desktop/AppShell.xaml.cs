using Anchor.Desktop.Views;

namespace Anchor.Desktop;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(InteroceptionPromptPage), typeof(InteroceptionPromptPage));
        Routing.RegisterRoute(nameof(IntentionCheckpointPage), typeof(IntentionCheckpointPage));
        Routing.RegisterRoute(nameof(AnchorChallengePage), typeof(AnchorChallengePage));
    }
}
