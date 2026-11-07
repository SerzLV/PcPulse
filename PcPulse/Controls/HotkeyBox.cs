using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PcPulse.Core.Settings;
using PcPulse.Platform;

namespace PcPulse.Controls;

/// <summary>
/// Click, then press a combination. Backspace/Delete clears it, Esc cancels.
/// A bare key is accepted only for F1–F24; anything else needs a modifier.
/// <see cref="Hotkey"/> binds two-way.
/// </summary>
public sealed class HotkeyBox : Border
{
    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey), typeof(Hotkey), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(Hotkey.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((HotkeyBox)d).Render()));

    private static readonly Brush Idle = Frozen(Color.FromRgb(0x26, 0x30, 0x4A));
    private static readonly Brush Active = Frozen(Color.FromRgb(0xB4, 0x7C, 0xFF));

    private readonly TextBlock _text = new()
    {
        FontSize = 13,
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    public HotkeyBox()
    {
        Focusable = true;
        Cursor = Cursors.Hand;
        Height = 34;
        MinWidth = 180;
        CornerRadius = new CornerRadius(8);
        BorderThickness = new Thickness(1);
        BorderBrush = Idle;
        Background = Frozen(Color.FromRgb(0x16, 0x1D, 0x2C));
        _text.FontFamily = (FontFamily)Application.Current.FindResource("MonoFont");
        Child = _text;
        Render();
    }

    public Hotkey Hotkey
    {
        get => (Hotkey)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        Keyboard.Focus(this);
        e.Handled = true;
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        BorderBrush = Active;
        RenderPending(ModifierKeys.None);
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        BorderBrush = Idle;
        Render();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var mods = Keyboard.Modifiers;

        if (key == Key.Escape && mods == ModifierKeys.None)
        {
            Keyboard.ClearFocus();
            return;
        }
        if (key is Key.Back or Key.Delete && mods == ModifierKeys.None)
        {
            Commit(Hotkey.None);
            return;
        }
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift
            or Key.RightShift or Key.LWin or Key.RWin)
        {
            RenderPending(mods);
            return;
        }
        if (mods == ModifierKeys.None && key is not (>= Key.F1 and <= Key.F24))
        {
            _text.Text = "добавьте Ctrl, Alt или Shift";
            return;
        }
        Commit(KeyMapping.ToHotkey(mods, key));
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        if (IsKeyboardFocused) RenderPending(Keyboard.Modifiers);
    }

    private void Commit(Hotkey hotkey)
    {
        // SetCurrentValue keeps the binding and pushes the value to the view model.
        SetCurrentValue(HotkeyProperty, hotkey);
        Keyboard.ClearFocus();
        Render();
    }

    private void RenderPending(ModifierKeys mods)
    {
        _text.Foreground = Active;
        _text.Text = mods == ModifierKeys.None
            ? "нажмите комбинацию…"
            : string.Join(" + ", Hotkey.ModifierNames(KeyMapping.ToHotkeyModifiers(mods))) + " + …";
    }

    private void Render()
    {
        var hotkey = Hotkey;
        _text.Foreground = (Brush)Application.Current.FindResource(hotkey.IsEmpty ? "MutedBrush" : "TextBrush");
        _text.Text = hotkey.ToString();
    }

    private static Brush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
