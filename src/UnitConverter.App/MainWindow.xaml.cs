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
    private const string Invalid = "Invalid";

    private readonly Settings _settings;
    private readonly DispatcherTimer _flashTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private GlobalHotKey? _hotKey;

    /// <summary>The value box the user last typed in; its partner shows the result.</summary>
    private TextBox _source;

    /// <summary>Set while the code is changing controls, so change handlers don't recurse.</summary>
    private bool _updating;
    private bool _exiting;
    private bool _positioned;
    private string _status = "";

    public event Action? HiddenToTray;

    public MainWindow(Settings settings)
    {
        InitializeComponent();
        _settings = settings;
        _source = FromBox;
        _flashTimer.Tick += (_, _) => { _flashTimer.Stop(); StatusText.Text = _status; };

        SetTopmost(settings.Topmost);
        RestorePosition();

        foreach (var box in new[] { FromBox, ToBox, DensityBox, InMinBox, InMaxBox, OutMinBox, OutMaxBox, ScaleInBox, ScaleOutBox })
            box.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;

        // Parameters, filled before any mode is shown so the first calculation sees them.
        _updating = true;
        var culture = CultureInfo.CurrentCulture;
        MaterialBox.ItemsSource = Material.Common;
        DensityUnitBox.ItemsSource = Catalog.Density.Units;
        var densityUnit = Catalog.Density.Find(settings.WeightFactorUnit) ?? Catalog.Density.DefaultTo;
        DensityUnitBox.SelectedItem = densityUnit;
        DensityBox.Text = Converter.Format(densityUnit.FromBase(settings.WeightFactorKgPerL), culture);
        ScalePresetBox.ItemsSource = ScalePreset.Common;
        InMinBox.Text = Converter.Format(settings.ScaleInMin, culture);
        InMaxBox.Text = Converter.Format(settings.ScaleInMax, culture);
        OutMinBox.Text = Converter.Format(settings.ScaleOutMin, culture);
        OutMaxBox.Text = Converter.Format(settings.ScaleOutMax, culture);
        ScaleInBox.Text = Converter.Format((settings.ScaleInMin + settings.ScaleInMax) / 2, culture);
        FromBox.Text = "1";
        _updating = false;

        ModeBox.ItemsSource = Mode.All;
        ModeBox.SelectedItem = Mode.FindByName(settings.Category) ?? Mode.FindByName("Length");

        Loaded += (_, _) => PlaceDefaultIfNeeded();
    }

    private Mode Mode => (Mode)ModeBox.SelectedItem;
    private Unit? FromUnit => FromUnitBox.SelectedItem as Unit;
    private Unit? ToUnit => ToUnitBox.SelectedItem as Unit;
    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private TextBox Partner(TextBox box) =>
        box == FromBox ? ToBox : box == ToBox ? FromBox : box == ScaleInBox ? ScaleOutBox : ScaleInBox;

    private bool IsForward(TextBox box) => box == FromBox || box == ScaleInBox;

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

    /// <summary>Saves the window position along with everything else in settings.</summary>
    private void SavePosition()
    {
        if (_positioned)
        {
            _settings.Left = Left;
            _settings.Top = Top;
        }
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

    // ---- Mode and parameter changes --------------------------------------------------

    private void ModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var mode = Mode;
        UnitPanel.Visibility = mode.Kind == ModeKind.Scaling ? Visibility.Collapsed : Visibility.Visible;
        DensityPanel.Visibility = mode.Kind == ModeKind.MassVolume ? Visibility.Visible : Visibility.Collapsed;
        ScalePanel.Visibility = mode.Kind == ModeKind.Scaling ? Visibility.Visible : Visibility.Collapsed;

        if (mode.Kind == ModeKind.Scaling)
        {
            if (_source != ScaleInBox && _source != ScaleOutBox)
                _source = ScaleInBox;
        }
        else
        {
            if (_source != FromBox && _source != ToBox)
                _source = FromBox;

            var from = mode.DefaultFrom;
            var to = mode.DefaultTo;
            if (_settings.Units.TryGetValue(mode.Name, out var saved) && saved.Length == 2)
            {
                from = mode.FindUnit(saved[0]) ?? from;
                to = mode.FindUnit(saved[1]) ?? to;
            }

            _updating = true;
            FromUnitBox.ItemsSource = mode.Units;
            ToUnitBox.ItemsSource = mode.Units;
            FromUnitBox.SelectedItem = from;
            ToUnitBox.SelectedItem = to;
            _updating = false;
        }

        _settings.Category = mode.Name;
        Recalculate();

        if (IsVisible)
            Dispatcher.BeginInvoke(() => { _source.Focus(); _source.SelectAll(); }, DispatcherPriority.Input);
    }

    private void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || FromUnit is null || ToUnit is null)
            return;
        RememberUnits();
        Recalculate();
    }

    private void ValueBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
            return;
        _source = (TextBox)sender;
        Recalculate();
    }

    private void DensityBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
            return;
        // A typed density no longer matches the chosen material.
        _updating = true;
        MaterialBox.SelectedItem = null;
        _updating = false;
        Recalculate();
    }

    private void DensityUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || DensityUnitBox.SelectedItem is not Unit unit)
            return;

        // Keep the same physical density, shown in the new unit.
        _settings.WeightFactorUnit = unit.Symbol;
        SetText(DensityBox, Converter.Format(unit.FromBase(_settings.WeightFactorKgPerL), Culture));
        Recalculate();
    }

    private void MaterialBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || MaterialBox.SelectedItem is not Material material || DensityUnitBox.SelectedItem is not Unit unit)
            return;
        SetText(DensityBox, Converter.Format(unit.FromBase(material.KgPerL), Culture));
        Recalculate();
    }

    private void RangeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
            return;
        _updating = true;
        ScalePresetBox.SelectedItem = null;
        _updating = false;
        Recalculate();
    }

    private void ScalePresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || ScalePresetBox.SelectedItem is not ScalePreset preset)
            return;
        SetText(InMinBox, Converter.Format(preset.Min, Culture));
        SetText(InMaxBox, Converter.Format(preset.Max, Culture));
        Recalculate();
    }

    // ---- Calculation -----------------------------------------------------------------

    private void Recalculate()
    {
        if (ModeBox.SelectedItem is not Mode mode)
            return;
        if (mode.Kind == ModeKind.Scaling)
            RecalculateScale();
        else
            RecalculateUnits(mode);
    }

    private void RecalculateUnits(Mode mode)
    {
        if (FromUnit is not { } from || ToUnit is not { } to)
            return;
        ClearInvalid(DensityBox, FromBox, ToBox);

        var density = 1.0;
        if (mode.Kind == ModeKind.MassVolume)
        {
            var densityUnit = (Unit)DensityUnitBox.SelectedItem;
            if (!TryParse(DensityBox, out var shown) || !(densityUnit.ToBase(shown) > 0))
            {
                Fail(DensityBox, "Multiplier must be a positive number");
                return;
            }
            density = densityUnit.ToBase(shown);
            _settings.WeightFactorKgPerL = density;
        }

        double Convert(double value, Unit a, Unit b) => mode.Kind == ModeKind.MassVolume
            ? MassVolume.Convert(value, a, b, density)
            : Converter.Convert(value, a, b);

        var target = Partner(_source);
        if (!TryCompute(target, x => IsForward(_source)
                ? Convert(x, from, to)
                : Convert(x, to, from)))
            return;

        var rate = Converter.Format(Convert(1, from, to), Culture);
        SetStatus($"1 {from.Symbol} = {rate} {to.Symbol}");
    }

    private void RecalculateScale()
    {
        ClearInvalid(InMinBox, InMaxBox, OutMinBox, OutMaxBox, ScaleInBox, ScaleOutBox);

        foreach (var box in new[] { InMinBox, InMaxBox, OutMinBox, OutMaxBox })
        {
            if (!TryParse(box, out _))
            {
                Fail(box, "Enter the input and output min and max");
                return;
            }
        }
        TryParse(InMinBox, out var inMin);
        TryParse(InMaxBox, out var inMax);
        TryParse(OutMinBox, out var outMin);
        TryParse(OutMaxBox, out var outMax);
        var scale = new LinearScale(inMin, inMax, outMin, outMax);

        (_settings.ScaleInMin, _settings.ScaleInMax, _settings.ScaleOutMin, _settings.ScaleOutMax) = (inMin, inMax, outMin, outMax);

        if (!scale.CanScale)
        {
            Fail(InMaxBox, "Input min and max can't be equal");
            return;
        }
        if (!IsForward(_source) && !scale.CanUnscale)
        {
            Fail(OutMaxBox, "Output min and max can't be equal");
            return;
        }

        if (!TryCompute(Partner(_source), v => IsForward(_source) ? scale.Scale(v) : scale.Unscale(v)))
            return;
        SyncSlider(scale);

        // Warn when the input is outside its range, since the result is then extrapolated.
        if (TryParse(ScaleInBox, out var input) && !scale.IsInputInRange(input))
        {
            SetStatus($"⚠ Input 1 outside {Converter.Format(inMin, Culture)} – {Converter.Format(inMax, Culture)}, extrapolated");
            return;
        }

        var offset = scale.Offset;
        var sign = offset < 0 ? "−" : "+";
        var offsetText = offset == 0 ? "" : $" {sign} {Converter.Format(Math.Abs(offset), Culture)}";
        SetStatus($"Input 2 = {Converter.Format(scale.Slope, Culture)} × Input 1{offsetText}");
    }

    /// <summary>Moves the slider to Input 1 and labels it with the range ends and percent of span.</summary>
    private void SyncSlider(LinearScale scale)
    {
        var lo = Math.Min(scale.InMin, scale.InMax);
        var hi = Math.Max(scale.InMin, scale.InMax);
        SliderMinText.Text = Converter.Format(lo, Culture);
        SliderMaxText.Text = Converter.Format(hi, Culture);
        if (!TryParse(ScaleInBox, out var input))
            return;

        _updating = true;
        ScaleSlider.Minimum = lo;
        ScaleSlider.Maximum = hi;
        ScaleSlider.SmallChange = (hi - lo) / 100;
        ScaleSlider.LargeChange = (hi - lo) / 10;
        ScaleSlider.Value = Math.Clamp(input, lo, hi);
        _updating = false;

        var percent = (input - scale.InMin) / (scale.InMax - scale.InMin) * 100;
        SliderPercentText.Text = $"{Converter.Format(Math.Round(percent, 1), Culture)} %";
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var range = ScaleSlider.Maximum - ScaleSlider.Minimum;
        if (_updating || !(range > 0))
            return;

        // Round to about a thousandth of the span, so 4–20 gives 12.34 and 0–27648 gives whole counts.
        var decimals = Math.Clamp(3 - (int)Math.Floor(Math.Log10(range)), 0, 10);
        SetText(ScaleInBox, Converter.Format(Math.Round(e.NewValue, decimals), Culture));
        _source = ScaleInBox;
        Recalculate();
    }

    /// <summary>
    /// Parses the source box and writes the result of <paramref name="compute"/> into <paramref name="target"/>.
    /// Returns false when there is nothing valid to compute (and the status already says why).
    /// </summary>
    private bool TryCompute(TextBox target, Func<double, double> compute)
    {
        if (string.IsNullOrWhiteSpace(_source.Text))
        {
            SetText(target, "");
            SetStatus("");
            return false;
        }
        if (!TryParse(_source, out var value))
        {
            Fail(_source, "Enter a number");
            return false;
        }
        SetText(target, Converter.Format(compute(value), Culture));
        return true;
    }

    private static bool TryParse(TextBox box, out double value, bool allowEmpty = false, double emptyValue = 0)
    {
        if (allowEmpty && string.IsNullOrWhiteSpace(box.Text))
        {
            value = emptyValue;
            return true;
        }
        return Converter.TryParse(box.Text, Culture, out value);
    }

    private void Fail(TextBox box, string message)
    {
        box.Tag = Invalid;
        SetText(Partner(_source), "");
        SetStatus(message);
    }

    private static void ClearInvalid(params TextBox[] boxes)
    {
        foreach (var box in boxes)
            box.Tag = null;
    }

    private void SetText(TextBox box, string text)
    {
        _updating = true;
        box.Text = text;
        _updating = false;
    }

    private void SetStatus(string text)
    {
        _status = text;
        if (!_flashTimer.IsEnabled)
            StatusText.Text = text;
    }

    private void RememberUnits()
    {
        _settings.Units[Mode.Name] = [FromUnit!.Symbol, ToUnit!.Symbol];
    }

    // ---- Actions ---------------------------------------------------------------------

    private void Swap()
    {
        if (Mode.Kind == ModeKind.Scaling)
        {
            _updating = true;
            ScalePresetBox.SelectedItem = null;
            (InMinBox.Text, OutMinBox.Text) = (OutMinBox.Text, InMinBox.Text);
            (InMaxBox.Text, OutMaxBox.Text) = (OutMaxBox.Text, InMaxBox.Text);
            (ScaleInBox.Text, ScaleOutBox.Text) = (ScaleOutBox.Text, ScaleInBox.Text);
            _updating = false;
        }
        else
        {
            if (FromUnit is not { } from || ToUnit is not { } to)
                return;
            _updating = true;
            FromUnitBox.SelectedItem = to;
            ToUnitBox.SelectedItem = from;
            (FromBox.Text, ToBox.Text) = (ToBox.Text, FromBox.Text);
            _updating = false;
            RememberUnits();
        }

        // The value the user typed moved to the other box; keep it as the source.
        _source = Partner(_source);
        Recalculate();
    }

    /// <summary>Copies the status line text (the rate or scaling formula) to the clipboard.</summary>
    private void CopyResult()
    {
        if (string.IsNullOrEmpty(_status))
            return;

        try
        {
            Clipboard.SetText(_status);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Another app is holding the clipboard open; the user can just click again.
            return;
        }

        StatusText.Text = "Copied ✓";
        _flashTimer.Stop();
        _flashTimer.Start();
    }

    // ---- Input handling --------------------------------------------------------------

    private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        ((TextBox)sender).SelectAll();

    private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
        var anyDropDownOpen = new[] { ModeBox, FromUnitBox, ToUnitBox, DensityUnitBox, MaterialBox, ScalePresetBox }
            .Any(c => c.IsDropDownOpen);

        if (e.Key == Key.Escape && !anyDropDownOpen)
        {
            HideWidget();
            e.Handled = true;
        }
        else if (ctrl && !shift && e.Key is >= Key.D1 and <= Key.D9 && e.Key - Key.D1 < Mode.All.Count)
        {
            ModeBox.SelectedItem = Mode.All[e.Key - Key.D1];
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
