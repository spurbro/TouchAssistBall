using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows;
using Forms = System.Windows.Forms;

namespace TouchAssistBall
{
    public class App : Application
    {
        private static Mutex _appMutex;
        private Forms.NotifyIcon _trayIcon;
        private FloatingBallWindow _ballWindow;
        private AppConfig _config;

        [STAThread]
        public static void Main()
        {
            AppLogger.Log("=== App Main Started ===");

            try
            {
                bool isNewInstance;
                _appMutex = new Mutex(true, "Local\\TouchAssistBall_Mutex_App", out isNewInstance);
                if (!isNewInstance)
                {
                    AppLogger.Log("Another instance is already running. Exiting.");
                    return;
                }

                AppLogger.Log("Creating App instance...");
                App app = new App();
                AppLogger.Log("Calling app.Run()...");
                app.Run();
                AppLogger.Log("app.Run() finished.");
            }
            catch (Exception ex)
            {
                AppLogger.Log("CRITICAL CRASH in Main: " + ex.ToString());
            }
            finally
            {
                if (_appMutex != null)
                {
                    try { _appMutex.ReleaseMutex(); } catch { }
                    _appMutex.Dispose();
                }
                AppLogger.Log("=== App Main Exited ===");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppLogger.Log("OnStartup starting...");
            try
            {
                base.OnStartup(e);

                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                DispatcherUnhandledException += (s, ev) => {
                    AppLogger.Log("DispatcherUnhandledException: " + ev.Exception.ToString());
                    ev.Handled = true;
                };

                AppLogger.Log("Loading config...");
                _config = AppConfig.Load();

                AppLogger.Log("Creating FloatingBallWindow...");
                _ballWindow = new FloatingBallWindow(_config);

                AppLogger.Log("Calling _ballWindow.Show()...");
                _ballWindow.Show();
                AppLogger.Log("_ballWindow.Show() returned.");

                AppLogger.Log("Initializing Tray Icon...");
                InitTrayIcon();
                AppLogger.Log("Tray Icon initialized.");
            }
            catch (Exception ex)
            {
                AppLogger.Log("EXCEPTION in OnStartup: " + ex.ToString());
            }
        }

        private void InitTrayIcon()
        {
            try
            {
                _trayIcon = new Forms.NotifyIcon();
                _trayIcon.Text = "触屏桌面悬浮球 (TouchAssist)";
                _trayIcon.Icon = CreateTrayIcon();
                _trayIcon.Visible = true;

                _trayIcon.ShowBalloonTip(3000, "触屏悬浮球已启动", "桌面悬浮球已常驻运行！可直接点击屏幕右侧小球或双击此托盘图标打开设置。", Forms.ToolTipIcon.Info);

                Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();

                Forms.ToolStripMenuItem itemSettings = new Forms.ToolStripMenuItem("⚙ 设置 (Settings)");
                itemSettings.Click += (s, e) => _ballWindow.OpenSettings();

                Forms.ToolStripMenuItem itemResetPos = new Forms.ToolStripMenuItem("📍 重置悬浮球位置");
                itemResetPos.Click += (s, e) => _ballWindow.ResetPositionToRight();

                Forms.ToolStripMenuItem itemToggle = new Forms.ToolStripMenuItem("👁 显示/隐藏悬浮球");
                itemToggle.Click += (s, e) => _ballWindow.ToggleVisibility();

                Forms.ToolStripMenuItem itemAutoStart = new Forms.ToolStripMenuItem("🚀 开机自启动");
                itemAutoStart.Checked = _config.StartWithWindows;
                itemAutoStart.Click += (s, e) =>
                {
                    _config.StartWithWindows = !_config.StartWithWindows;
                    itemAutoStart.Checked = _config.StartWithWindows;
                    _config.ApplyAutoStart();
                    _config.Save();
                };

                Forms.ToolStripMenuItem itemSep = new Forms.ToolStripMenuItem("-");

                Forms.ToolStripMenuItem itemExit = new Forms.ToolStripMenuItem("🚪 退出 (Exit)");
                itemExit.Click += (s, e) =>
                {
                    Shutdown();
                };

                menu.Items.AddRange(new Forms.ToolStripItem[] { itemSettings, itemResetPos, itemToggle, itemAutoStart, itemSep, itemExit });
                _trayIcon.ContextMenuStrip = menu;
                _trayIcon.DoubleClick += (s, e) => _ballWindow.OpenSettings();
            }
            catch (Exception ex)
            {
                AppLogger.Log("InitTrayIcon error: " + ex.Message);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    if (_trayIcon.Icon != null)
                    {
                        _trayIcon.Icon.Dispose();
                    }
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            }
            catch { }
            base.OnExit(e);
        }

        private static Icon CreateTrayIcon()
        {
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                Icon exeIcon = Icon.ExtractAssociatedIcon(exePath);
                if (exeIcon != null)
                {
                    return exeIcon;
                }
            }
            catch { }

            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(2, 2, 28, 28), Color.FromArgb(37, 99, 235), Color.FromArgb(14, 165, 233), 45f))
                {
                    g.FillEllipse(lgb, 2, 2, 28, 28);
                }
                using (Pen p = new Pen(Color.White, 2f))
                {
                    g.DrawEllipse(p, 3, 3, 26, 26);
                }
                using (SolidBrush dot = new SolidBrush(Color.White))
                {
                    g.FillEllipse(dot, 12, 12, 8, 8);
                }

                IntPtr hIcon = bmp.GetHicon();
                try
                {
                    using (Icon temp = Icon.FromHandle(hIcon))
                    {
                        return (Icon)temp.Clone();
                    }
                }
                finally
                {
                    NativeMethods.DestroyIcon(hIcon);
                }
            }
        }
    }
}
