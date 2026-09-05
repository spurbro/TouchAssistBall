using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TouchAssistBall
{
    public class SettingsWindow : Window
    {
        private readonly AppConfig _config;

        private class GestureSlotUI
        {
            public ActionType DefaultAction;
            public ComboBox CbAction;
            public TextBox TxtCustomKey;
            public Button BtnRecord;
            public Button BtnKeyHelper;
            public StackPanel CustomKeyPanel;
            public Border BadgeLiveTag;
            public TextBlock TxtLiveTag;
            public bool IsRecording = false;
        }

        private GestureSlotUI _slotClick;
        private GestureSlotUI _slotLongPress;
        private GestureSlotUI _slotSwipeLeft;
        private GestureSlotUI _slotSwipeRight;
        private GestureSlotUI _slotSwipeUp;
        private GestureSlotUI _slotSwipeDown;

        private Slider _sliderSwipeThreshold;
        private TextBlock _txtSwipeThresholdVal;
        private Slider _sliderLongPressThreshold;
        private TextBlock _txtLongPressThresholdVal;
        private Slider _sliderSize;
        private TextBlock _txtSizeVal;
        private Slider _sliderNormalOpacity;
        private TextBlock _txtNormalOpacityVal;
        private Slider _sliderActiveOpacity;
        private TextBlock _txtActiveOpacityVal;
        private Slider _sliderIdleOpacity;
        private TextBlock _txtIdleOpacityVal;
        private Slider _sliderIdleTimeout;
        private TextBlock _txtIdleTimeoutVal;

        private CheckBox _chkShowHUD;
        private CheckBox _chkAutoStart;

        private TextBlock _txtStatusBanner;

        private class ActionItem
        {
            public ActionType Action { get; set; }
            public string DisplayName { get; set; }
            public override string ToString() { return DisplayName; }
        }

        public SettingsWindow(AppConfig config)
        {
            _config = config;

            Title = "触屏桌面悬浮球 - 全功能按键自定义与设置";
            Width = 580;
            Height = 740;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
            FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI");

            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
                }
            }
            catch { }

            BuildUI();
            LoadConfigToUI();
        }

        private void BuildUI()
        {
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(65) });

            TabControl tabs = new TabControl { Margin = new Thickness(10) };

            TabItem tabGestures = new TabItem { Header = "🎯 自由按键映射 (全自定义)", Content = BuildGesturesTab() };
            TabItem tabAppearance = new TabItem { Header = "🎨 触控灵敏度与外观", Content = BuildAppearanceTab() };
            TabItem tabAbout = new TabItem { Header = "ℹ️ 操作指南", Content = BuildAboutTab() };

            tabs.Items.Add(tabGestures);
            tabs.Items.Add(tabAppearance);
            tabs.Items.Add(tabAbout);

            root.Children.Add(tabs);
            Grid.SetRow(tabs, 0);

            // Bottom Buttons Bar
            Grid bottomBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)) };
            StackPanel spLeft = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(15, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            StackPanel spRight = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 15, 0), VerticalAlignment = VerticalAlignment.Center };

            Button btnReset = new Button
            {
                Content = "恢复默认值",
                Width = 95,
                Height = 34,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                Cursor = Cursors.Hand
            };
            btnReset.Click += (s, e) => ResetDefaults();
            spLeft.Children.Add(btnReset);

            _txtStatusBanner = new TextBlock
            {
                Text = "💡 设置后点击保存立即生效",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                FontSize = 12,
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            spLeft.Children.Add(_txtStatusBanner);

            Button btnCancel = new Button
            {
                Content = "取消",
                Width = 75,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            Button btnSave = new Button
            {
                Content = "💾 保存并应用",
                Width = 120,
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13.5,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnSave.Click += (s, e) => SaveUIToConfig();

            spRight.Children.Add(btnCancel);
            spRight.Children.Add(btnSave);

            bottomBar.Children.Add(spLeft);
            bottomBar.Children.Add(spRight);

            root.Children.Add(bottomBar);
            Grid.SetRow(bottomBar, 1);

            Content = root;
        }

        private ScrollViewer BuildGesturesTab()
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(16) };

            Border tipBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 14, 165, 233)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 14, 165, 233)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 14)
            };
            TextBlock lblHint = new TextBlock
            {
                Text = "💡 智能输入框退格：当检测到光标处于文本输入框内时，点击悬浮球将自动触发【⌫ 退格】，长按将自动触发【持续退格】。\n以下为悬浮球的默认按键与滑动手势映射：",
                Foreground = new SolidColorBrush(Color.FromRgb(3, 105, 161)),
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };
            tipBox.Child = lblHint;
            sp.Children.Add(tipBox);

            _slotClick = AddGestureSlot(sp, "🖱️ 悬浮球单击动作 (短按 / Tap):", ActionType.None);
            _slotLongPress = AddGestureSlot(sp, "✊ 悬浮球长按动作 (物理按住 / Hold - 随松手释放):", ActionType.HoldCurrentKey);
            _slotSwipeLeft = AddGestureSlot(sp, "👈 按住往左滑 (Swipe Left):", ActionType.Copy);
            _slotSwipeRight = AddGestureSlot(sp, "👉 按住往右滑 (Swipe Right):", ActionType.Paste);
            _slotSwipeUp = AddGestureSlot(sp, "👆 按住往上滑 (Swipe Up):", ActionType.VoiceTyping);
            _slotSwipeDown = AddGestureSlot(sp, "👇 按住往下滑 (Swipe Down):", ActionType.Screenshot);

            return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private GestureSlotUI AddGestureSlot(StackPanel parent, string title, ActionType defaultAction)
        {
            GestureSlotUI slot = new GestureSlotUI { DefaultAction = defaultAction };

            Grid headerGrid = new Grid { Margin = new Thickness(0, 8, 0, 4) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock tb = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Live visual tag badge
            slot.TxtLiveTag = new TextBlock
            {
                Text = "已就绪",
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52))
            };

            slot.BadgeLiveTag = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = slot.TxtLiveTag
            };

            headerGrid.Children.Add(tb);
            headerGrid.Children.Add(slot.BadgeLiveTag);
            Grid.SetColumn(slot.BadgeLiveTag, 1);

            slot.CbAction = new ComboBox
            {
                Height = 32,
                Margin = new Thickness(0, 0, 0, 4),
                FontSize = 13
            };

            foreach (ActionType at in Enum.GetValues(typeof(ActionType)))
            {
                slot.CbAction.Items.Add(new ActionItem { Action = at, DisplayName = ActionExecutor.GetActionDisplayName(at) });
            }

            // Custom Key Panel
            slot.CustomKeyPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed
            };

            slot.TxtCustomKey = new TextBox
            {
                Width = 230,
                Height = 30,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(6, 0, 6, 0),
                ToolTip = "输入自定义按键组合，例如：Ctrl+Alt+A、Win+D、LCtrl+F1、RAlt+Enter 等"
            };

            slot.TxtCustomKey.TextChanged += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(slot.TxtCustomKey.Text))
                {
                    ActionItem cur = slot.CbAction.SelectedItem as ActionItem;
                    if (cur == null || cur.Action != ActionType.CustomKey)
                    {
                        SetSlotToCustomKey(slot);
                    }
                }
                UpdateSlotLiveTag(slot);
            };

            slot.BtnRecord = new Button
            {
                Content = "🎙 录制按键",
                Width = 85,
                Height = 30,
                Margin = new Thickness(8, 0, 0, 0),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                Cursor = Cursors.Hand
            };

            slot.BtnKeyHelper = new Button
            {
                Content = "➕ 插入按键",
                Width = 85,
                Height = 30,
                Margin = new Thickness(6, 0, 0, 0),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                Cursor = Cursors.Hand
            };

            slot.BtnRecord.Click += (s, e) => StartKeyRecording(slot);
            slot.BtnKeyHelper.Click += (s, e) => ShowKeyHelperMenu(slot);

            slot.CustomKeyPanel.Children.Add(slot.TxtCustomKey);
            slot.CustomKeyPanel.Children.Add(slot.BtnRecord);
            slot.CustomKeyPanel.Children.Add(slot.BtnKeyHelper);

            slot.CbAction.SelectionChanged += (s, e) =>
            {
                ActionItem item = slot.CbAction.SelectedItem as ActionItem;
                if (item != null && item.Action == ActionType.CustomKey)
                {
                    slot.CustomKeyPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    slot.CustomKeyPanel.Visibility = Visibility.Collapsed;
                }
                UpdateSlotLiveTag(slot);
            };

            parent.Children.Add(headerGrid);
            parent.Children.Add(slot.CbAction);
            parent.Children.Add(slot.CustomKeyPanel);

            return slot;
        }

        private void SetSlotToCustomKey(GestureSlotUI slot)
        {
            if (slot == null || slot.CbAction == null) return;
            for (int i = 0; i < slot.CbAction.Items.Count; i++)
            {
                ActionItem item = slot.CbAction.Items[i] as ActionItem;
                if (item != null && item.Action == ActionType.CustomKey)
                {
                    if (slot.CbAction.SelectedIndex != i)
                    {
                        slot.CbAction.SelectedIndex = i;
                    }
                    if (slot.CustomKeyPanel != null)
                    {
                        slot.CustomKeyPanel.Visibility = Visibility.Visible;
                    }
                    break;
                }
            }
        }

        private void UpdateSlotLiveTag(GestureSlotUI slot)
        {
            ActionItem item = slot.CbAction.SelectedItem as ActionItem;
            if (item == null) return;

            if (item.Action == ActionType.CustomKey)
            {
                string key = slot.TxtCustomKey.Text.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    slot.TxtLiveTag.Text = "⚠️ 未输入按键";
                    slot.BadgeLiveTag.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                    slot.BadgeLiveTag.BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165));
                    slot.TxtLiveTag.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }
                else
                {
                    slot.TxtLiveTag.Text = "✅ " + key.ToUpper();
                    slot.BadgeLiveTag.Background = new SolidColorBrush(Color.FromRgb(238, 242, 255));
                    slot.BadgeLiveTag.BorderBrush = new SolidColorBrush(Color.FromRgb(165, 180, 252));
                    slot.TxtLiveTag.Foreground = new SolidColorBrush(Color.FromRgb(67, 56, 202));
                }
            }
            else
            {
                slot.TxtLiveTag.Text = "✅ " + ActionExecutor.GetActionShortLabel(item.Action);
                slot.BadgeLiveTag.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
                slot.BadgeLiveTag.BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208));
                slot.TxtLiveTag.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
            }
        }

        private void StartKeyRecording(GestureSlotUI slot)
        {
            slot.IsRecording = true;
            slot.BtnRecord.Content = "⏳ 请按按键...";
            slot.BtnRecord.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
            slot.TxtCustomKey.Focus();

            KeyEventHandler handler = null;
            handler = (s, e) =>
            {
                e.Handled = true;
                Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

                if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                    key == Key.LeftAlt || key == Key.RightAlt ||
                    key == Key.LeftShift || key == Key.RightShift ||
                    key == Key.LWin || key == Key.RWin)
                {
                    return;
                }

                string combo = BuildKeyComboString(key);
                if (!string.IsNullOrEmpty(combo))
                {
                    slot.TxtCustomKey.Text = combo;
                    SetSlotToCustomKey(slot);
                    UpdateSlotLiveTag(slot);
                }

                slot.IsRecording = false;
                slot.BtnRecord.Content = "🎙 录制按键";
                slot.BtnRecord.Background = Brushes.White;
                slot.TxtCustomKey.PreviewKeyDown -= handler;
            };

            slot.TxtCustomKey.PreviewKeyDown += handler;
        }

        private string BuildKeyComboString(Key key)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (Keyboard.IsKeyDown(Key.LeftCtrl)) sb.Append("Ctrl+");
            else if (Keyboard.IsKeyDown(Key.RightCtrl)) sb.Append("RCtrl+");
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) sb.Append("Ctrl+");

            if (Keyboard.IsKeyDown(Key.LeftAlt)) sb.Append("Alt+");
            else if (Keyboard.IsKeyDown(Key.RightAlt)) sb.Append("RAlt+");
            else if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) sb.Append("Alt+");

            if (Keyboard.IsKeyDown(Key.LeftShift)) sb.Append("Shift+");
            else if (Keyboard.IsKeyDown(Key.RightShift)) sb.Append("RShift+");
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) sb.Append("Shift+");

            if (Keyboard.IsKeyDown(Key.LWin)) sb.Append("Win+");
            else if (Keyboard.IsKeyDown(Key.RWin)) sb.Append("RWin+");
            else if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) sb.Append("Win+");

            string keyName = key.ToString();

            if (key >= Key.D0 && key <= Key.D9) keyName = key.ToString().Substring(1);
            else if (key >= Key.NumPad0 && key <= Key.NumPad9) keyName = key.ToString().Substring(6);
            else
            {
                switch (key)
                {
                    case Key.OemPlus: keyName = "+"; break;
                    case Key.OemMinus: keyName = "-"; break;
                    case Key.Oem1: keyName = ";"; break;
                    case Key.Oem2: keyName = "/"; break;
                    case Key.Oem3: keyName = "~"; break;
                    case Key.Oem4: keyName = "["; break;
                    case Key.Oem5: keyName = "\\"; break;
                    case Key.Oem6: keyName = "]"; break;
                    case Key.Oem7: keyName = "'"; break;
                    case Key.OemComma: keyName = ","; break;
                    case Key.OemPeriod: keyName = "."; break;
                }
            }

            sb.Append(keyName);
            return sb.ToString();
        }

        private void ShowKeyHelperMenu(GestureSlotUI slot)
        {
            ContextMenu menu = new ContextMenu();

            string[] commonKeys = new string[] {
                "Ctrl+ (通用Ctrl)", "LCtrl+ (左Ctrl)", "RCtrl+ (右Ctrl)",
                "Alt+ (通用Alt)", "LAlt+ (左Alt)", "RAlt+ (右Alt/AltGr)",
                "Shift+ (通用Shift)", "LShift+ (左Shift)", "RShift+ (右Shift)",
                "Win+ (Windows键)",
                "Ctrl+Z (撤销)", "Ctrl+Y (重做)", "Ctrl+C (复制)", "Ctrl+V (粘贴)", "Ctrl+A (全选)", "Ctrl+X (剪切)",
                "Ctrl++ (放大/Zoom In)", "Ctrl+- (缩小/Zoom Out)",
                "Win+Shift+S (截屏)", "Win+D (桌面)", "Win+V (剪贴板)", "Alt+Tab (切换)", "Alt+F4 (关闭)", "Ctrl+Shift+Esc (任务管理器)",
                "F5 (刷新)", "F11 (全屏)", "Esc", "Enter", "Backspace", "Delete", "Tab", "Space"
            };

            foreach (string k in commonKeys)
            {
                string keyVal = k.Contains(" ") ? k.Substring(0, k.IndexOf(' ')) : k;
                MenuItem item = new MenuItem { Header = k };
                item.Click += (s, e) =>
                {
                    if (keyVal.EndsWith("+"))
                    {
                        if (!slot.TxtCustomKey.Text.Contains(keyVal)) slot.TxtCustomKey.Text += keyVal;
                    }
                    else
                    {
                        slot.TxtCustomKey.Text = keyVal;
                    }
                    SetSlotToCustomKey(slot);
                    UpdateSlotLiveTag(slot);
                };
                menu.Items.Add(item);
            }

            menu.PlacementTarget = slot.BtnKeyHelper;
            menu.IsOpen = true;
        }

        private ScrollViewer BuildAppearanceTab()
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(20) };

            // Swipe Threshold
            Grid gSwipe = new Grid { Margin = new Thickness(0, 6, 0, 4) };
            gSwipe.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gSwipe.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            TextBlock lblSwipe = new TextBlock { Text = "滑动手势触发距离 (滑动判定灵敏度):", FontWeight = FontWeights.Bold };
            _txtSwipeThresholdVal = new TextBlock { Text = "25 px (精准灵敏)", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), TextAlignment = TextAlignment.Right };
            gSwipe.Children.Add(lblSwipe);
            gSwipe.Children.Add(_txtSwipeThresholdVal);
            Grid.SetColumn(_txtSwipeThresholdVal, 1);

            _sliderSwipeThreshold = new Slider { Minimum = 15, Maximum = 65, Value = 25, TickFrequency = 5, IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 15) };
            _sliderSwipeThreshold.ValueChanged += (s, e) => {
                if (_txtSwipeThresholdVal != null)
                {
                    int v = (int)_sliderSwipeThreshold.Value;
                    string tag = v <= 20 ? "极灵敏" : (v >= 45 ? "大范围" : "精准灵敏");
                    _txtSwipeThresholdVal.Text = v + " px (" + tag + ")";
                }
            };

            sp.Children.Add(gSwipe);
            sp.Children.Add(_sliderSwipeThreshold);

            // Long Press Threshold
            Grid gHold = new Grid { Margin = new Thickness(0, 6, 0, 4) };
            gHold.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gHold.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            TextBlock lblHold = new TextBlock { Text = "长按触发判定时间 (物理按住判定时间):", FontWeight = FontWeights.Bold };
            _txtLongPressThresholdVal = new TextBlock { Text = "350 ms (自然跟手)", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), TextAlignment = TextAlignment.Right };
            gHold.Children.Add(lblHold);
            gHold.Children.Add(_txtLongPressThresholdVal);
            Grid.SetColumn(_txtLongPressThresholdVal, 1);

            _sliderLongPressThreshold = new Slider { Minimum = 150, Maximum = 750, Value = 350, TickFrequency = 50, IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 15) };
            _sliderLongPressThreshold.ValueChanged += (s, e) => {
                if (_txtLongPressThresholdVal != null)
                {
                    int v = (int)_sliderLongPressThreshold.Value;
                    string tag = v <= 250 ? "极速响应" : (v >= 500 ? "深度长按" : "自然跟手");
                    _txtLongPressThresholdVal.Text = v + " ms (" + tag + ")";
                }
            };

            sp.Children.Add(gHold);
            sp.Children.Add(_sliderLongPressThreshold);

            // Size
            Grid gSize = new Grid { Margin = new Thickness(0, 6, 0, 4) };
            gSize.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gSize.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            TextBlock lblSize = new TextBlock { Text = "悬浮球大小 (像素直径):", FontWeight = FontWeights.Bold };
            _txtSizeVal = new TextBlock { Text = "64 px", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), TextAlignment = TextAlignment.Right };
            gSize.Children.Add(lblSize);
            gSize.Children.Add(_txtSizeVal);
            Grid.SetColumn(_txtSizeVal, 1);

            _sliderSize = new Slider { Minimum = 45, Maximum = 95, Value = 64, TickFrequency = 5, IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 15) };
            _sliderSize.ValueChanged += (s, e) => { if (_txtSizeVal != null) _txtSizeVal.Text = (int)_sliderSize.Value + " px"; };

            sp.Children.Add(gSize);
            sp.Children.Add(_sliderSize);

            // Normal Opacity
            Grid gNorm = new Grid { Margin = new Thickness(0, 6, 0, 4) };
            gNorm.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gNorm.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            TextBlock lblNorm = new TextBlock { Text = "平时静止不透明度:", FontWeight = FontWeights.Bold };
            _txtNormalOpacityVal = new TextBlock { Text = "85 %", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), TextAlignment = TextAlignment.Right };
            gNorm.Children.Add(lblNorm);
            gNorm.Children.Add(_txtNormalOpacityVal);
            Grid.SetColumn(_txtNormalOpacityVal, 1);

            _sliderNormalOpacity = new Slider { Minimum = 30, Maximum = 100, Value = 85, TickFrequency = 5, IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 15) };
            _sliderNormalOpacity.ValueChanged += (s, e) => { if (_txtNormalOpacityVal != null) _txtNormalOpacityVal.Text = (int)_sliderNormalOpacity.Value + " %"; };

            sp.Children.Add(gNorm);
            sp.Children.Add(_sliderNormalOpacity);

            // Active Opacity
            Grid gAct = new Grid { Margin = new Thickness(0, 6, 0, 4) };
            gAct.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gAct.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            TextBlock lblAct = new TextBlock { Text = "触碰/激活不透明度:", FontWeight = FontWeights.Bold };
            _txtActiveOpacityVal = new TextBlock { Text = "100 %", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), TextAlignment = TextAlignment.Right };
            gAct.Children.Add(lblAct);
            gAct.Children.Add(_txtActiveOpacityVal);
            Grid.SetColumn(_txtActiveOpacityVal, 1);

            _sliderActiveOpacity = new Slider { Minimum = 50, Maximum = 100, Value = 100, TickFrequency = 5, IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 15) };
            _sliderActiveOpacity.ValueChanged += (s, e) => { if (_txtActiveOpacityVal != null) _txtActiveOpacityVal.Text = (int)_sliderActiveOpacity.Value + " %"; };

            sp.Children.Add(gAct);
            sp.Children.Add(_sliderActiveOpacity);

            // Idle Opacity (Deep Fade)
            Grid gIdle = new Grid { Margin = new Thickness(0, 6, 0, 4) };
            gIdle.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gIdle.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            TextBlock lblIdle = new TextBlock { Text = "长时间未操作淡化不透明度 (减少遮挡):", FontWeight = FontWeights.Bold };
            _txtIdleOpacityVal = new TextBlock { Text = "22 %", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), TextAlignment = TextAlignment.Right };
            gIdle.Children.Add(lblIdle);
            gIdle.Children.Add(_txtIdleOpacityVal);
            Grid.SetColumn(_txtIdleOpacityVal, 1);

            _sliderIdleOpacity = new Slider { Minimum = 10, Maximum = 50, Value = 22, TickFrequency = 2, IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 15) };
            _sliderIdleOpacity.ValueChanged += (s, e) => { if (_txtIdleOpacityVal != null) _txtIdleOpacityVal.Text = (int)_sliderIdleOpacity.Value + " %"; };

            sp.Children.Add(gIdle);
            sp.Children.Add(_sliderIdleOpacity);

            // Idle Timeout
            Grid gIdleTime = new Grid { Margin = new Thickness(0, 6, 0, 4) };
            gIdleTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gIdleTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            TextBlock lblIdleTime = new TextBlock { Text = "无操作自动淡化时间 (秒):", FontWeight = FontWeights.Bold };
            _txtIdleTimeoutVal = new TextBlock { Text = "5 秒", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), TextAlignment = TextAlignment.Right };
            gIdleTime.Children.Add(lblIdleTime);
            gIdleTime.Children.Add(_txtIdleTimeoutVal);
            Grid.SetColumn(_txtIdleTimeoutVal, 1);

            _sliderIdleTimeout = new Slider { Minimum = 2, Maximum = 15, Value = 5, TickFrequency = 1, IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 15) };
            _sliderIdleTimeout.ValueChanged += (s, e) => { if (_txtIdleTimeoutVal != null) _txtIdleTimeoutVal.Text = (int)_sliderIdleTimeout.Value + " 秒"; };

            sp.Children.Add(gIdleTime);
            sp.Children.Add(_sliderIdleTimeout);

            // Checkboxes
            _chkShowHUD = new CheckBox { Content = "长按/滑动时扩展四扇区圆盘快捷轮盘 (Show 4-Sector Radial Menu)", IsChecked = true, Margin = new Thickness(0, 6, 0, 6) };
            _chkAutoStart = new CheckBox { Content = "开机随 Windows 自动启动 (Launch at Startup)", Margin = new Thickness(0, 6, 0, 6) };

            sp.Children.Add(_chkShowHUD);
            sp.Children.Add(_chkAutoStart);

            return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private ScrollViewer BuildAboutTab()
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(20) };

            StackPanel spHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            try
            {
                string pngPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.png");
                if (System.IO.File.Exists(pngPath))
                {
                    Image appImg = new Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(pngPath, UriKind.Absolute)),
                        Width = 52,
                        Height = 52,
                        Margin = new Thickness(0, 0, 14, 0)
                    };
                    spHeader.Children.Add(appImg);
                }
            }
            catch { }

            StackPanel spTitle = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock tbTitle = new TextBlock { Text = "TouchAssistBall 触屏悬浮球", FontSize = 17, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) };
            TextBlock tbSub = new TextBlock { Text = "Windows 触控桌面辅助工具 • 极简流体设计", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)), Margin = new Thickness(0, 3, 0, 0) };
            spTitle.Children.Add(tbTitle);
            spTitle.Children.Add(tbSub);
            spHeader.Children.Add(spTitle);
            sp.Children.Add(spHeader);

            TextBlock tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13.5,
                LineHeight = 22,
                Text =
                    "【触屏桌面悬浮球 TouchAssist 使用指南】\n\n" +
                    "1. 全功能自由按键映射（支持区分左右修饰键）\n" +
                    "   • 默认支持通用 Ctrl/Alt/Shift/Win，也明确支持 LCtrl/RCtrl/LAlt/RAlt(AltGr)/LShift/RShift 等精确左右按键绑定。\n" +
                    "   • 设置时右上角实时显示对应功能的生效标签。\n\n" +
                    "2. 移动悬浮球位置（双击并在第 2 下长按，或右键菜单一键移动/重置）\n" +
                    "   • 快速轻点第 1 下，紧接着点下第 2 下按住 0.2 秒变为 ✥ 移动十字，即可平稳拖拽小球位置。\n" +
                    "   • 也可随时右键点击悬浮球，选择【✥ 拖动悬浮球位置】或【📍 重置位置到屏幕右侧】。\n\n" +
                    "3. 自动闲置淡化透明（极度省心防遮挡）\n" +
                    "   • 停止触碰 5 秒后，悬浮球自动淡化至极低不透明度（约 22%），几乎不挡视线；手指一碰瞬间唤醒！\n\n" +
                    "4. 智能单击（Tap）\n" +
                    "   • 文本框内：悬浮球显示 ⌫ 标识，轻触即退格（或自定义按键），长按连续快速退格，绝不抢夺光标焦点。\n" +
                    "   • 非文本框：单触默认静默无动作（杜绝误触）。\n\n" +
                    "5. 长按绽放四扇区圆盘快捷轮盘（Radial Pie Menu & Gestures）\n" +
                    "   • 长按小球（或向外滑动）将流畅绽放为一个大圆盘，优雅划分为四个扇形区域：\n" +
                    "   • 👆 上扇区：对应上滑快捷键（默认：回车 / 语音输入）\n" +
                    "   • 👇 下扇区：对应下滑快捷键（默认：系统截屏）\n" +
                    "   • 👈 左扇区：对应左滑快捷键（默认：复制 Ctrl+C）\n" +
                    "   • 👉 右扇区：对应右滑快捷键（默认：粘贴 Ctrl+V）\n" +
                    "   • 手指划入扇区发光高亮，抬手即刻触发对应操作；若不想触发，中途拉回中心虚线圆环松手即可安全取消。"
            };

            sp.Children.Add(tb);
            return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private void LoadConfigToUI()
        {
            LoadSlotConfig(_slotClick, _config.ClickAction, _config.CustomClickKey);
            LoadSlotConfig(_slotLongPress, _config.LongPressAction, _config.CustomLongPressKey);
            LoadSlotConfig(_slotSwipeLeft, _config.SwipeLeftAction, _config.CustomSwipeLeftKey);
            LoadSlotConfig(_slotSwipeRight, _config.SwipeRightAction, _config.CustomSwipeRightKey);
            LoadSlotConfig(_slotSwipeUp, _config.SwipeUpAction, _config.CustomSwipeUpKey);
            LoadSlotConfig(_slotSwipeDown, _config.SwipeDownAction, _config.CustomSwipeDownKey);

            _sliderSwipeThreshold.Value = Math.Max(_sliderSwipeThreshold.Minimum, Math.Min(_sliderSwipeThreshold.Maximum, _config.SwipeThreshold));
            int sv = (int)_sliderSwipeThreshold.Value;
            _txtSwipeThresholdVal.Text = sv + " px (" + (sv <= 20 ? "极灵敏" : (sv >= 45 ? "大范围" : "精准灵敏")) + ")";

            _sliderLongPressThreshold.Value = Math.Max(_sliderLongPressThreshold.Minimum, Math.Min(_sliderLongPressThreshold.Maximum, _config.LongPressThresholdMs));
            int hv = (int)_sliderLongPressThreshold.Value;
            _txtLongPressThresholdVal.Text = hv + " ms (" + (hv <= 250 ? "极速响应" : (hv >= 500 ? "深度长按" : "自然跟手")) + ")";

            _sliderSize.Value = Math.Max(_sliderSize.Minimum, Math.Min(_sliderSize.Maximum, _config.BallSize));
            _txtSizeVal.Text = (int)_sliderSize.Value + " px";

            _sliderNormalOpacity.Value = (int)(_config.NormalOpacity * 100);
            _txtNormalOpacityVal.Text = (int)_sliderNormalOpacity.Value + " %";

            _sliderActiveOpacity.Value = (int)(_config.ActiveOpacity * 100);
            _txtActiveOpacityVal.Text = (int)_sliderActiveOpacity.Value + " %";

            _sliderIdleOpacity.Value = (int)(_config.IdleOpacity * 100);
            _txtIdleOpacityVal.Text = (int)_sliderIdleOpacity.Value + " %";

            _sliderIdleTimeout.Value = Math.Max(2, Math.Min(15, _config.IdleTimeoutSec));
            _txtIdleTimeoutVal.Text = (int)_sliderIdleTimeout.Value + " 秒";

            _chkShowHUD.IsChecked = _config.ShowDirectionHUD;
            _chkAutoStart.IsChecked = _config.StartWithWindows;
        }

        private void LoadSlotConfig(GestureSlotUI slot, ActionType act, string customKey)
        {
            for (int i = 0; i < slot.CbAction.Items.Count; i++)
            {
                ActionItem item = slot.CbAction.Items[i] as ActionItem;
                if (item != null && item.Action == act)
                {
                    slot.CbAction.SelectedIndex = i;
                    break;
                }
            }

            slot.TxtCustomKey.Text = customKey ?? "";
            slot.CustomKeyPanel.Visibility = (act == ActionType.CustomKey) ? Visibility.Visible : Visibility.Collapsed;
            UpdateSlotLiveTag(slot);
        }

        private void ResetDefaults()
        {
            LoadSlotConfig(_slotClick, ActionType.None, "");
            LoadSlotConfig(_slotLongPress, ActionType.HoldCurrentKey, "");
            LoadSlotConfig(_slotSwipeLeft, ActionType.Copy, "");
            LoadSlotConfig(_slotSwipeRight, ActionType.Paste, "");
            LoadSlotConfig(_slotSwipeUp, ActionType.VoiceTyping, "RAlt");
            LoadSlotConfig(_slotSwipeDown, ActionType.Screenshot, "");

            _sliderSwipeThreshold.Value = 25;
            _sliderLongPressThreshold.Value = 350;
            _sliderSize.Value = 64;
            _sliderNormalOpacity.Value = 85;
            _sliderActiveOpacity.Value = 100;
            _sliderIdleOpacity.Value = 22;
            _sliderIdleTimeout.Value = 5;
            _chkShowHUD.IsChecked = true;
            _chkAutoStart.IsChecked = false;

            _txtStatusBanner.Text = "🔄 已恢复为默认设置！";
            _txtStatusBanner.Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        }

        private void SaveUIToConfig()
        {
            _config.ClickAction = GetSlotAction(_slotClick);
            _config.CustomClickKey = _slotClick.TxtCustomKey.Text.Trim();

            _config.LongPressAction = GetSlotAction(_slotLongPress);
            _config.CustomLongPressKey = _slotLongPress.TxtCustomKey.Text.Trim();

            _config.SwipeLeftAction = GetSlotAction(_slotSwipeLeft);
            _config.CustomSwipeLeftKey = _slotSwipeLeft.TxtCustomKey.Text.Trim();

            _config.SwipeRightAction = GetSlotAction(_slotSwipeRight);
            _config.CustomSwipeRightKey = _slotSwipeRight.TxtCustomKey.Text.Trim();

            _config.SwipeUpAction = GetSlotAction(_slotSwipeUp);
            _config.CustomSwipeUpKey = _slotSwipeUp.TxtCustomKey.Text.Trim();

            _config.SwipeDownAction = GetSlotAction(_slotSwipeDown);
            _config.CustomSwipeDownKey = _slotSwipeDown.TxtCustomKey.Text.Trim();

            _config.SwipeThreshold = (int)_sliderSwipeThreshold.Value;
            _config.LongPressThresholdMs = (int)_sliderLongPressThreshold.Value;
            _config.BallSize = (int)_sliderSize.Value;
            _config.NormalOpacity = (float)(_sliderNormalOpacity.Value / 100.0);
            _config.ActiveOpacity = (float)(_sliderActiveOpacity.Value / 100.0);
            _config.IdleOpacity = (float)(_sliderIdleOpacity.Value / 100.0);
            _config.IdleTimeoutSec = (int)_sliderIdleTimeout.Value;
            _config.ShowDirectionHUD = _chkShowHUD.IsChecked == true;
            _config.StartWithWindows = _chkAutoStart.IsChecked == true;

            // Instant prompt to user
            MessageBox.Show("✅ 快捷键与手势设置已成功保存并立即生效！", "设置已保存", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private ActionType GetSlotAction(GestureSlotUI slot)
        {
            ActionItem item = slot.CbAction.SelectedItem as ActionItem;
            if (item != null)
            {
                // Safety: If custom key text exists and custom key panel is visible, always save as CustomKey
                if (!string.IsNullOrWhiteSpace(slot.TxtCustomKey.Text) && slot.CustomKeyPanel.Visibility == Visibility.Visible)
                {
                    return ActionType.CustomKey;
                }
                return item.Action;
            }
            return slot.DefaultAction;
        }
    }
}
