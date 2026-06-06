using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Maipiyon;

/// <summary>ホットキー設定ダイアログ（キー入力キャプチャ）</summary>
public class HotkeyDialog : Window
{
    public uint ResultMods { get; private set; }
    public uint ResultVk   { get; private set; }

    private readonly TextBlock _preview;
    private uint _pendingMods;
    private uint _pendingVk;

    private const uint MOD_ALT   = 0x0001;
    private const uint MOD_CTRL  = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    public HotkeyDialog(uint currentMods, uint currentVk)
    {
        ResultMods = currentMods;
        ResultVk   = currentVk;

        Title           = "ホットキー設定";
        Width           = 300;
        Height          = 200;
        ResizeMode      = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x1D, 0x2E));
        Foreground      = Brushes.White;
        ShowInTaskbar   = false;

        var panel = new StackPanel { Margin = new Thickness(20) };

        panel.Children.Add(new TextBlock
        {
            Text       = "新しいショートカットを押してください",
            Foreground = new SolidColorBrush(Color.FromRgb(0xA9, 0xB1, 0xD6)),
            FontSize   = 12,
            Margin     = new Thickness(0, 0, 0, 12),
        });

        _preview = new TextBlock
        {
            Text       = HotkeyHelper.Format(currentMods, currentVk),
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xBF, 0xA5)),
            FontSize   = 18,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin     = new Thickness(0, 0, 0, 20),
        };
        panel.Children.Add(_preview);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

        var okBtn = MakeBtn("保存", Color.FromRgb(0x00, 0xBF, 0xA5));
        okBtn.Click += (_, _) =>
        {
            if (_pendingVk != 0)
            {
                ResultMods = _pendingMods;
                ResultVk   = _pendingVk;
            }
            DialogResult = true;
        };

        var cancelBtn = MakeBtn("キャンセル", Color.FromRgb(0x56, 0x5F, 0x89));
        cancelBtn.Margin = new Thickness(10, 0, 0, 0);
        cancelBtn.Click += (_, _) => DialogResult = false;

        btnRow.Children.Add(okBtn);
        btnRow.Children.Add(cancelBtn);
        panel.Children.Add(btnRow);

        Content = panel;
        Focusable = true;
        KeyDown += OnKeyDown;
        KeyUp   += OnKeyUp;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // 修飾キーのみの入力は無視
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
            return;

        uint mods = 0;
        if (Keyboard.IsKeyDown(Key.LeftCtrl)  || Keyboard.IsKeyDown(Key.RightCtrl))  mods |= MOD_CTRL;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) mods |= MOD_SHIFT;
        if (Keyboard.IsKeyDown(Key.LeftAlt)   || Keyboard.IsKeyDown(Key.RightAlt))   mods |= MOD_ALT;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        _pendingMods = mods;
        _pendingVk   = vk;
        _preview.Text = HotkeyHelper.Format(mods, vk);
    }

    private void OnKeyUp(object sender, KeyEventArgs e) => e.Handled = true;

    private static Button MakeBtn(string text, Color bg)
    {
        var btn = new Button
        {
            Content    = text,
            Width      = 80,
            Height     = 32,
            Foreground = Brushes.White,
            FontSize   = 12,
            Cursor     = Cursors.Hand,
            Background = new SolidColorBrush(bg),
            BorderThickness = new Thickness(0),
        };
        return btn;
    }
}

/// <summary>ホットキーのフォーマット・ユーティリティ</summary>
public static class HotkeyHelper
{
    private const uint MOD_ALT   = 0x0001;
    private const uint MOD_CTRL  = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    public static string Format(uint mods, uint vk)
    {
        var parts = new List<string>();
        if ((mods & MOD_CTRL)  != 0) parts.Add("Ctrl");
        if ((mods & MOD_SHIFT) != 0) parts.Add("Shift");
        if ((mods & MOD_ALT)   != 0) parts.Add("Alt");

        var key = KeyInterop.KeyFromVirtualKey((int)vk);
        parts.Add(key.ToString());

        return string.Join(" + ", parts);
    }
}
