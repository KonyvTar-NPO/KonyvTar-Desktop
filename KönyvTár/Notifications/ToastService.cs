using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;

namespace KönyvTár.Notifications;

public sealed class ToastService
{
    private const int MaxVisibleMessages = 4;

    private static readonly TimeSpan InfoDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan SuccessDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan WarningDuration = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan ErrorDuration = TimeSpan.FromSeconds(8);

    private DispatcherTimer? _timer;
    private DateTimeOffset _lastTick;

    private ToastService()
    {
    }

    public static ToastService Instance { get; } = new();

    public ObservableCollection<ToastMessage> Messages { get; } = [];

    public void ShowInfo(string message) => Show(message, ToastKind.Info);

    public void ShowSuccess(string message) => Show(message, ToastKind.Success);

    public void ShowWarning(string message) => Show(message, ToastKind.Warning);

    public void ShowError(string message) => Show(message, ToastKind.Error);

    public void Show(
        string message,
        ToastKind kind = ToastKind.Info,
        TimeSpan? duration = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Show(message, kind, duration));
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
            return;

        var toast = new ToastMessage(
            message.Trim(),
            kind,
            duration ?? GetDefaultDuration(kind));

        while (Messages.Count >= MaxVisibleMessages)
            Messages.RemoveAt(Messages.Count - 1);

        Messages.Insert(0, toast);
        EnsureTimer();
    }

    public void Close(ToastMessage message)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Close(message));
            return;
        }

        Messages.Remove(message);
        StopTimerIfEmpty();
    }

    private void EnsureTimer()
    {
        if (_timer is null)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            _timer.Tick += OnTimerTick;
        }

        if (_timer.IsEnabled)
            return;

        _lastTick = DateTimeOffset.UtcNow;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastTick;
        _lastTick = now;

        foreach (var toast in Messages.ToList())
        {
            toast.Advance(elapsed);
            if (toast.IsExpired)
                Messages.Remove(toast);
        }

        StopTimerIfEmpty();
    }

    private void StopTimerIfEmpty()
    {
        if (Messages.Count == 0)
            _timer?.Stop();
    }

    private static TimeSpan GetDefaultDuration(ToastKind kind) => kind switch
    {
        ToastKind.Success => SuccessDuration,
        ToastKind.Warning => WarningDuration,
        ToastKind.Error => ErrorDuration,
        _ => InfoDuration,
    };
}
