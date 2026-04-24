using Anchor.Mobile.Services;

namespace Anchor.Mobile.Views;

public partial class PairingPage : ContentPage
{
    private readonly DesktopConnection _conn;
    public PairingPage(DesktopConnection conn)
    {
        InitializeComponent();
        _conn = conn;
    }

    private async void OnPair(object? sender, EventArgs e)
    {
        var host = HostEntry.Text?.Trim();
        if (string.IsNullOrEmpty(host)) { Status.Text = "Enter the desktop host."; return; }
        try
        {
            await _conn.ConnectAsync(host);
            Status.Text = "Connected. Overlay challenges will arrive here.";
        }
        catch (Exception ex) { Status.Text = $"Failed: {ex.Message}"; }
    }
}
