namespace Anchor.iOS.Views;

public partial class HomePage : ContentPage
{
    public HomePage() { InitializeComponent(); }
    private async void OnPair(object? s, EventArgs e) => await Shell.Current.GoToAsync(nameof(PairingPage));
    private async void OnRegister(object? s, EventArgs e) => await Shell.Current.GoToAsync(nameof(RegistrationWizardPage));
}
