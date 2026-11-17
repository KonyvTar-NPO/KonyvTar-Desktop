using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KönyvTár.Views.Auth;

public partial class ForgotCodePage : UserControl
{
    public event System.Action? BackRequested;
    public event System.Action? ContinueRequested;

    public ForgotCodePage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => BackRequested?.Invoke();
        ContinueButton.Click += (_, _) => ContinueRequested?.Invoke();
    }

    public void SetEmail(string email) => EmailText.Text = email;
}
