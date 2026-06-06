using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Maipiyon;

public partial class MainWindow : Window
{
    private readonly MicService  _svc;
    private readonly AppSettings _settings;
    private List<MicDevice>      _mics = [];

    // ── ダークテーマカラー定義 ─────────────────────────────────
    private static readonly Color ColCard    = Color.FromRgb(0x22, 0x24, 0x36);
    private static readonly Color ColCardH   = Color.FromRgb(0x2A, 0x2D, 0x45);
    private static readonly Color ColActive  = Color.FromRgb(0x0D, 0x2E, 0x2A);
    private static readonly Color ColActiveH = Color.FromRgb(0x0F, 0x38, 0x33);
    private static readonly Color ColAccent  = Color.FromRgb(0x00, 0xBF, 0xA5); // teal
    private static readonly Color ColRed     = Color.FromRgb(0xEF, 0x53, 0x50);
    private static readonly Color ColTextPri = Colors.White;
    private static readonly Color ColTextSec = Color.FromRgb(0xA9, 0xB1, 0xD6);
    private static readonly Color ColTextDim = Color.FromRgb(0x56, 0x5F, 0x89);

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
        StatusLabel.Text       = def != null ? $"🎙  {def.Name}" : Strings.NoDefaultMic;
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

        // マイク名 + 接続タイプ (縦StackPanel)
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(11, 10, 11, 10),
        };

        var text = new TextBlock
        {
            Text         = mic.Name,
            Foreground   = new SolidColorBrush(active ? ColTextPri : ColTextSec),
            FontSize     = 13,
            FontWeight   = active ? FontWeights.SemiBold : FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        stack.Children.Add(text);

        if (!string.IsNullOrEmpty(mic.ConnectionType))
        {
            stack.Children.Add(new TextBlock
            {
                Text       = mic.ConnectionType,
                Foreground = new SolidColorBrush(active
                    ? Color.FromRgb(0x4D, 0xD0, 0xC4)   // 明るいteal
                    : ColTextDim),
                FontSize   = 10,
                Margin     = new Thickness(0, 2, 0, 0),
            });
        }

        Grid.SetColumn(stack, 1);

        grid.Children.Add(accentBar);
        grid.Children.Add(stack);
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

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(11, 10, 11, 10),
        };

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

        Grid.SetColumn(stack, 1);
        grid.Children.Add(accentBar);
        grid.Children.Add(stack);
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
    }

    private void MuteBtn_Click(object sender, RoutedEventArgs e)
    {
        _svc.SetMute(!_svc.IsMuted());
        UpdateMuteBtn();
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
                new SolidColorBrush(ColCard));
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
            new SolidColorBrush(muted ? ColRed : ColTextSec));
        label.SetValue(TextBlock.FontSizeProperty, 13.0);
        label.SetValue(TextBlock.FontWeightProperty,
            muted ? FontWeights.SemiBold : FontWeights.Normal);
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
