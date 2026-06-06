using NAudio.CoreAudioApi;
using System.Runtime.InteropServices;

namespace Maipiyon;

public record MicDevice(string Id, string Name, bool IsDefault, string ConnectionType = "");

// ── IPolicyConfig COM インターフェース ─────────────────────────────────
// Windows 10/11 でデフォルトオーディオエンドポイントを変更する非公式API
// CLSID: {870AF99C-171D-4F9E-AF0D-E63DF40C2BC9} = CPolicyConfigClient
// IID:   {F8679F50-850A-41CF-9C72-430F290290C8} = IPolicyConfig
[ComImport]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    void GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId, out IntPtr ppFormat);
    void GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId,
                         [MarshalAs(UnmanagedType.Bool)] bool bDefault, out IntPtr ppFormat);
    void ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId);
    void SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId,
                         IntPtr pEndpointFormat, IntPtr MixFormat);
    void GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId,
                             [MarshalAs(UnmanagedType.Bool)] bool bDefault,
                             out long pmftDefaultPeriod, out long pmftMinimumPeriod);
    void SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId, long pmftPeriod);
    void GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId, out int pDeviceShareMode);
    void SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId, int deviceShareMode);
    void GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId,
                          [MarshalAs(UnmanagedType.Bool)] bool bFxStore, IntPtr key, IntPtr pv);
    void SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId,
                          [MarshalAs(UnmanagedType.Bool)] bool bFxStore, IntPtr key, IntPtr pv);
    // vtable slot 13
    [PreserveSig]
    int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId, uint eRole);
    // vtable slot 14
    [PreserveSig]
    int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string pstrEndpointId,
                              [MarshalAs(UnmanagedType.Bool)] bool bVisible);
}

public class MicService : IDisposable
{
    private readonly MMDeviceEnumerator _enum = new();
    private readonly NotifyClient       _notify;

    // デバイスの追加・削除・状態変化・デフォルト変更時に発火
    public event Action? DevicesChanged;

    // CPolicyConfigClient の CLSID (Windows 10/11)
    private static readonly Guid s_clsid = new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");

    // これまでに見えたことがあるデバイス（消えたデバイスをグレーアウト表示するため）
    private readonly Dictionary<string, MicDevice> _knownDevices = [];

    public MicService()
    {
        // 起動時に全デバイスを記録しておく（グレーアウト用）
        MergeKnownDevices();

        _notify = new NotifyClient(() => DevicesChanged?.Invoke());
        _enum.RegisterEndpointNotificationCallback(_notify);
    }

    private void MergeKnownDevices()
    {
        var defaultId = GetDefaultId();
        foreach (var d in _enum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            _knownDevices[d.ID] = new MicDevice(d.ID, d.FriendlyName, d.ID == defaultId, DetectConnectionType(d));
    }

    // ── デバイス列挙（安定した順番で全デバイスを返す）────────
    // isGhosted=true のデバイスは現在アクティブでない（グレーアウト表示用）
    public List<(MicDevice Mic, bool IsGhosted)> GetAllMics()
    {
        MergeKnownDevices();
        var activeIds = _enum
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => d.ID)
            .ToHashSet();
        var defaultId = GetDefaultId();

        return _knownDevices.Values
            .Select(m => (
                Mic: m with { IsDefault = m.Id == defaultId },
                IsGhosted: !activeIds.Contains(m.Id)
            ))
            .ToList();
    }

    public string GetDefaultId()
    {
        try { return _enum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).ID; }
        catch { return ""; }
    }

    // ── ミュート ────────────────────────────────────────────
    public bool IsMuted()
    {
        try { return _enum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).AudioEndpointVolume.Mute; }
        catch { return false; }
    }

    // ミュート状態が変化したときに発火（トレイアイコン更新用）
    public event Action? MuteChanged;

    public void SetMute(bool mute)
    {
        try
        {
            _enum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).AudioEndpointVolume.Mute = mute;
            MuteChanged?.Invoke();
        }
        catch { }
    }

    // ── デフォルト切り替え ─────────────────────────────────
    public (bool ok, string err) SetDefault(string deviceId)
    {
        try
        {
            var type = Type.GetTypeFromCLSID(s_clsid)
                       ?? throw new Exception("PolicyConfig CLSID が見つかりません");
            var obj = Activator.CreateInstance(type)
                      ?? throw new Exception("PolicyConfig 作成失敗");

            if (obj is not IPolicyConfig pc)
                return (false, "IPolicyConfig インターフェース取得失敗 (E_NOINTERFACE)");

            // 全ロールに設定（eConsole=0, eMultimedia=1, eCommunications=2）
            pc.SetDefaultEndpoint(deviceId, 0);
            pc.SetDefaultEndpoint(deviceId, 1);
            pc.SetDefaultEndpoint(deviceId, 2);

            // 反映まで少し待ってから確認
            System.Threading.Thread.Sleep(80);
            if (IsNowDefault(deviceId))
                return (true, "");

            return (false, "APIコール成功しましたがデバイスが変更されませんでした");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private bool IsNowDefault(string deviceId)
    {
        foreach (Role r in new[] { Role.Console, Role.Multimedia, Role.Communications })
        {
            try
            {
                if (_enum.GetDefaultAudioEndpoint(DataFlow.Capture, r).ID == deviceId) return true;
            }
            catch { }
        }
        return false;
    }

    // ── 接続タイプ検出 ─────────────────────────────────────
    // PKEY_Device_EnumeratorName  = {A45C254E-DF1C-4EFD-8020-67D146A850E0}, pid=24
    // PKEY_DeviceInterface_FriendlyName = {026E516E-B814-414B-83CD-856D6FEF4822}, pid=2
    private static string DetectConnectionType(NAudio.CoreAudioApi.MMDevice dev)
    {
        // 試み1: デバイス列挙子名（USB / BTHHFENUM / HDAUDIO など）
        var enumerator = ReadStringProp(dev,
            new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 24);
        if (enumerator.Length > 0)
        {
            var up = enumerator.ToUpperInvariant();
            if (up == "USB") return "USB";
            if (up.StartsWith("BTHHF") || up.StartsWith("BTHLE") || up.Contains("BT"))
                return "Bluetooth";
            if (up == "HDAUDIO") return "3.5mm / 内蔵";
        }

        // 試み2: インターフェース名（"USB Audio Device" / "Bluetooth Hands-free Audio" など）
        var ifName = ReadStringProp(dev,
            new Guid("026E516E-B814-414B-83CD-856D6FEF4822"), 2);
        if (ifName.Length > 0)
        {
            var up = ifName.ToUpperInvariant();
            if (up.Contains("USB"))       return "USB";
            if (up.Contains("BLUETOOTH") || up.Contains("HANDS-FREE")) return "Bluetooth";
        }

        return "";
    }

    private static string ReadStringProp(
        NAudio.CoreAudioApi.MMDevice dev, Guid guid, int pid)
    {
        try
        {
            var key = new NAudio.CoreAudioApi.PropertyKey(guid, pid);
            return dev.Properties[key].Value as string ?? "";
        }
        catch { return ""; }
    }

    public void Dispose()
    {
        _enum.UnregisterEndpointNotificationCallback(_notify);
        _enum.Dispose();
    }

    // ── デバイス変化通知クライアント ────────────────────────
    private sealed class NotifyClient : NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
    {
        private readonly Action _callback;
        public NotifyClient(Action callback) => _callback = callback;

        public void OnDeviceStateChanged(string deviceId, DeviceState newState) => _callback();
        public void OnDeviceAdded(string pwstrDeviceId)                          => _callback();
        public void OnDeviceRemoved(string deviceId)                             => _callback();
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => _callback();
        public void OnPropertyValueChanged(string pwstrDeviceId, NAudio.CoreAudioApi.PropertyKey key) { }
    }
}
