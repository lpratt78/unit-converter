using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using UnitConverter.Core;

namespace UnitConverter.App;

public partial class MainWindow : Window
{
    private readonly Settings _settings;
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private GlobalHotKey? _hotKey;

    /// <summary>The box the user last typed in; the other box shows the converted value.</summary>
    private TextBox _source;

    /// <summary>Set while the code is changing controls, so change handlers don't recurse.</summary>
    private bool _updating;
    private bool _exiting;
    private bool _positioned;

    public event Action? HiddenToTray;

    public MainWindow(Settings settings)
    {
        InitializeComponent();
        _settings = settings;
        _source = FromBox;
        _statusTimer.Tick += (_, _) => { _statusTimer.Stop(); UpdateStatus(); };

        SetTopmost(settings.Topmost);
        RestorePosition();

        foreach (var box in new[] { FromBox, ToBox })
            box.PreviewMouseLeftButtonDown += ValueBox_PreviewMouseLeftButtonDown;

        CategoryBox.ItemsSource = Catalog.All;
        CategoryBox.SelectedItem = Catalog.Find(settings.Category) ?? Catalog.Length;
        FromBox.Text = "1";

        Loaded += (_, _) => PlaceDefaultIfNeeded();
    }

    private Category Category => (Category)CategoryBox.SelectedItem;
    private Unit? FromUnit => FromUnitBox.SelectedItem as Unit;
    private Unit? ToUnit => ToUnitBox.SelectedItem as Unit;
    private TextBox Target => ReferenceEquals(_source, FromBox) ? ToBox : FromBox;

    // ---- Showing, hiding and exiting -------------------------------------------------

    public void EnsureHandle() => new WindowInteropHelper(this).EnsureHandle();

    public void ShowWidget()
    {
        Show();
        Activate();
        _source.Focus();
        _source.SelectAll();
    }

    public void HideWidget()
    {
        SavePosition();
        Hide();
        HiddenToTray?.Invoke();
    }

    public void ToggleWidget()
    {
        if (IsVisible)
            HideWidget();
        else
            ShowWidget();
    }

    public void SetTopmost(bool topmost)
    {
        Topmost = topmost;
        PinButton.IsChecked = topmost;
        _settings.Topmost = topmost;
        _settings.Save();
    }

    public void ExitApp()
    {
        _exiting = true;
        SavePosition();
        Close();
        Application.Current.Shutdown();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hotKey = GlobalHotKey.TryRegister(this, GlobalHotKey.Modifiers.Control | GlobalHotKey.Modifiers.Alt,
            Key.U, ToggleWidget);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Alt+F4 hides the widget like the hide button; only "Exit" in the tray menu quits.
        if (!_exiting && !Dispatcher.HasShutdownStarted)
        {
            e.Cancel = true;
            HideWidget();
            return;
        }
        _hotKey?.Dispose();
        base.OnClosing(e);
    }

    // ---- Position --------------------------------------------------------------------

    private void RestorePosition()
    {
        if (_settings.Left is not { } left || _settings.Top is not { } top)
            return;

        // Ignore a saved position that is no longer on any screen (e.g. a monitor was unplugged).
        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        if (!screen.Contains(new Point(left + 60, top + 30)))
            return;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = left;
        Top = top;
        _positioned = true;
    }

    private void PlaceDefaultIfNeeded()
    {
        if (_positioned)
            return;
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth;
        Top = area.Bottom - ActualHeight;
        _positioned = true;
    }

    private void SavePosition()
    {
        if (!_positioned)
            return;
        _settings.Left = Left;
        _settings.Top = Top;
        _settings.Save();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        // Clicks on inputs and buttons are handled by those controls; anything else drags.
        // Clicks inside an open drop-down belong to its own popup window and must not drag.
        base.OnMouseLeftButtonDown(e);
        if (e.ButtonState != MouseButtonState.Pressed
            || e.OriginalSource is not Visual visual
            || PresentationSource.FromVisual(visual)?.RootVisual != this)
            return;
        DragMove();
        SavePosition();
    }

    // ---- Conversion ------------------------------------------------------------------

    private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var category = Category;
        var from = category.DefaultFrom;
        var to = category.DefaultTo;
        if (_settings.Units.TryGetValue(category.Name, out var saved) && saved.Length == 2)
        {
            from = category.Find(saved[0]) ?? from;
            to = category.Find(saved[1]) ?? to;
        }

        _updating = true;
        FromUnitBox.ItemsSource = category.Units;
        ToUnitBox.ItemsSource = category.Units;
        FromUnitBox.SelectedItem = from;
        ToUnitBox.SelectedItem = to;
        _updating = false;

        _settings.Category = category.Name;
        _settings.Save();
        Recalculate();
    }

    private void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || FromUnit is null || ToUnit is null)
            return;
        RememberUnits();
        Recalculate();
    }

    private void FromBox_TextChanged(object sender, TextChangedEventArgs e) => OnUserTyped(FromBox);

    private void ToBox_TextChanged(object sender, TextChangedEventArgs e) => OnUserTyped(ToBox);

    private void OnUserTyped(TextBox box)
    {
        if (_updating)
            return;
        _source = box;
        Recalculate();
    }

    private void Recalculate()
    {
        if (FromUnit is not { } from || ToUnit is not { } to)
            return;

        var culture = CultureInfo.CurrentCulture;
        var target = Target;
        string result;

        if (string.IsNullOrWhiteSpace(_source.Text))
        {
            _source.Tag = null;
            result = "";
        }
        else if (Converter.TryParse(_source.Text, culture, out var value))
        {
            _source.Tag = null;
            var converted = ReferenceEquals(_source, FromBox)
                ? Converter.Convert(value, from, to)
                : Converter.Convert(value, to, from);
            result = Converter.Format(converted, culture);
        }
        else
        {
            _source.Tag = "Invalid";
            result = "";
        }

        target.Tag = null;
        _updating = true;
        target.Text = result;
        _updating = false;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_statusTimer.IsEnabled || FromUnit is not { } from || ToUnit is not { } to)
            return;

        if (_source.Tag is "Invalid")
        {
            StatusText.Text = "Enter a number";
            return;
        }

        var culture = CultureInfo.CurrentCulture;
        var rate = Converter.Format(Converter.Convert(1, from, to), culture);
        StatusText.Text = $"1 {from.Symbol} = {rate} {to.Symbol}";
    }

    private void RememberUnits()
    {
        _settings.Units[Category.Name] = [FromUnit!.Symbol, ToUnit!.Symbol];
        _settings.Save();
    }

    private void Swap()
    {
        if (FromUnit is not { } from || ToUnit is not { } to)
            return;

        // Swapping both units and both values keeps the pair consistent, so nothing needs recomputing.
        _updating = true;
        FromUnitBox.SelectedItem = to;
        ToUnitBox.SelectedItem = from;
        (FromBox.Text, ToBox.Text) = (ToBox.Text, FromBox.Text);
        (FromBox.Tag, ToBox.Tag) = (ToBox.Tag, FromBox.Tag);
        _updating = false;

        _source = Target;
        RememberUnits();
        UpdateStatus();
    }

    private void CopyResult()
    {
        var target = Target;
        var unit = ReferenceEquals(target, FromBox) ? FromUnit : ToUnit;
        if (string.IsNullOrEmpty(target.Text) || unit is null)
            return;

        try
        {
            Clipboard.SetText(target.Text);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Another app is holding the clipboard open; the user can just click again.
            return;
        }

        StatusText.Text = $"Copied {target.Text} {unit.Symbol}";
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    // ---- Input handling --------------------------------------------------------------

    private void ValueBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        ((TextBox)sender).SelectAll();

    private void ValueBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // First click selects the whole value so typing replaces it; later clicks place the caret.
        var box = (TextBox)sender;
        if (!box.IsKeyboardFocusWithin)
        {
            box.Focus();
            e.Handled = true;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        if (e.Key == Key.Escape && !CategoryBox.IsDropDownOpen && !FromUnitBox.IsDropDownOpen && !ToUnitBox.IsDropDownOpen)
        {
            HideWidget();
            e.Handled = true;
        }
        else if (ctrl && !shift && e.Key is >= Key.D1 and <= Key.D9 && e.Key - Key.D1 < Catalog.All.Count)
        {
            CategoryBox.SelectedItem = Catalog.All[e.Key - Key.D1];
            e.Handled = true;
        }
        else if (ctrl && !shift && e.Key == Key.R)
        {
            Swap();
            e.Handled = true;
        }
        else if (ctrl && shift && e.Key == Key.C)
        {
            CopyResult();
            e.Handled = true;
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e) => SetTopmost(PinButton.IsChecked == true);

    private void HideButton_Click(object sender, RoutedEventArgs e) => HideWidget();

    private void SwapButton_Click(object sender, RoutedEventArgs e) => Swap();

    private void CopyButton_Click(object sender, RoutedEventArgs e) => CopyResult();
}
