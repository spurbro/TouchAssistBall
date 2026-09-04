using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TouchAssistBall
{
    public class FloatingBallWindow : Window
    {
        private enum GestureMode
        {
            Idle,
            Touching,   // Normal touch down (evaluating tap vs swipe)
            Sliding,    // User is dragging in one of the 4 swipe directions
            Moving      // Double-tap & held: User is dragging the ball position
        }

        private enum SlideDirection
        {
            None,
            Up,
            Down,
            Left,
            Right
        }

        private readonly AppConfig _config;
        private GestureMode _mode = GestureMode.Idle;
        private SlideDirection _currentDirection = SlideDirection.None;

        private Point _touchStartScreenPt;
        private Point _touchOffsetInWindow;
        private Point _lastPointerScreenPt;
        private DateTime _touchStartTime;

        // Double-Tap and Hold Detection Variables
        private DateTime _lastTapReleaseTime = DateTime.MinValue;
        private Point _lastTapScreenPt;
        private bool _isSecondTapCandidate = false;
        private readonly DispatcherTimer _doubleTapHoldTimer;
        private readonly DispatcherTimer _singleTapTimer;

        // Long-Press Physical Key Hold Simulation
        private readonly DispatcherTimer _longPressTimer;
        private bool _isPhysicalHolding = false;
        private bool _isLongPressTriggered = false;
        private string _activeHoldLabel = "";

        // Debounce guard against touch-lift bounce generating repeated click actions
        private DateTime _lastClickExecutedTime = DateTime.MinValue;
        private const int CLICK_DEBOUNCE_MS = 40;

        // Timers
        private readonly DispatcherTimer _inputCheckTimer;
        private readonly DispatcherTimer _feedbackTimer;
        private readonly DispatcherTimer _topmostKeeperTimer;

        // Inactivity Deep Fade & Visibility Control
        private readonly DispatcherTimer _idleFadeTimer;
        private DateTime _lastInteractionTime = DateTime.UtcNow;
        private bool _isUserHidden = false;

        private bool _isInputFocused = false;
        private bool _isActionFeedback = false;
        private string _feedbackText = "";

        // UI Visual Components
        private Canvas _rootCanvas;
        private Grid _ballGrid;
        private ScaleTransform _ballScale;
        private Ellipse _ballEllipse;
        private Ellipse _ballBorder;
        private Ellipse _ballInnerRing;
        private TextBlock _ballIcon;

        private Border _badgeUp;
        private Border _badgeDown;
        private Border _badgeLeft;
        private Border _badgeRight;
        private TextBlock _txtUp;
        private TextBlock _txtDown;
        private TextBlock _txtLeft;
        private TextBlock _txtRight;

        private Ellipse _centerCancelZone;

        // Swipe Distance Tuning
        private const double HUD_EXT = 95.0;

        private double CancelRadiusPx
        {
            get { return Math.Max(20.0, _config.BallSize * 0.4); }
        }

        // Honor user-configured swipe threshold (config.json "SwipeThreshold")
        private double SwipeThresholdPx
        {
            get { return Math.Max(12.0, _config.SwipeThreshold); }
        }

        // Helper to query current physical screen working area for multi-monitor support
        private Rect GetCurrentScreenWorkingArea()
        {
            try
            {
                int centerX = (int)(Left + HUD_EXT + _config.BallSize / 2);
                int centerY = (int)(Top + HUD_EXT + _config.BallSize / 2);
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(centerX, centerY));
                if (screen != null)
                {
                    return new Rect(screen.WorkingArea.Left, screen.WorkingArea.Top, screen.WorkingArea.Width, screen.WorkingArea.Height);
                }
            }
            catch { }
            return new Rect(0, 0, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
        }

        // Convert a window-relative WPF point into VIRTUAL SCREEN space in DIP units.
        private Point ToVirtualScreenPt(Point windowRelativePt)
        {
            return new Point(Left + windowRelativePt.X, Top + windowRelativePt.Y);
        }

        public FloatingBallWindow(AppConfig config)
        {
            _config = config;

            // Window Setup: Null background allows transparent areas to pass clicks through
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = null;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;

            // Disable Windows native press-and-hold right-click system gesture
            Stylus.SetIsPressAndHoldEnabled(this, false);
            Stylus.SetIsFlicksEnabled(this, false);
            Stylus.SetIsTapFeedbackEnabled(this, false);
            Stylus.SetIsTouchFeedbackEnabled(this, false);

            double ballSize = _config.BallSize;
            Width = ballSize + HUD_EXT * 2;
            Height = ballSize + HUD_EXT * 2;

            double screenW = SystemParameters.WorkArea.Width;
            double screenH = SystemParameters.WorkArea.Height;
            double defX = screenW - ballSize - 75;
            double defY = (screenH / 2) - (ballSize / 2);

            if (_config.PosX != -1 && _config.PosY != -1)
            {
                Left = _config.PosX - HUD_EXT;
                Top = _config.PosY - HUD_EXT;
                ClampAndSaveCurrentPosition();
            }
            else
            {
                Left = defX - HUD_EXT;
                Top = defY - HUD_EXT;
                _config.PosX = (int)defX;
                _config.PosY = (int)defY;
                _config.Save();
            }

            Opacity = _config.NormalOpacity;

            BuildUI();

            // 1. Double-Tap and Hold Timer (180ms on second tap enters Move Mode)
            _doubleTapHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
            _doubleTapHoldTimer.Tick += OnDoubleTapHoldTimerTick;

            // 2. Single-Tap Resolution Timer (decouples click action from double-tap-and-hold move)
            _singleTapTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _singleTapTimer.Tick += OnSingleTapTimerTick;

            // 2b. Long-Press Timer (Physically holds keys until pointer lift)
            _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(150, _config.LongPressThresholdMs)) };
            _longPressTimer.Tick += OnLongPressTimerTick;

            // 3. Input Field Focus Detection Timer
            _inputCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _inputCheckTimer.Tick += OnInputCheckTimerTick;
            _inputCheckTimer.Start();

            // 4. Action Feedback Timer
            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _feedbackTimer.Tick += (s, e) =>
            {
                _isActionFeedback = false;
                _feedbackTimer.Stop();
                UpdateBallVisuals();
            };

            // 5. Topmost Keeper Timer
            _topmostKeeperTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _topmostKeeperTimer.Tick += OnTopmostKeeperTick;
            _topmostKeeperTimer.Start();

            // 6. Inactivity Deep Fade Timer (auto fades to IdleOpacity after 5s)
            _idleFadeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _idleFadeTimer.Tick += OnIdleFadeTimerTick;
            _idleFadeTimer.Start();

            // Touch & Mouse Event Handlers on _ballGrid
            _ballGrid.PreviewTouchDown += OnBallTouchDown;
            _ballGrid.PreviewTouchMove += OnBallTouchMove;
            _ballGrid.PreviewTouchUp += OnBallTouchUp;

            _ballGrid.PreviewMouseDown += OnBallMouseDown;
            _ballGrid.PreviewMouseMove += OnBallMouseMove;
            _ballGrid.PreviewMouseUp += OnBallMouseUp;

            ContextMenu = CreateContextMenu();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(hwnd);
            if (source != null)
            {
                source.AddHook(WndProc);
            }

            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                exStyle | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST);

            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            KeepWindowOnTop();
        }

        protected override void OnClosed(EventArgs e)
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            base.OnClosed(e);
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AppLogger.Log("DisplaySettingsChanged detected: adjusting ball position to screen bounds.");
                    ClampAndSaveCurrentPosition();
                }), DispatcherPriority.Normal);
            }
            catch { }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_MOUSEACTIVATE)
            {
                handled = true;
                return (IntPtr)NativeMethods.MA_NOACTIVATE;
            }

            if (msg == NativeMethods.WM_TABLET_QUERYSYSTEMGESTURESTATUS)
            {
                handled = true;
                return (IntPtr)(NativeMethods.TABLET_DISABLE_PRESSANDHOLD |
                                NativeMethods.TABLET_DISABLE_PENTAPFEEDBACK |
                                NativeMethods.TABLET_DISABLE_PENBARRELFEEDBACK |
                                NativeMethods.TABLET_DISABLE_FLICKS);
            }

            return IntPtr.Zero;
        }

        private void OnTopmostKeeperTick(object sender, EventArgs e)
        {
            if (_isUserHidden) return; // Do not revive if user explicitly hid the ball!

            if (Visibility != Visibility.Visible)
            {
                Visibility = Visibility.Visible;
            }
            KeepWindowOnTop();
        }

        private void OnIdleFadeTimerTick(object sender, EventArgs e)
        {
            if (_mode != GestureMode.Idle || _isActionFeedback || _isPhysicalHolding || _isUserHidden)
            {
                return;
            }

            double elapsedSec = (DateTime.UtcNow - _lastInteractionTime).TotalSeconds;
            if (elapsedSec >= _config.IdleTimeoutSec && Opacity > _config.IdleOpacity + 0.01)
            {
                // Smoothly fade to idle opacity to minimize visual distraction
                DoubleAnimation fadeAnim = new DoubleAnimation(_config.IdleOpacity, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                BeginAnimation(OpacityProperty, fadeAnim);
            }
        }

        private void KeepWindowOnTop()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
                }
            }
            catch { }
        }

        private void BuildUI()
        {
            double ballSize = _config.BallSize;

            _rootCanvas = new Canvas
            {
                Width = Width,
                Height = Height,
                Background = null,
                ClipToBounds = false
            };

            _ballScale = new ScaleTransform(1.0, 1.0, ballSize / 2, ballSize / 2);
            _ballGrid = new Grid
            {
                Width = ballSize,
                Height = ballSize,
                Cursor = Cursors.Hand,
                RenderTransform = _ballScale,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            Canvas.SetLeft(_ballGrid, HUD_EXT);
            Canvas.SetTop(_ballGrid, HUD_EXT);

            _ballEllipse = new Ellipse
            {
                Width = ballSize,
                Height = ballSize,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 14,
                    ShadowDepth = 3,
                    Opacity = 0.55
                }
            };

            _ballBorder = new Ellipse
            {
                Width = ballSize,
                Height = ballSize,
                StrokeThickness = 3.0
            };

            _ballInnerRing = new Ellipse
            {
                Width = ballSize - 12,
                Height = ballSize - 12,
                Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                StrokeThickness = 1.2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _ballIcon = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = ballSize * 0.38
            };

            _ballGrid.Children.Add(_ballEllipse);
            _ballGrid.Children.Add(_ballBorder);
            _ballGrid.Children.Add(_ballInnerRing);
            _ballGrid.Children.Add(_ballIcon);

            double cancelRadius = CancelRadiusPx;
            _centerCancelZone = new Ellipse
            {
                Width = cancelRadius * 2,
                Height = cancelRadius * 2,
                Stroke = new SolidColorBrush(Color.FromArgb(140, 148, 163, 184)),
                StrokeThickness = 1.8,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                Opacity = 0,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(_centerCancelZone, HUD_EXT + ballSize / 2 - cancelRadius);
            Canvas.SetTop(_centerCancelZone, HUD_EXT + ballSize / 2 - cancelRadius);

            _badgeUp = CreateDirectionBadge(out _txtUp);
            _badgeDown = CreateDirectionBadge(out _txtDown);
            _badgeLeft = CreateDirectionBadge(out _txtLeft);
            _badgeRight = CreateDirectionBadge(out _txtRight);

            PositionDirectionBadges();

            _rootCanvas.Children.Add(_centerCancelZone);
            _rootCanvas.Children.Add(_badgeUp);
            _rootCanvas.Children.Add(_badgeDown);
            _rootCanvas.Children.Add(_badgeLeft);
            _rootCanvas.Children.Add(_badgeRight);
            _rootCanvas.Children.Add(_ballGrid);

            Content = _rootCanvas;
            UpdateBallVisuals();
        }

        private Border CreateDirectionBadge(out TextBlock tb)
        {
            tb = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 13.0,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Border b = new Border
            {
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14, 7, 14, 7),
                Background = new SolidColorBrush(Color.FromArgb(235, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, 148, 163, 184)),
                BorderThickness = new Thickness(1.8),
                Opacity = 0,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 10,
                    ShadowDepth = 2,
                    Opacity = 0.5
                },
                Child = tb
            };

            return b;
        }

        private void PositionDirectionBadges()
        {
            double ballSize = _config.BallSize;
            double cx = HUD_EXT + ballSize / 2;
            double cy = HUD_EXT + ballSize / 2;
            double r = ballSize / 2 + 48;

            _txtUp.Text = ActionExecutor.GetActionShortLabel(_config.SwipeUpAction, _config.CustomSwipeUpKey);
            _txtDown.Text = ActionExecutor.GetActionShortLabel(_config.SwipeDownAction, _config.CustomSwipeDownKey);
            _txtLeft.Text = ActionExecutor.GetActionShortLabel(_config.SwipeLeftAction, _config.CustomSwipeLeftKey);
            _txtRight.Text = ActionExecutor.GetActionShortLabel(_config.SwipeRightAction, _config.CustomSwipeRightKey);

            _badgeUp.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _badgeDown.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _badgeLeft.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _badgeRight.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Canvas.SetLeft(_badgeUp, cx - _badgeUp.DesiredSize.Width / 2);
            Canvas.SetTop(_badgeUp, cy - r - _badgeUp.DesiredSize.Height / 2);

            Canvas.SetLeft(_badgeDown, cx - _badgeDown.DesiredSize.Width / 2);
            Canvas.SetTop(_badgeDown, cy + r - _badgeDown.DesiredSize.Height / 2);

            Canvas.SetLeft(_badgeLeft, cx - r - _badgeLeft.DesiredSize.Width / 2);
            Canvas.SetTop(_badgeLeft, cy - _badgeLeft.DesiredSize.Height / 2);

            Canvas.SetLeft(_badgeRight, cx + r - _badgeRight.DesiredSize.Width / 2);
            Canvas.SetTop(_badgeRight, cy - _badgeRight.DesiredSize.Height / 2);
        }

        private void UpdateBallVisuals()
        {
            if (_isActionFeedback)
            {
                _ballEllipse.Fill = new LinearGradientBrush(
                    Color.FromArgb(255, 16, 185, 129),
                    Color.FromArgb(255, 5, 150, 105), 65);
                _ballBorder.Stroke = new SolidColorBrush(Color.FromArgb(255, 209, 250, 229));
                _ballIcon.Text = _feedbackText;
                _ballIcon.FontSize = _config.BallSize * 0.22;
                return;
            }

            if (_isPhysicalHolding)
            {
                // Purple/Indigo with hold label during active physical key hold
                _ballEllipse.Fill = new LinearGradientBrush(
                    Color.FromArgb(255, 147, 51, 234),
                    Color.FromArgb(255, 109, 40, 217), 65);
                _ballBorder.Stroke = new SolidColorBrush(Color.FromArgb(255, 233, 213, 255));
                _ballIcon.Text = "✊" + (string.IsNullOrEmpty(_activeHoldLabel) ? "按住" : _activeHoldLabel);
                _ballIcon.FontSize = _ballIcon.Text.Length > 3 ? _config.BallSize * 0.18 : _config.BallSize * 0.28;
                return;
            }

            if (_mode == GestureMode.Moving)
            {
                // Amber/Orange with ✥ in Move mode (Double-tap & held)
                _ballEllipse.Fill = new LinearGradientBrush(
                    Color.FromArgb(255, 245, 158, 11),
                    Color.FromArgb(255, 217, 119, 6), 65);
                _ballBorder.Stroke = new SolidColorBrush(Color.FromArgb(255, 254, 243, 199));
                _ballIcon.Text = "✥";
                _ballIcon.FontSize = _config.BallSize * 0.42;
                return;
            }

            if (_isInputFocused)
            {
                _ballEllipse.Fill = new LinearGradientBrush(
                    Color.FromArgb(255, 14, 165, 233),
                    Color.FromArgb(255, 2, 132, 199), 65);
                _ballBorder.Stroke = new SolidColorBrush(Color.FromArgb(255, 224, 242, 254));
                _ballIcon.Text = "⌫";
                _ballIcon.FontSize = _config.BallSize * 0.40;
                return;
            }

            if (_mode == GestureMode.Touching || _mode == GestureMode.Sliding)
            {
                _ballEllipse.Fill = new LinearGradientBrush(
                    Color.FromArgb(255, 79, 70, 229),
                    Color.FromArgb(255, 67, 56, 202), 65);
                _ballBorder.Stroke = new SolidColorBrush(Color.FromArgb(255, 224, 231, 255));
                _ballIcon.Text = "●";
                _ballIcon.FontSize = _config.BallSize * 0.28;
                return;
            }

            // Normal Idle State
            _ballEllipse.Fill = new LinearGradientBrush(
                Color.FromArgb(255, 30, 41, 59),
                Color.FromArgb(255, 15, 23, 42), 65);
            _ballBorder.Stroke = new SolidColorBrush(Color.FromArgb(220, 203, 213, 225));
            _ballIcon.Text = "●";
            _ballIcon.FontSize = _config.BallSize * 0.28;
        }

        private void OnInputCheckTimerTick(object sender, EventArgs e)
        {
            if (_mode != GestureMode.Idle) return;

            bool currentInput = InputDetector.IsInInputField();
            if (currentInput != _isInputFocused)
            {
                _isInputFocused = currentInput;
                UpdateBallVisuals();
            }
        }

        private void OnDoubleTapHoldTimerTick(object sender, EventArgs e)
        {
            _doubleTapHoldTimer.Stop();
            if (_isSecondTapCandidate && _mode == GestureMode.Touching)
            {
                _isSecondTapCandidate = false;
                _mode = GestureMode.Moving;
                _currentDirection = SlideDirection.None;

                // Re-anchor the drag offset at the CURRENT pointer position so the ball
                // does not snap-jump by any drift accumulated since the second tap-down
                _touchStartScreenPt = _lastPointerScreenPt;
                _touchOffsetInWindow = new Point(_lastPointerScreenPt.X - Left, _lastPointerScreenPt.Y - Top);

                UpdateBallVisuals();
            }
        }

        private void OnLongPressTimerTick(object sender, EventArgs e)
        {
            _longPressTimer.Stop();
            if (_mode == GestureMode.Touching && !_isSecondTapCandidate)
            {
                _isLongPressTriggered = true;
                _isPhysicalHolding = true;

                bool inInput = InputDetector.IsInInputField(true);
                if (inInput)
                {
                    _activeHoldLabel = "退格";
                    AppLogger.Log("LongPress threshold reached in InputField -> StartHold Backspace");
                    ActionExecutor.StartHold(ActionType.Backspace);
                    UpdateBallVisuals();
                }
                else
                {
                    ActionType action = (_config.LongPressAction == ActionType.HoldCurrentKey) ? _config.ClickAction : _config.LongPressAction;
                    string customKey = (_config.LongPressAction == ActionType.HoldCurrentKey) ? _config.CustomClickKey : _config.CustomLongPressKey;

                    if (action != ActionType.None)
                    {
                        _activeHoldLabel = ActionExecutor.GetActionShortLabel(action, customKey);
                        AppLogger.Log("LongPress threshold reached -> StartHold action=" + action + " label=" + _activeHoldLabel);
                        ActionExecutor.StartHold(action, customKey);
                        UpdateBallVisuals();
                    }
                }
            }
        }

        private void OnSingleTapTimerTick(object sender, EventArgs e)
        {
            _singleTapTimer.Stop();
            if ((DateTime.UtcNow - _lastClickExecutedTime).TotalMilliseconds >= CLICK_DEBOUNCE_MS)
            {
                _lastClickExecutedTime = DateTime.UtcNow;
                ExecuteClickAction();
            }
            Opacity = _config.NormalOpacity;
            UpdateBallVisuals();
        }

        #region Native Touch & Mouse Event Handlers (With Synthesized Mouse Filter)

        private void OnBallTouchDown(object sender, TouchEventArgs e)
        {
            Point screenPt = ToVirtualScreenPt(e.GetTouchPoint(this).Position);
            HandlePointerDown(screenPt);
            _ballGrid.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void OnBallTouchMove(object sender, TouchEventArgs e)
        {
            if (_mode == GestureMode.Idle) return;
            Point screenPt = ToVirtualScreenPt(e.GetTouchPoint(this).Position);
            HandlePointerMove(screenPt);
            e.Handled = true;
        }

        private void OnBallTouchUp(object sender, TouchEventArgs e)
        {
            if (_mode == GestureMode.Idle) return;
            _ballGrid.ReleaseTouchCapture(e.TouchDevice);
            HandlePointerUp();
            e.Handled = true;
        }

        private void OnBallMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right) return;
            if (e.StylusDevice != null && e.StylusDevice.TabletDevice != null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch)
            {
                return; // Only ignore touch-synthesized mouse events! Allow real stylus pen!
            }

            Point posInWindow = e.GetPosition(this);
            Point screenPt = ToVirtualScreenPt(posInWindow);
            HandlePointerDown(screenPt);
            _ballGrid.CaptureMouse();
            e.Handled = true;
        }

        private void OnBallMouseMove(object sender, MouseEventArgs e)
        {
            if (_mode == GestureMode.Idle) return;
            if (e.StylusDevice != null && e.StylusDevice.TabletDevice != null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch)
            {
                return;
            }

            Point posInWindow = e.GetPosition(this);
            Point screenPt = ToVirtualScreenPt(posInWindow);
            HandlePointerMove(screenPt);
            e.Handled = true;
        }

        private void OnBallMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mode == GestureMode.Idle) return;
            if (e.StylusDevice != null && e.StylusDevice.TabletDevice != null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch)
            {
                return;
            }

            _ballGrid.ReleaseMouseCapture();
            HandlePointerUp();
            e.Handled = true;
        }

        #endregion

        #region Core Gesture State Machine (Zero-Drift 1:1 Move & Instant Swipe)

        private void HandlePointerDown(Point screenPt)
        {
            _longPressTimer.Stop();
            _isPhysicalHolding = false;
            _isLongPressTriggered = false;
            _activeHoldLabel = "";

            _mode = GestureMode.Touching;
            _currentDirection = SlideDirection.None;
            _touchStartScreenPt = screenPt;
            _lastPointerScreenPt = screenPt;
            _touchOffsetInWindow = new Point(screenPt.X - Left, screenPt.Y - Top);
            _touchStartTime = DateTime.UtcNow;
            _lastInteractionTime = DateTime.UtcNow;

            // Immediately wake up from idle fade to active opacity
            BeginAnimation(OpacityProperty, null);
            Opacity = _config.ActiveOpacity;

            double msSinceLastTap = (DateTime.UtcNow - _lastTapReleaseTime).TotalMilliseconds;
            double distFromLastTap = Math.Sqrt(Math.Pow(screenPt.X - _lastTapScreenPt.X, 2) + Math.Pow(screenPt.Y - _lastTapScreenPt.Y, 2));

            if (_singleTapTimer.IsEnabled || (msSinceLastTap < 300 && distFromLastTap < 40))
            {
                _singleTapTimer.Stop();
                _isSecondTapCandidate = true;
                _doubleTapHoldTimer.Start();
            }
            else
            {
                _isSecondTapCandidate = false;
                _longPressTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(150, _config.LongPressThresholdMs));
                _longPressTimer.Start();
            }

            _ballScale.ScaleX = 0.92;
            _ballScale.ScaleY = 0.92;

            UpdateBallVisuals();
        }

        private void HandlePointerMove(Point screenPt)
        {
            _lastPointerScreenPt = screenPt;

            // Fast-path: If in the 2nd tap of a double-tap, any deliberate movement immediately enters Moving mode
            if (_isSecondTapCandidate && _mode == GestureMode.Touching)
            {
                double dxMove = screenPt.X - _touchStartScreenPt.X;
                double dyMove = screenPt.Y - _touchStartScreenPt.Y;
                double distMove = Math.Sqrt(dxMove * dxMove + dyMove * dyMove);
                if (distMove >= 8.0)
                {
                    _longPressTimer.Stop();
                    _doubleTapHoldTimer.Stop();
                    _singleTapTimer.Stop();
                    _isSecondTapCandidate = false;
                    _mode = GestureMode.Moving;
                    _currentDirection = SlideDirection.None;
                    _touchStartScreenPt = screenPt;
                    _touchOffsetInWindow = new Point(screenPt.X - Left, screenPt.Y - Top);
                    UpdateBallVisuals();
                }
            }

            // 1. Move Mode (Double-tap & held / dragged): 1:1 Screen-Absolute Hardware Tracking
            if (_mode == GestureMode.Moving)
            {
                Rect wa = GetCurrentScreenWorkingArea();

                double newLeft = screenPt.X - _touchOffsetInWindow.X;
                double newTop = screenPt.Y - _touchOffsetInWindow.Y;

                Left = Math.Max(wa.Left - HUD_EXT + 10, Math.Min(wa.Right - Width + HUD_EXT - 10, newLeft));
                Top = Math.Max(wa.Top - HUD_EXT + 10, Math.Min(wa.Bottom - Height + HUD_EXT - 10, newTop));
                return;
            }

            // 2. Swipe Displacement Calculation from touch origin (screen coords, stable frame)
            double dx = screenPt.X - _touchStartScreenPt.X;
            double dy = screenPt.Y - _touchStartScreenPt.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (_mode == GestureMode.Touching && dist >= SwipeThresholdPx)
            {
                _longPressTimer.Stop();
                _doubleTapHoldTimer.Stop();
                _singleTapTimer.Stop();
                if (_isPhysicalHolding)
                {
                    ActionExecutor.ReleaseHold();
                    _isPhysicalHolding = false;
                    _isLongPressTriggered = false;
                }
                _isSecondTapCandidate = false;
                _mode = GestureMode.Sliding;
                AppLogger.Log(string.Format("Swipe START: dist={0:F1}px threshold={1:F1}", dist, SwipeThresholdPx));
                ShowHUD(true);
            }

            if (_mode == GestureMode.Sliding)
            {
                if (dist < CancelRadiusPx)
                {
                    _currentDirection = SlideDirection.None;
                    HighlightHUD(SlideDirection.None);
                    _centerCancelZone.Opacity = 1.0;
                }
                else
                {
                    _currentDirection = ResolveDirection(dx, dy);
                    HighlightHUD(_currentDirection);
                    _centerCancelZone.Opacity = 0.0;
                }
            }
        }

        private void HandlePointerUp()
        {
            _doubleTapHoldTimer.Stop();
            _longPressTimer.Stop();
            GestureMode endMode = _mode;
            SlideDirection endDir = _currentDirection;

            _mode = GestureMode.Idle;
            _currentDirection = SlideDirection.None;
            ShowHUD(false);
            _centerCancelZone.Opacity = 0.0;

            _ballScale.ScaleX = 1.0;
            _ballScale.ScaleY = 1.0;

            // Scenario A: Was in Physical Long-Press Key Hold Mode -> Release held keys (KeyUp)
            if (_isPhysicalHolding || _isLongPressTriggered)
            {
                _isPhysicalHolding = false;
                _isLongPressTriggered = false;
                _isSecondTapCandidate = false;
                _singleTapTimer.Stop();
                _lastTapReleaseTime = DateTime.MinValue;

                ActionExecutor.ReleaseHold();
                TriggerFeedback("松开");
                Opacity = _config.NormalOpacity;
                UpdateBallVisuals();
                return;
            }

            // Scenario B: Was in Move Mode -> Save position (never triggers click actions)
            if (endMode == GestureMode.Moving)
            {
                _isSecondTapCandidate = false;
                _singleTapTimer.Stop();
                _lastTapReleaseTime = DateTime.MinValue;

                ClampAndSaveCurrentPosition();

                _lastInteractionTime = DateTime.UtcNow;
                BeginAnimation(OpacityProperty, null);
                Opacity = _config.NormalOpacity;
                UpdateBallVisuals();
                return;
            }

            // Scenario C: Was in Swipe Mode -> Execute Swipe Action immediately (or cancel cleanly if in deadzone)
            if (endMode == GestureMode.Sliding)
            {
                _isSecondTapCandidate = false;
                _singleTapTimer.Stop();
                _lastTapReleaseTime = DateTime.MinValue;

                if (endDir != SlideDirection.None)
                {
                    AppLogger.Log("Swipe END: direction=" + endDir);
                    ExecuteSwipeAction(endDir);
                    TriggerFeedback(GetSwipeActionLabel(endDir));
                }
                else
                {
                    AppLogger.Log("Swipe CANCELLED: finger returned to center cancel deadzone");
                    _lastInteractionTime = DateTime.UtcNow;
                    BeginAnimation(OpacityProperty, null);
                    Opacity = _config.NormalOpacity;
                    UpdateBallVisuals();
                }
                return;
            }

            // Scenario D: Was a Quick Tap (< 350ms) -> Schedule clean physical single-tap execution
            if (endMode == GestureMode.Touching)
            {
                TimeSpan duration = DateTime.UtcNow - _touchStartTime;
                if (duration.TotalMilliseconds < Math.Max(250, _config.LongPressThresholdMs + 50))
                {
                    // Instant tap for input box backspace: 0 delay, crisp typing experience!
                    if (_isInputFocused)
                    {
                        _isSecondTapCandidate = false;
                        _singleTapTimer.Stop();
                        _lastClickExecutedTime = DateTime.UtcNow;
                        ExecuteClickAction();
                    }
                    else
                    {
                        _lastTapReleaseTime = DateTime.UtcNow;
                        _lastTapScreenPt = _touchStartScreenPt;

                        if (_isSecondTapCandidate)
                        {
                            // User tapped twice quickly without holding:
                            // Execute the pending 1st tap immediately, and schedule the 2nd tap!
                            _isSecondTapCandidate = false;
                            _singleTapTimer.Stop();
                            _lastClickExecutedTime = DateTime.UtcNow;
                            ExecuteClickAction();

                            _singleTapTimer.Interval = TimeSpan.FromMilliseconds(200);
                            _singleTapTimer.Start();
                        }
                        else
                        {
                            // Potential single-tap: start delayed single-tap resolution
                            _singleTapTimer.Stop();
                            _singleTapTimer.Interval = TimeSpan.FromMilliseconds(200);
                            _singleTapTimer.Start();
                        }
                    }
                }
            }

            _lastInteractionTime = DateTime.UtcNow;
            BeginAnimation(OpacityProperty, null);
            Opacity = _config.NormalOpacity;
            UpdateBallVisuals();
        }

        #endregion

        private SlideDirection ResolveDirection(double dx, double dy)
        {
            double angle = Math.Atan2(dy, dx) * (180.0 / Math.PI);

            if (angle >= -135 && angle < -45) return SlideDirection.Up;
            if (angle >= 45 && angle < 135) return SlideDirection.Down;
            if (angle >= -45 && angle < 45) return SlideDirection.Right;
            return SlideDirection.Left;
        }

        private void ShowHUD(bool show)
        {
            if (!_config.ShowDirectionHUD) return;
            PositionDirectionBadges();

            double targetOpacity = show ? 1.0 : 0.0;
            _badgeUp.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(120)));
            _badgeDown.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(120)));
            _badgeLeft.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(120)));
            _badgeRight.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(120)));
        }

        private void HighlightHUD(SlideDirection dir)
        {
            SetBadgeHighlight(_badgeUp, dir == SlideDirection.Up);
            SetBadgeHighlight(_badgeDown, dir == SlideDirection.Down);
            SetBadgeHighlight(_badgeLeft, dir == SlideDirection.Left);
            SetBadgeHighlight(_badgeRight, dir == SlideDirection.Right);
        }

        private void SetBadgeHighlight(Border badge, bool isHighlighted)
        {
            if (isHighlighted)
            {
                badge.Background = new SolidColorBrush(Color.FromArgb(245, 37, 99, 235));
                badge.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 147, 197, 253));
                badge.BorderThickness = new Thickness(2.5);
            }
            else
            {
                badge.Background = new SolidColorBrush(Color.FromArgb(230, 15, 23, 42));
                badge.BorderBrush = new SolidColorBrush(Color.FromArgb(180, 148, 163, 184));
                badge.BorderThickness = new Thickness(1.8);
            }
        }

        private void ExecuteClickAction()
        {
            bool inInput = InputDetector.IsInInputField(true);
            if (inInput)
            {
                ActionExecutor.Execute(ActionType.Backspace);
                TriggerFeedback("⌫ 退格");
            }
            else
            {
                if (_config.ClickAction != ActionType.None)
                {
                    ActionExecutor.Execute(_config.ClickAction, _config.CustomClickKey);
                    TriggerFeedback(ActionExecutor.GetActionShortLabel(_config.ClickAction, _config.CustomClickKey));
                }
            }
        }

        private void ExecuteSwipeAction(SlideDirection dir)
        {
            switch (dir)
            {
                case SlideDirection.Up:
                    ActionExecutor.Execute(_config.SwipeUpAction, _config.CustomSwipeUpKey);
                    break;
                case SlideDirection.Down:
                    ActionExecutor.Execute(_config.SwipeDownAction, _config.CustomSwipeDownKey);
                    break;
                case SlideDirection.Left:
                    ActionExecutor.Execute(_config.SwipeLeftAction, _config.CustomSwipeLeftKey);
                    break;
                case SlideDirection.Right:
                    ActionExecutor.Execute(_config.SwipeRightAction, _config.CustomSwipeRightKey);
                    break;
            }
        }

        private string GetSwipeActionLabel(SlideDirection dir)
        {
            switch (dir)
            {
                case SlideDirection.Up: return ActionExecutor.GetActionShortLabel(_config.SwipeUpAction, _config.CustomSwipeUpKey);
                case SlideDirection.Down: return ActionExecutor.GetActionShortLabel(_config.SwipeDownAction, _config.CustomSwipeDownKey);
                case SlideDirection.Left: return ActionExecutor.GetActionShortLabel(_config.SwipeLeftAction, _config.CustomSwipeLeftKey);
                case SlideDirection.Right: return ActionExecutor.GetActionShortLabel(_config.SwipeRightAction, _config.CustomSwipeRightKey);
                default: return "";
            }
        }

        private void TriggerFeedback(string label)
        {
            _feedbackText = label;
            _isActionFeedback = true;
            Opacity = _config.ActiveOpacity;
            UpdateBallVisuals();
            _feedbackTimer.Start();
        }

        #region Free Floating Multi-Monitor Position Management

        public void ResetPositionToRight()
        {
            Rect wa = GetCurrentScreenWorkingArea();
            double ballSize = _config.BallSize;
            double targetBallLeft = wa.Right - ballSize - 35;
            double targetBallTop = wa.Top + (wa.Height / 2) - (ballSize / 2);

            Left = targetBallLeft - HUD_EXT;
            Top = targetBallTop - HUD_EXT;
            SaveCurrentPosition();
            TriggerFeedback("📍 已重置位置");
        }

        public void EnterMoveModeFromMenu()
        {
            _mode = GestureMode.Moving;
            _currentDirection = SlideDirection.None;
            _touchOffsetInWindow = new Point(Width / 2, Height / 2);
            UpdateBallVisuals();
            TriggerFeedback("✥ 拖动模式");
        }

        public void ClampAndSaveCurrentPosition()
        {
            Rect wa = GetCurrentScreenWorkingArea();
            double ballSize = _config.BallSize;

            double curBallX = Left + HUD_EXT;
            double curBallY = Top + HUD_EXT;

            double clampedX = Math.Max(wa.Left + 15, Math.Min(wa.Right - ballSize - 15, curBallX));
            double clampedY = Math.Max(wa.Top + 15, Math.Min(wa.Bottom - ballSize - 15, curBallY));

            Left = clampedX - HUD_EXT;
            Top = clampedY - HUD_EXT;

            SaveCurrentPosition();
        }

        public void SaveCurrentPosition()
        {
            _config.PosX = (int)(Left + HUD_EXT);
            _config.PosY = (int)(Top + HUD_EXT);
            _config.Save();
        }

        #endregion

        private ContextMenu CreateContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            MenuItem itemSettings = new MenuItem { Header = "⚙ 设置 (Settings)" };
            itemSettings.Click += (s, ev) => OpenSettings();

            MenuItem itemMove = new MenuItem { Header = "✥ 拖动悬浮球位置 (Move Ball)" };
            itemMove.Click += (s, ev) => EnterMoveModeFromMenu();

            MenuItem itemResetPos = new MenuItem { Header = "📍 重置位置到屏幕右侧" };
            itemResetPos.Click += (s, ev) => ResetPositionToRight();

            MenuItem itemExit = new MenuItem { Header = "🚪 退出 (Exit)" };
            itemExit.Click += (s, ev) => Application.Current.Shutdown();

            menu.Items.Add(itemSettings);
            menu.Items.Add(new Separator());
            menu.Items.Add(itemMove);
            menu.Items.Add(itemResetPos);
            menu.Items.Add(new Separator());
            menu.Items.Add(itemExit);

            return menu;
        }

        public void ToggleVisibility()
        {
            if (Visibility == Visibility.Visible)
            {
                _isUserHidden = true;
                Visibility = Visibility.Hidden;
            }
            else
            {
                _isUserHidden = false;
                Visibility = Visibility.Visible;
                KeepWindowOnTop();
            }
        }

        public void OpenSettings()
        {
            SettingsWindow win = new SettingsWindow(_config);
            if (win.ShowDialog() == true)
            {
                _config.Save();
                _config.ApplyAutoStart();
                Width = _config.BallSize + HUD_EXT * 2;
                Height = _config.BallSize + HUD_EXT * 2;
                if (_rootCanvas != null)
                {
                    _rootCanvas.Width = Width;
                    _rootCanvas.Height = Height;
                }
                if (_ballScale != null)
                {
                    _ballScale.CenterX = _config.BallSize / 2.0;
                    _ballScale.CenterY = _config.BallSize / 2.0;
                }
                _ballGrid.Width = _config.BallSize;
                _ballGrid.Height = _config.BallSize;
                _ballEllipse.Width = _config.BallSize;
                _ballEllipse.Height = _config.BallSize;
                _ballBorder.Width = _config.BallSize;
                _ballBorder.Height = _config.BallSize;
                _ballInnerRing.Width = _config.BallSize - 12;
                _ballInnerRing.Height = _config.BallSize - 12;
                double cancelRadius = CancelRadiusPx;
                _centerCancelZone.Width = cancelRadius * 2;
                _centerCancelZone.Height = cancelRadius * 2;
                Canvas.SetLeft(_centerCancelZone, HUD_EXT + _config.BallSize / 2 - cancelRadius);
                Canvas.SetTop(_centerCancelZone, HUD_EXT + _config.BallSize / 2 - cancelRadius);
                _lastInteractionTime = DateTime.UtcNow;
                BeginAnimation(OpacityProperty, null);
                Opacity = _config.NormalOpacity;
                PositionDirectionBadges();
                ClampAndSaveCurrentPosition();
                UpdateBallVisuals();
            }
        }
    }
}
