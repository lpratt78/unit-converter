using System;
using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace UnitConverter.App;

/// <summary>The notification-area icon: click to show or hide, right-click for options.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly MainWindow _window;
    private readonly Forms.ToolStripMenuItem _topmostItem;
    private readonly Forms.ToolStripMenuItem _startupItem;

    public TrayIcon(MainWindow window)
    {
        _window = window;

        var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico")).Stream;

        _topmostItem = new Forms.ToolStripMenuItem("Always on top", null, (_, _) => _window.SetTopmost(!_window.Topmost));
        _startupItem = new Forms.ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleStartup());

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(new Forms.ToolStripMenuItem("Show / hide", null, (_, _) => _window.ToggleWidget())
        {
            ShortcutKeyDisplayString = "Ctrl+Alt+U",
        });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_topmostItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _window.ExitApp());
        menu.Opening += (_, _) =>
        {
            _topmostItem.Checked = _window.Topmost;
            _startupItem.Checked = StartupRegistration.IsEnabled;
        };

        _icon = new Forms.NotifyIcon
        {
            Icon = new Icon(iconStream, Forms.SystemInformation.SmallIconSize),
            Text = "Unit Converter",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                _window.ToggleWidget();
        };
    }

    /// <summary>The first time the widget is hidden, explain where it went.</summary>
    public void ShowHintOnce(Settings settings)
    {
        if (settings.TrayHintShown)
            return;
        settings.TrayHintShown = true;
        settings.Save();
        _icon.ShowBalloonTip(4000, "Unit Converter is still running",
            "Click the tray icon or press Ctrl+Alt+U to bring it back.", Forms.ToolTipIcon.None);
    }

    private static void ToggleStartup()
    {
        try
        {
            StartupRegistration.SetEnabled(!StartupRegistration.IsEnabled);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't change the startup setting.\n\n{ex.Message}", "Unit Converter");
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
