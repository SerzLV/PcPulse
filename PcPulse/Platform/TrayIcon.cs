using System.Windows;
using PcPulse.Core.Settings;
using PcPulse.Shell;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace PcPulse.Platform;

/// <summary>WinForms NotifyIcon with a dark context menu.</summary>
public sealed class TrayIcon : ITrayIcon
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _overlayItem;
    private bool _hintShown;

    public event Action? OpenRequested;
    public event Action? OverlayToggleRequested;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayIcon()
    {
        var menu = new Forms.ContextMenuStrip
        {
            Renderer = new Forms.ToolStripProfessionalRenderer(new DarkColors()) { RoundedEdges = false },
            ForeColor = Drawing.Color.FromArgb(0xE6, 0xEA, 0xF2),
            Font = new Drawing.Font("Segoe UI", 10f),
            ShowImageMargin = false,
            ShowCheckMargin = true,
        };
        var open = new Forms.ToolStripMenuItem("Открыть PC Pulse", null, (_, _) => OpenRequested?.Invoke())
        {
            Font = new Drawing.Font("Segoe UI", 10f, Drawing.FontStyle.Bold),
        };
        _overlayItem = new Forms.ToolStripMenuItem("Оверлей", null, (_, _) => OverlayToggleRequested?.Invoke());
        menu.Items.Add(open);
        menu.Items.Add(_overlayItem);
        menu.Items.Add(new Forms.ToolStripMenuItem("Настройки…", null, (_, _) => SettingsRequested?.Invoke()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Выход", null, (_, _) => ExitRequested?.Invoke()));

        var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/PcPulse.ico")).Stream;
        _icon = new Forms.NotifyIcon
        {
            Icon = new Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize),
            Text = "PC Pulse",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) OpenRequested?.Invoke();
        };
    }

    public void SetOverlayState(bool isOn, Hotkey hotkey)
    {
        _overlayItem.Checked = isOn;
        _overlayItem.ShortcutKeyDisplayString = hotkey.IsEmpty ? null : hotkey.ToString().Replace(" ", "");
    }

    // Windows caps the hover text at 127 characters.
    public void SetTooltip(string text) => _icon.Text = text.Length > 127 ? text[..127] : text;

    public void ShowHiddenHintOnce()
    {
        if (_hintShown) return;
        _hintShown = true;
        _icon.ShowBalloonTip(3000, "PC Pulse работает в трее",
            "Кликните по иконке, чтобы открыть окно. Мониторинг и оверлей продолжают работать.", Forms.ToolTipIcon.None);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    private sealed class DarkColors : Forms.ProfessionalColorTable
    {
        private static readonly Drawing.Color Bg = Drawing.Color.FromArgb(0x12, 0x18, 0x26);
        private static readonly Drawing.Color Hover = Drawing.Color.FromArgb(0x1E, 0x26, 0x36);
        private static readonly Drawing.Color Line = Drawing.Color.FromArgb(0x26, 0x30, 0x4A);

        public override Drawing.Color ToolStripDropDownBackground => Bg;
        public override Drawing.Color ImageMarginGradientBegin => Bg;
        public override Drawing.Color ImageMarginGradientMiddle => Bg;
        public override Drawing.Color ImageMarginGradientEnd => Bg;
        public override Drawing.Color MenuBorder => Line;
        public override Drawing.Color MenuItemBorder => Hover;
        public override Drawing.Color MenuItemSelected => Hover;
        public override Drawing.Color SeparatorDark => Line;
        public override Drawing.Color SeparatorLight => Bg;
        public override Drawing.Color CheckBackground => Hover;
        public override Drawing.Color CheckSelectedBackground => Line;
        public override Drawing.Color CheckPressedBackground => Line;
        public override Drawing.Color ButtonSelectedBorder => Line;
    }
}
