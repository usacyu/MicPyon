using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WinForms = System.Windows.Forms;

namespace Maipiyon;

public partial class App
{
    private WinForms.NotifyIcon?    _tray;
    private MainWindow?             _window;
    private System.Threading.Mutex? _mutex;
    private MicService?             _svc;
    private AppSettings             _settings = AppSettings.Load();

    private System.Drawing.Icon? _iconActive;
    private System.Drawing.Icon? _iconMuted;

    // ── スタートアップ登録（レジストリ）──────────────────────
    private const string RUN_KEY  = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string APP_NAME = "MicPyon";

    private static bool IsStartupEnabled()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RUN_KEY, false);
        return key?.GetValue(APP_NAME) is not null;
    }

    private static void SetStartupEnabled(bool enabled)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = System.Environment.ProcessPath
                          ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
                key.SetValue(APP_NAME, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(APP_NAME, false);
        }
    }

    // ── グローバルホットキー ──────────────────────────────────
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int  WM_HOTKEY   = 0x0312;
    private const int  HK_MUTE     = 9001;

    private HwndSource? _hotkeySource;
    private IntPtr      _hotkeyHwnd = IntPtr.Zero;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new System.Threading.Mutex(true, "Global\\MaipyonSingleInstance", out bool created);
        if (!created)
        {
            MessageBox.Show(Strings.AlreadyRunning,
                            "MicPyon", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _iconActive = MakeTrayIcon(false);
        _iconMuted  = MakeTrayIcon(true);

        _svc = new MicService();
        _svc.DevicesChanged += () => Dispatcher.BeginInvoke(RefreshTray);
        _svc.MuteChanged    += () => Dispatcher.BeginInvoke(RefreshTray);

        _window = new MainWindow(_svc, _settings);
        SetupTray();
        RegisterGlobalHotkey();
        // トレイが本拠地 — 起動時はウィンドウを表示しない
    }

    // ── トレイ初期設定 ───────────────────────────────────────
    private void SetupTray()
    {
        _tray = new WinForms.NotifyIcon
        {
            Icon    = _iconActive,
            Text    = "まいぴょん",
            Visible = true,
        };

        // 左クリック → ミュートトグル
        _tray.MouseClick += (_, args) =>
        {
            if (args.Button != WinForms.MouseButtons.Left) return;
            _svc!.SetMute(!_svc.IsMuted());
        };

        RefreshTray();
    }

    // ── トレイ状態を更新（アイコン・ツールチップ・メニュー）──
    private void RefreshTray()
    {
        if (_tray == null || _svc == null) return;

        var allMics  = _svc.GetAllMics().Select(x => x.Mic).ToList();
        var visible  = allMics.Where(m => !_settings.ExcludedIds.Contains(m.Id)).ToList();
        int excluded = allMics.Count - visible.Count;
        bool muted   = _svc.IsMuted();
        var def      = visible.FirstOrDefault(m => m.IsDefault)
                       ?? allMics.FirstOrDefault(m => m.IsDefault);

        // アイコン（赤=ミュート、緑=アクティブ）
        _tray.Icon = muted ? _iconMuted : _iconActive;

        // ツールチップ（64文字制限）
        var hkLabel = HotkeyHelper.Format(_settings.HotkeyMods, _settings.HotkeyVk);
        var tip = muted
            ? Strings.TrayMuted(hkLabel)
            : Strings.TrayActive(hkLabel, def?.Name ?? "");
        _tray.Text = tip.Length > 63 ? tip[..63] : tip;

        var oldMenu = _tray.ContextMenuStrip;
        var menu    = new WinForms.ContextMenuStrip();

        // ── マイク一覧（表示中のみ）──────────────────────────
        foreach (var mic in visible)
        {
            var m    = mic;
            var item = new WinForms.ToolStripMenuItem(m.Name)
            {
                Checked      = m.IsDefault,
                CheckOnClick = false,
            };
            if (m.IsDefault)
                item.Font = new System.Drawing.Font(
                    System.Drawing.SystemFonts.MenuFont!, System.Drawing.FontStyle.Bold);
            item.Click += (_, _) => _svc.SetDefault(m.Id);
            menu.Items.Add(item);
        }

        if (visible.Count == 0)
            menu.Items.Add(Strings.NoMicsVisible).Enabled = false;

        menu.Items.Add(new WinForms.ToolStripSeparator());

        // ── 表示デバイス管理サブメニュー ─────────────────────
        var mgmt = new WinForms.ToolStripMenuItem(Strings.DevicesMgmt(excluded));

        foreach (var mic in allMics)
        {
            var m      = mic;
            bool isHid = _settings.ExcludedIds.Contains(m.Id);
            var sub    = new WinForms.ToolStripMenuItem(m.Name)
            {
                Checked      = !isHid,
                CheckOnClick = false,
                ToolTipText  = isHid ? Strings.ShowDevice : Strings.HideDevice,
            };
            sub.Click += (_, _) =>
            {
                if (_settings.ExcludedIds.Contains(m.Id))
                    _settings.ExcludedIds.Remove(m.Id);
                else
                    _settings.ExcludedIds.Add(m.Id);
                _settings.Save();
                RefreshTray();
                // ウィンドウが開いていれば同期
                _window?.RequestRefresh();
            };
            mgmt.DropDownItems.Add(sub);
        }
        menu.Items.Add(mgmt);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        // ── ミュートトグル ──────────────────────────────────
        var muteItem = new WinForms.ToolStripMenuItem(muted ? Strings.Unmute : Strings.MuteMenu);
        muteItem.Click += (_, _) => _svc.SetMute(!_svc.IsMuted());
        menu.Items.Add(muteItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        // ── ホットキー設定 ───────────────────────────────────
        menu.Items.Add(Strings.HotkeyMenu(hkLabel)).Click += (_, _) =>
            Dispatcher.BeginInvoke(OpenHotkeySettings);

        // ── スタートアップ登録 ───────────────────────────────
        var startupItem = new WinForms.ToolStripMenuItem(Strings.StartupItem)
        {
            Checked      = IsStartupEnabled(),
            CheckOnClick = false,
        };
        startupItem.Click += (_, _) =>
        {
            bool newState = !IsStartupEnabled();
            SetStartupEnabled(newState);
            startupItem.Checked = IsStartupEnabled();
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        // ── ウィンドウ / 終了 ────────────────────────────────
        menu.Items.Add(Strings.OpenWindow).Click += (_, _) => ShowWindow();
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(Strings.Exit).Click += (_, _) => ExitApp();

        _tray.ContextMenuStrip = menu;
        oldMenu?.Dispose();
    }

    // ── グローバルホットキー登録 ─────────────────────────────
    private void RegisterGlobalHotkey()
    {
        var helper = new WindowInteropHelper(_window!);
        helper.EnsureHandle();
        _hotkeyHwnd = helper.Handle;

        _hotkeySource = HwndSource.FromHwnd(_hotkeyHwnd);
        _hotkeySource?.AddHook(HotkeyHook);

        ApplyHotkey();
    }

    private void ApplyHotkey()
    {
        UnregisterHotKey(_hotkeyHwnd, HK_MUTE);
        if (!RegisterHotKey(_hotkeyHwnd, HK_MUTE, _settings.HotkeyMods, _settings.HotkeyVk))
        {
            var label = HotkeyHelper.Format(_settings.HotkeyMods, _settings.HotkeyVk);
            _tray?.ShowBalloonTip(4000, "MicPyon",
                Strings.HotkeyFailed(label),
                WinForms.ToolTipIcon.Warning);
        }
    }

    private void OpenHotkeySettings()
    {
        var dlg = new HotkeyDialog(_settings.HotkeyMods, _settings.HotkeyVk);
        if (dlg.ShowDialog() == true)
        {
            _settings.HotkeyMods = dlg.ResultMods;
            _settings.HotkeyVk   = dlg.ResultVk;
            _settings.Save();
            ApplyHotkey();
            RefreshTray();
            _window?.UpdateHotkeyLabel();
            _window?.RequestRefresh();
        }
    }

    private IntPtr HotkeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HK_MUTE)
        {
            _svc!.SetMute(!_svc.IsMuted());
            handled = true;
        }
        return IntPtr.Zero;
    }

    // ── ウィンドウを手前に表示 ──────────────────────────────
    private void ShowWindow()
    {
        _window?.Show();
        if (_window != null) _window.WindowState = WindowState.Normal;
        _window?.Activate();
    }

    private void ExitApp()
    {
        if (_tray != null) _tray.Visible = false;
        Dispatcher.Invoke(Shutdown);
    }

    // ── トレイアイコン生成（16×16 px）──────────────────────
    private static System.Drawing.Icon MakeTrayIcon(bool muted)
    {
        var bmp = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);

        using var bg = new System.Drawing.SolidBrush(
            muted ? System.Drawing.Color.FromArgb(211,  47,  47)
                  : System.Drawing.Color.FromArgb(  0, 150, 136));
        g.FillEllipse(bg, 0, 0, 15, 15);

        if (muted)
        {
            using var p = new System.Drawing.Pen(System.Drawing.Color.White, 2.2f);
            g.DrawLine(p, 4, 4, 11, 11);
            g.DrawLine(p, 11, 4, 4, 11);
        }
        else
        {
            using var wb = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            g.FillEllipse(wb, 5, 2, 6, 6);
            g.FillRectangle(wb, 5, 4, 6, 5);
            g.FillEllipse(wb, 5, 6, 6, 4);
            using var wp = new System.Drawing.Pen(System.Drawing.Color.White, 1.5f);
            g.DrawArc(wp, 4, 8, 8, 5, 0, 180);
            g.DrawLine(wp, 8, 13, 8, 15);
            g.DrawLine(wp, 5, 15, 11, 15);
        }

        var hIcon = bmp.GetHicon();
        bmp.Dispose();
        return System.Drawing.Icon.FromHandle(hIcon);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_hotkeyHwnd != IntPtr.Zero) UnregisterHotKey(_hotkeyHwnd, HK_MUTE);
        _hotkeySource?.Dispose();
        _tray?.Dispose();
        _iconActive?.Dispose();
        _iconMuted?.Dispose();
        _svc?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
