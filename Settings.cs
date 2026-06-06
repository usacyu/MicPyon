using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maipiyon;

/// <summary>アプリ設定（AppData に保存）</summary>
public class AppSettings
{
    private static readonly string _dir =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Maipiyon");
    private static readonly string _file = System.IO.Path.Combine(_dir, "settings.json");

    /// <summary>トレイ・ウィンドウに表示しないデバイスの ID 一覧</summary>
    [JsonPropertyName("excluded")]
    public HashSet<string> ExcludedIds { get; set; } = [];

    /// <summary>ホットキー修飾キー (Ctrl=2, Shift=4, Alt=1)</summary>
    [JsonPropertyName("hotkeyMods")]
    public uint HotkeyMods { get; set; } = 0x0002 | 0x0004; // Ctrl+Shift

    /// <summary>ホットキー仮想キーコード (M=0x4D)</summary>
    [JsonPropertyName("hotkeyVk")]
    public uint HotkeyVk { get; set; } = 0x4D; // M

    public static AppSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(_file))
                return JsonSerializer.Deserialize<AppSettings>(System.IO.File.ReadAllText(_file)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            System.IO.Directory.CreateDirectory(_dir);
            System.IO.File.WriteAllText(_file,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
