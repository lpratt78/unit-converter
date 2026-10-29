using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace UnitConverter.App;

public partial class App : Application
{
    private const string InstanceName = "UnitConverter.SingleInstance.{6C2F0B1E-7E43-4C55-9F0B-2A61C4D0A9E1}";

    private Mutex? _instanceMutex;
    private EventWaitHandle? _showSignal;
    private TrayIcon? _tray;
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Only one widget at a time. A second launch just brings the first one forward.
        _instanceMutex = new Mutex(initiallyOwned: true, InstanceName + ".Mutex", out var isFirst);
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, InstanceName + ".Show");
        if (!isFirst)
        {
            _showSignal.Set();
            Shutdown();
            return;
        }

        var settings = Settings.Load();
        _window = new MainWindow(settings);
        _tray = new TrayIcon(_window);
        _window.HiddenToTray += () => _tray.ShowHintOnce(settings);

        var waitThread = new Thread(() =>
        {
            while (_showSignal.WaitOne())
                Dispatcher.BeginInvoke(() => _window.ShowWidget());
        }) { IsBackground = true, Name = "SingleInstanceListener" };
        waitThread.Start();

        if (e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase))
            _window.EnsureHandle();
        else
            _window.ShowWidget();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _window?.ExitApp();
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
