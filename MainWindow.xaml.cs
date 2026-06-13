using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Maipiyon;

public partial class MainWindow : Window
{
    private readonly MicService  _svc;
    private readonly AppSettings _settings;
    private List<MicDevice>      _mics = [];

    // ── ダークテーマカラー定義 ─────────────────────────────────
    private static readonly Color ColCard    = Color.FromRgb(0x22, 0x24, 0x36);
    private static readonly Color ColCardH   = Color.FromRgb(0x2A, 0x2D, 0x45);
    private static readonly Color ColActive  = Color.FromRgb(0x2E, 0x21, 0x40); // 選択行（ピンク寄りダーク）
    private static readonly Color ColActiveH = Color.FromRgb(0x38, 0x28, 0x4A);
    private static readonly Color ColAccent  = Color.FromRgb(0xEA, 0x4C, 0x9D); // ブランドピンク
    private static readonly Color ColRed     = Color.FromRgb(0xEF, 0x53, 0x50);
    private static readonly Color ColTextPri = Colors.White;
    private static readonly Color ColTextSec = Color.FromRgb(0xD5, 0xDB, 0xF0); // マイク名（非選択）
    private static readonly Color ColTextDim = Color.FromRgb(0x82, 0x8D, 0xB8); // サブテキスト（明るめ）

    // ── 行アイコン / 選択行テキスト ─────────────────────────────
    private static readonly Color      ColChip       = Color.FromRgb(0x2A, 0x2E, 0x45);
    private static readonly Color      ColChipActive = Color.FromRgb(0x3A, 0x24, 0x40);
    private static readonly Color      ColIcon       = Color.FromRgb(0x9A, 0xA3, 0xCC);
    private static readonly Color      ColNameActive = Color.FromRgb(0xF8, 0xC2, 0xDE);
    private static readonly Color      ColSubActive  = Color.FromRgb(0xCE, 0x93, 0xB4);
    private static readonly FontFamily SegoeIcons    = new("Segoe MDL2 Assets");

    // 接続タイプ → アイコン（Bluetooth はBTマーク、それ以外はマイク）
    private static string IconGlyph(string? conn)
        => conn != null && conn.Contains("Bluetooth")
            ? ((char)0xE702).ToString()   // Bluetooth
            : ((char)0xE720).ToString();  // Microphone

    public MainWindow(MicService svc, AppSettings settings)
    {
        _svc      = svc;
        _settings = settings;
        InitializeComponent();
        Topmost = true;

        // 初回表示時に画面右下へ配置
        Loaded += (_, _) =>
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Right  - ActualWidth  - 14;
            Top  = wa.Bottom - ActualHeight - 14;
        };

        _svc.DevicesChanged += () => Dispatcher.BeginInvoke(Refresh);
        _svc.MuteChanged    += () => Dispatcher.BeginInvoke(UpdateMuteBtn);
        Refresh();
    }

    // App 側から設定変更を通知されたときに呼ぶ
    public void RequestRefresh() => Dispatcher.BeginInvoke(Refresh);

    // ── タイトルバードラッグ ────────────────────────────────────
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // ボタン上のクリックは除外
        if (e.OriginalSource is Button ||
            (e.OriginalSource as FrameworkElement)?.TemplatedParent is Button)
            return;
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // ×ボタン → ウィンドウを隠す（アプリは終了しない）
    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Hide();

    private void Window_Closing(object s, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    // ── マイク一覧を描画 ─────────────────────────────────────────
    // ホットキー表示ラベル（外部から更新用）
    public void UpdateHotkeyLabel()
    {
        var label = HotkeyHelper.Format(_settings.HotkeyMods, _settings.HotkeyVk);
        HotkeyLabel.Text = Strings.HotkeyPrefix + label;
    }

    private void Refresh()
    {
        var all = _svc.GetAllMics()
                      .Where(x => !_settings.ExcludedIds.Contains(x.Mic.Id))
                      .ToList();
        _mics = all.Where(x => !x.IsGhosted).Select(x => x.Mic).ToList();
        MicPanel.Children.Clear();

        foreach (var (mic, isGhosted) in all)
            MicPanel.Children.Add(isGhosted ? MakeGhostCard(mic) : MakeMicCard(mic));

        // 再取得リンク
        var refreshLink = MakeTextBtn(Strings.Refresh, ColTextDim, 10.5);
        refreshLink.Margin = new Thickness(0, 6, 0, 2);
        refreshLink.MouseLeftButtonDown += (_, _) => Refresh();
        MicPanel.Children.Add(refreshLink);

        UpdateMuteBtn();
        UpdateHotkeyLabel();

        var def = _mics.FirstOrDefault(m => m.IsDefault);
        StatusLabel.Text       = def != null ? Strings.UsingMic(def.Name) : Strings.NoDefaultMic;
        StatusLabel.Foreground = new SolidColorBrush(ColTextDim);
    }

    // ── マイクカード（Borderベース）─────────────────────────────
    private Border MakeMicCard(MicDevice mic)
    {
        bool active  = mic.IsDefault;
        var  bgNorm  = active ? ColActive  : ColCard;
        var  bgHover = active ? ColActiveH : ColCardH;

        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            Margin       = new Thickness(0, 0, 0, 5),
            Cursor       = Cursors.Hand,
            Background   = new SolidColorBrush(bgNorm),
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 左アクセントバー
        var accentBar = new Border
        {
            Background   = new SolidColorBrush(active ? ColAccent : Colors.Transparent),
            CornerRadius = new CornerRadius(8, 0, 0, 8),
        };
        Grid.SetColumn(accentBar, 0);

        // アイコンチップ ＋（マイク名 + 接続タイプ）
        var content = new Grid { Margin = new Thickness(8, 8, 11, 8) };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconChip = new Border
        {
            Width             = 30,
            Height            = 30,
            CornerRadius      = new CornerRadius(8),
            Background        = new SolidColorBrush(active ? ColChipActive : ColChip),
            Margin            = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text                = IconGlyph(mic.ConnectionType),
                FontFamily          = SegoeIcons,
                FontSize            = 15,
                Foreground          = new SolidColorBrush(active ? ColAccent : ColIcon),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            },
        };
        Grid.SetColumn(iconChip, 0);

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(stack, 1);

        stack.Children.Add(new TextBlock
        {
            Text         = mic.Name,
            Foreground   = new SolidColorBrush(active ? ColNameActive : ColTextSec),
            FontSize     = 13,
            FontWeight   = active ? FontWeights.SemiBold : FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        if (!string.IsNullOrEmpty(mic.ConnectionType))
        {
            stack.Children.Add(new TextBlock
            {
                Text       = mic.ConnectionType,
                Foreground = new SolidColorBrush(active ? ColSubActive : ColTextDim),
                FontSize   = 10,
                Margin     = new Thickness(0, 2, 0, 0),
            });
        }

        content.Children.Add(iconChip);
        content.Children.Add(stack);
        Grid.SetColumn(content, 1);

        grid.Children.Add(accentBar);
        grid.Children.Add(content);
        card.Child = grid;

        // ホバーエフェクト
        card.MouseEnter += (_, _) => card.Background = new SolidColorBrush(bgHover);
        card.MouseLeave += (_, _) => card.Background = new SolidColorBrush(bgNorm);

        // クリック → デフォルトマイク切り替え
        var micId = mic.Id;
        card.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            var (ok, err) = _svc.SetDefault(micId);
            if (!ok)
            {
                StatusLabel.Text       = $"⚠  {err}";
                StatusLabel.Foreground = new SolidColorBrush(ColRed);
            }
            else
            {
                Refresh();
                StatusLabel.Foreground = new SolidColorBrush(ColAccent);
            }
        };

        return card;
    }

    // ── グレーアウトカード（消えたデバイス表示）─────────────────
    private Border MakeGhostCard(MicDevice mic)
    {
        var dimBg = Color.FromRgb(0x1C, 0x1E, 0x2E); // 通常より暗め

        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            Margin       = new Thickness(0, 0, 0, 5),
            Background   = new SolidColorBrush(dimBg),
            Opacity      = 0.55,
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var accentBar = new Border
        {
            Background   = Brushes.Transparent,
            CornerRadius = new CornerRadius(8, 0, 0, 8),
        };
        Grid.SetColumn(accentBar, 0);

        var content = new Grid { Margin = new Thickness(8, 8, 11, 8) };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconChip = new Border
        {
            Width             = 30,
            Height            = 30,
            CornerRadius      = new CornerRadius(8),
            Background        = new SolidColorBrush(ColChip),
            Margin            = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text                = IconGlyph(mic.ConnectionType),
                FontFamily          = SegoeIcons,
                FontSize            = 15,
                Foreground          = new SolidColorBrush(ColTextDim),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            },
        };
        Grid.SetColumn(iconChip, 0);

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(stack, 1);

        stack.Children.Add(new TextBlock
        {
            Text         = mic.Name,
            Foreground   = new SolidColorBrush(ColTextDim),
            FontSize     = 13,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        stack.Children.Add(new TextBlock
        {
            Text       = Strings.InUse,
            Foreground = new SolidColorBrush(ColTextDim),
            FontSize   = 10,
            Margin     = new Thickness(0, 2, 0, 0),
        });

        content.Children.Add(iconChip);
        content.Children.Add(stack);
        Grid.SetColumn(content, 1);

        grid.Children.Add(accentBar);
        grid.Children.Add(content);
        card.Child = grid;

        return card;
    }

    // ── ミュートボタン更新 ───────────────────────────────────────
    private void UpdateMuteBtn()
    {
        bool muted   = _svc.IsMuted();
        MuteBtn.Content  = muted ? Strings.MuteActive : Strings.MuteIdle;
        MuteBtn.Template = MakeMuteBtnTemplate(muted);

        // タイトルバーのドット: 通常=teal / ミュート中=赤
        TitleDot.Fill = new SolidColorBrush(muted ? ColRed : ColAccent);

        // ミュート中オーバーレイ（赤枠＋斜めバナー）。目の端で気づけるよう赤枠を点滅
        MuteOverlay.Visibility = muted ? Visibility.Visible : Visibility.Collapsed;
        if (muted) { EnsurePulse(); _pulse!.Begin(); }
        else       { _pulse?.Stop(); }
    }

    private void MuteBtn_Click(object sender, RoutedEventArgs e)
    {
        _svc.SetMute(!_svc.IsMuted());
        UpdateMuteBtn();
    }

    // ミュート中オーバーレイ → どこをクリックしても解除
    private void MuteOverlay_Unmute(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _svc.SetMute(false);
        UpdateMuteBtn();
    }

    // 赤枠のゆっくり点滅（うるさすぎず、視界の端で気づける速さ）
    private Storyboard? _pulse;
    private void EnsurePulse()
    {
        if (_pulse != null) return;
        var anim = new DoubleAnimation
        {
            From           = 1.0,
            To             = 0.6,
            Duration       = new Duration(TimeSpan.FromMilliseconds(700)),
            AutoReverse    = true,
            RepeatBehavior = RepeatBehavior.Forever,
        };
        Storyboard.SetTarget(anim, MuteFrame);
        Storyboard.SetTargetProperty(anim, new PropertyPath(OpacityProperty));
        _pulse = new Storyboard();
        _pulse.Children.Add(anim);
    }

    // ── ミュートボタンテンプレート ────────────────────────────────
    private static ControlTemplate MakeMuteBtnTemplate(bool muted)
    {
        var tmpl    = new ControlTemplate(typeof(Button));
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        factory.SetValue(Border.PaddingProperty, new Thickness(0, 11, 0, 11));

        if (muted)
        {
            factory.SetValue(Border.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(0x44, 0x0E, 0x0E)));
            factory.SetValue(Border.BorderBrushProperty,
                new SolidColorBrush(ColRed));
            factory.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
        }
        else
        {
            factory.SetValue(Border.BackgroundProperty,
                new SolidColorBrush(ColAccent));               // ブランドピンクのベタ塗り
            factory.SetValue(Border.BorderBrushProperty,
                new SolidColorBrush(Colors.Transparent));
            factory.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
        }

        var row = new FrameworkElementFactory(typeof(StackPanel));
        row.SetValue(StackPanel.OrientationProperty,           Orientation.Horizontal);
        row.SetValue(StackPanel.HorizontalAlignmentProperty,   HorizontalAlignment.Center);
        row.SetValue(StackPanel.VerticalAlignmentProperty,     VerticalAlignment.Center);

        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetBinding(TextBlock.TextProperty,
            new System.Windows.Data.Binding("Content")
            { RelativeSource = new System.Windows.Data.RelativeSource(
                System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        label.SetValue(TextBlock.ForegroundProperty,
            new SolidColorBrush(muted ? ColRed : Colors.White));
        label.SetValue(TextBlock.FontSizeProperty, 13.0);
        label.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        label.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

        row.AppendChild(label);
        factory.AppendChild(row);
        tmpl.VisualTree = factory;
        return tmpl;
    }

    // ── テキストリンク生成 ────────────────────────────────────────
    private static Border MakeTextBtn(string text, Color color, double fontSize)
    {
        var b = new Border
        {
            Cursor              = Cursors.Hand,
            Background          = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        b.Child = new TextBlock
        {
            Text       = text,
            FontSize   = fontSize,
            Foreground = new SolidColorBrush(color),
        };
        return b;
    }
}
