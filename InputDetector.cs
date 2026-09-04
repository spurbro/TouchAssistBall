using System;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace TouchAssistBall
{
    public static class InputDetector
    {
        private static IntPtr _lastForegroundHwnd = IntPtr.Zero;
        private static volatile bool _cachedUiaResult = false;
        private static volatile bool _isUiaRunning = false;
        private static DateTime _lastUiaCheckTime = DateTime.MinValue;
        private const int UIA_THROTTLE_MS_STABLE = 2500; // Throttle to 2.5s when foreground window unchanged
        private const int UIA_THROTTLE_MS_FAST = 400;     // Fast throttle when switching or forced
        private static uint _currentProcessId = 0;

        public static bool IsInInputField(bool forceCheck = false)
        {
            try
            {
                if (_currentProcessId == 0)
                {
                    _currentProcessId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                }

                IntPtr hForeground = NativeMethods.GetForegroundWindow();
                if (hForeground == IntPtr.Zero)
                {
                    _cachedUiaResult = false;
                    return false;
                }

                uint fgPid;
                NativeMethods.GetWindowThreadProcessId(hForeground, out fgPid);
                if (fgPid == _currentProcessId)
                {
                    // Foreground is our own window (Settings, context menu, etc.)
                    return false;
                }

                bool isWindowChanged = (hForeground != _lastForegroundHwnd);
                if (isWindowChanged)
                {
                    _lastForegroundHwnd = hForeground;
                    _cachedUiaResult = false;
                }

                // 1. Fast Win32 Caret & Thread Focus Check (synchronous, ~0ms, non-blocking)
                if (CheckWin32Input(hForeground))
                {
                    _cachedUiaResult = true;
                    return true;
                }

                // Quick Exclusion: If foreground is desktop or shell, skip UIA completely
                StringBuilder sbClass = new StringBuilder(64);
                if (NativeMethods.GetClassName(hForeground, sbClass, 64) > 0)
                {
                    string cls = sbClass.ToString();
                    if (cls.Equals("Progman", StringComparison.OrdinalIgnoreCase) ||
                        cls.Equals("WorkerW", StringComparison.OrdinalIgnoreCase) ||
                        cls.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase))
                    {
                        _cachedUiaResult = false;
                        return false;
                    }
                }

                // 2. Asynchronous throttled UIA check on background thread
                TriggerAsyncUiaCheck(hForeground, forceCheck, isWindowChanged);

                return _cachedUiaResult;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckWin32Input(IntPtr hForeground)
        {
            try
            {
                uint processId;
                uint threadId = NativeMethods.GetWindowThreadProcessId(hForeground, out processId);
                if (threadId == 0) return false;

                NativeMethods.GUITHREADINFO gui = new NativeMethods.GUITHREADINFO();
                gui.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.GUITHREADINFO));

                if (NativeMethods.GetGUIThreadInfo(threadId, ref gui))
                {
                    if (gui.hwndCaret != IntPtr.Zero &&
                        (gui.rcCaret.Width > 0 || gui.rcCaret.Height > 0))
                    {
                        return true;
                    }

                    IntPtr hFocus = (gui.hwndFocus != IntPtr.Zero) ? gui.hwndFocus : gui.hwndActive;
                    if (hFocus != IntPtr.Zero)
                    {
                        StringBuilder sbClass = new StringBuilder(128);
                        if (NativeMethods.GetClassName(hFocus, sbClass, 128) > 0)
                        {
                            if (IsTextInputClassName(sbClass.ToString()))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static void TriggerAsyncUiaCheck(IntPtr targetHwnd, bool forceCheck, bool isWindowChanged)
        {
            DateTime now = DateTime.UtcNow;
            if (_isUiaRunning) return;

            int throttleMs = forceCheck || isWindowChanged ? UIA_THROTTLE_MS_FAST : UIA_THROTTLE_MS_STABLE;
            if (!forceCheck && (now - _lastUiaCheckTime).TotalMilliseconds < throttleMs)
            {
                return;
            }

            _isUiaRunning = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (NativeMethods.GetForegroundWindow() != targetHwnd)
                    {
                        return;
                    }

                    bool res = CheckUiaInternal(targetHwnd);
                    _cachedUiaResult = res;
                    _lastUiaCheckTime = DateTime.UtcNow;
                }
                catch { }
                finally
                {
                    _isUiaRunning = false;
                }
            });
        }

        private static bool CheckUiaInternal(IntPtr hForeground)
        {
            try
            {
                AutomationElement focused = AutomationElement.FocusedElement;
                if (focused == null) return false;

                AutomationElement.AutomationElementInformation info = focused.Current;

                string cn = info.ClassName ?? "";
                if (cn.Equals("Progman", StringComparison.OrdinalIgnoreCase) ||
                    cn.Equals("WorkerW", StringComparison.OrdinalIgnoreCase) ||
                    cn.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase) ||
                    cn.Equals("SHELLDLL_DefView", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                ControlType ct = info.ControlType;
                bool isEditLike = (ct == ControlType.Edit) ||
                                  (ct == ControlType.Custom && IsTextInputClassName(cn)) ||
                                  (ct == ControlType.Document && IsTextInputClassName(cn));

                if (!isEditLike) return false;

                if (!info.HasKeyboardFocus) return false;

                object valPatternObj;
                if (focused.TryGetCurrentPattern(ValuePattern.Pattern, out valPatternObj))
                {
                    ValuePattern vp = valPatternObj as ValuePattern;
                    if (vp != null && vp.Current.IsReadOnly) return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTextInputClassName(string cls)
        {
            if (string.IsNullOrEmpty(cls)) return false;
            string lower = cls.ToLowerInvariant();

            return lower.Contains("edit") ||
                   lower.Contains("textbox") ||
                   lower.Contains("textarea") ||
                   lower.Contains("scintilla");
        }
    }
}
