using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;

namespace KönyvTár.Notifications;

public sealed class ToastMessage : ObservableObject
{
    private static readonly IBrush InfoAccent = new SolidColorBrush(Color.Parse("#3B82F6"));
    private static readonly IBrush InfoBackground = new SolidColorBrush(Color.Parse("#123B82F6"));
    private static readonly IBrush InfoBorder = new SolidColorBrush(Color.Parse("#663B82F6"));

    private static readonly IBrush SuccessAccent = new SolidColorBrush(Color.Parse("#22C55E"));
    private static readonly IBrush SuccessBackground = new SolidColorBrush(Color.Parse("#1222C55E"));
    private static readonly IBrush SuccessBorder = new SolidColorBrush(Color.Parse("#6622C55E"));

    private static readonly IBrush WarningAccent = new SolidColorBrush(Color.Parse("#F59E0B"));
    private static readonly IBrush WarningBackground = new SolidColorBrush(Color.Parse("#12F59E0B"));
    private static readonly IBrush WarningBorder = new SolidColorBrush(Color.Parse("#66F59E0B"));

    private static readonly IBrush ErrorAccent = new SolidColorBrush(Color.Parse("#EF4444"));
    private static readonly IBrush ErrorBackground = new SolidColorBrush(Color.Parse("#12EF4444"));
    private static readonly IBrush ErrorBorder = new SolidColorBrush(Color.Parse("#66EF4444"));

    private readonly TimeSpan _duration;
    private TimeSpan _remaining;
    private double _progress = 1;

    public ToastMessage(string message, ToastKind kind, TimeSpan duration)
    {
        Message = message;
        Kind = kind;
        _duration = duration > TimeSpan.Zero ? duration : TimeSpan.FromSeconds(4);
        _remaining = _duration;
    }

    public string Message { get; }

    public ToastKind Kind { get; }

    public string Title => Kind switch
    {
        ToastKind.Success => "Siker",
        ToastKind.Warning => "Figyelmeztetés",
        ToastKind.Error => "Hiba",
        _ => "Információ",
    };

    public MaterialIconKind Icon => Kind switch
    {
        ToastKind.Success => MaterialIconKind.CheckCircle,
        ToastKind.Warning => MaterialIconKind.AlertCircle,
        ToastKind.Error => MaterialIconKind.CloseCircle,
        _ => MaterialIconKind.InformationCircle,
    };

    public IBrush AccentBrush => Kind switch
    {
        ToastKind.Success => SuccessAccent,
        ToastKind.Warning => WarningAccent,
        ToastKind.Error => ErrorAccent,
        _ => InfoAccent,
    };

    public IBrush BackgroundBrush => Kind switch
    {
        ToastKind.Success => SuccessBackground,
        ToastKind.Warning => WarningBackground,
        ToastKind.Error => ErrorBackground,
        _ => InfoBackground,
    };

    public IBrush BorderBrush => Kind switch
    {
        ToastKind.Success => SuccessBorder,
        ToastKind.Warning => WarningBorder,
        ToastKind.Error => ErrorBorder,
        _ => InfoBorder,
    };

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public bool IsExpired => _remaining <= TimeSpan.Zero;

    public void Advance(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
            return;

        _remaining -= elapsed;
        if (_remaining < TimeSpan.Zero)
            _remaining = TimeSpan.Zero;

        Progress = Math.Clamp(_remaining.TotalMilliseconds / _duration.TotalMilliseconds, 0, 1);
    }
}
