using System.Globalization;

namespace Maipiyon;

/// <summary>OS言語に応じてUI文字列を切り替える</summary>
public static class Strings
{
    private static readonly bool IsJa =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja";

    // ── MainWindow ───────────────────────────────────────────
    public static string Refresh        => IsJa ? "🔄  再取得"                    : "🔄  Refresh";
    public static string NoDefaultMic   => IsJa ? "既定マイクなし"               : "No default mic";
    public static string InUse          => IsJa ? "使用中 — 切り替え不可"        : "In use — cannot switch";
    public static string MuteActive     => IsJa ? "🔇  ミュート中　— クリックで解除" : "🔇  Muted — Click to unmute";
    public static string MuteIdle       => IsJa ? "🎤  ミュート"                  : "🎤  Mute";
    public static string UsingMic(string n) => IsJa ? $"🎙  {n} を使ってるよ" : $"🎙  {n}";
    public static string CloseTooltip   => IsJa ? "閉じる（トレイに残ります）"   : "Close (stays in tray)";
    public static string HotkeyPrefix   => IsJa ? "ミュート: "                    : "Mute: ";

    // ── Tray menu ────────────────────────────────────────────
    public static string TrayMuted(string hk)     => IsJa ? $"まいぴょん [{hk}] ミュート中"    : $"MicPyon [{hk}] Muted";
    public static string TrayActive(string hk, string dev) => IsJa ? $"まいぴょん [{hk}] {dev}" : $"MicPyon [{hk}] {dev}";
    public static string NoMicsVisible  => IsJa ? "（表示中のマイクがありません）" : "(No microphones available)";
    public static string DevicesMgmt(int hidden) => hidden > 0
        ? (IsJa ? $"⚙ 表示するデバイス（{hidden}台を非表示中）" : $"⚙ Devices ({hidden} hidden)")
        : (IsJa ? "⚙ 表示するデバイスを選ぶ" : "⚙ Choose devices to show");
    public static string ShowDevice     => IsJa ? "クリックで再表示"    : "Click to show";
    public static string HideDevice     => IsJa ? "クリックで非表示に"  : "Click to hide";
    public static string Unmute         => IsJa ? "🔊 ミュート解除"      : "🔊 Unmute";
    public static string MuteMenu       => IsJa ? "🔇 ミュートする"      : "🔇 Mute";
    public static string OpenWindow     => IsJa ? "📋 ウィンドウを開く" : "📋 Open window";
    public static string Exit           => IsJa ? "終了"                 : "Exit";
    public static string HotkeyMenu(string hk) => IsJa ? $"⌨ ホットキー設定（現在: {hk}）" : $"⌨ Hotkey settings (current: {hk})";
    public static string StartupItem    => IsJa ? "🚀 Windows起動時に自動実行" : "🚀 Launch at Windows startup";

    // ── Startup ──────────────────────────────────────────────
    public static string AlreadyRunning => IsJa
        ? "まいぴょんはすでに起動しています。\nタスクトレイを確認してください。"
        : "MicPyon is already running.\nCheck the system tray.";
    public static string HotkeyFailed(string hk) => IsJa
        ? $"{hk} は別のアプリが使用中のため登録できませんでした。"
        : $"{hk} is already in use by another application.";

    // ── HotkeyDialog ─────────────────────────────────────────
    public static string HotkeyDialogTitle  => IsJa ? "ホットキー設定"                  : "Hotkey Settings";
    public static string HotkeyDialogPrompt => IsJa ? "新しいショートカットを押してください" : "Press a new shortcut key";
    public static string Save               => IsJa ? "保存"       : "Save";
    public static string Cancel             => IsJa ? "キャンセル" : "Cancel";
}
