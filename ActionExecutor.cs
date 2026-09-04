using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace TouchAssistBall
{
    public enum ActionType
    {
        None = 0,
        Backspace = 1,
        Copy = 2,
        Paste = 3,
        Screenshot = 4,        // Instant Full Screen Screenshot (PrintScreen / Win+PrintScreen)
        VoiceTyping = 5,       // Right Alt for Quark Qianwen / iFlytek IME Voice Typing
        Enter = 6,             // Enter
        SnippetScreenshot = 7, // Win + Shift + S (Snippet Overlay)
        Undo = 8,              // Ctrl + Z
        Redo = 9,              // Ctrl + Y
        SelectAll = 10,        // Ctrl + A
        Cut = 11,              // Ctrl + X
        Save = 12,             // Ctrl + S
        AltTab = 13,           // Alt + Tab
        ShowDesktop = 14,      // Win + D
        ClipboardHistory = 15, // Win + V
        CloseWindow = 16,      // Ctrl + W
        Refresh = 17,          // F5
        Esc = 18,              // Esc
        Delete = 19,           // Delete
        Space = 20,            // Space
        Tab = 21,              // Tab
        RightAlt = 22,         // Direct Right Alt
        HoldCurrentKey = 23,   // Physically hold down the active key until release
        ContinuousBackspace = 24, // Continuously hold backspace
        ContinuousDelete = 25, // Continuously hold delete
        HoldCtrl = 26,         // Physically hold Ctrl
        HoldAlt = 27,          // Physically hold Alt
        HoldShift = 28,        // Physically hold Shift
        HoldWin = 29,          // Physically hold Win
        HoldSpace = 30,        // Physically hold Space (Photoshop/CAD hand tool)
        CustomKey = 99         // Arbitrary user-defined key combo
    }

    public static class ActionExecutor
    {
        public static string GetActionDisplayName(ActionType action)
        {
            switch (action)
            {
                case ActionType.None: return "🚫 无动作 (None)";
                case ActionType.Backspace: return "⌫ 退格 (Backspace)";
                case ActionType.Copy: return "📋 复制 (Ctrl + C)";
                case ActionType.Paste: return "📥 粘贴 (Ctrl + V)";
                case ActionType.Screenshot: return "📸 全屏截屏 (直接截取整个屏幕至剪贴板/保存)";
                case ActionType.SnippetScreenshot: return "✂️ 区域截图工具 (Win + Shift + S)";
                case ActionType.VoiceTyping: return "🎙️ 语音输入 (单击右Alt - 夸克千问/讯飞输入法)";
                case ActionType.RightAlt: return "🔤 右 Alt 键 (RAlt)";
                case ActionType.Enter: return "↵ 回车换行 (Enter)";
                case ActionType.Undo: return "↩ 撤销 (Ctrl + Z)";
                case ActionType.Redo: return "↪ 重做 (Ctrl + Y)";
                case ActionType.SelectAll: return "🔲 全选 (Ctrl + A)";
                case ActionType.Cut: return "✂️ 剪切 (Ctrl + X)";
                case ActionType.Save: return "💾 保存 (Ctrl + S)";
                case ActionType.AltTab: return "🔀 切换应用 (Alt + Tab)";
                case ActionType.ShowDesktop: return "🖥️ 显示桌面 (Win + D)";
                case ActionType.ClipboardHistory: return "📋 剪贴板历史 (Win + V)";
                case ActionType.CloseWindow: return "❌ 关闭当前窗口/标签 (Ctrl + W)";
                case ActionType.Refresh: return "🔄 刷新 (F5)";
                case ActionType.Esc: return "⎋ 退出/取消 (Esc)";
                case ActionType.Delete: return "⌦ 向后删除 (Delete)";
                case ActionType.Space: return "␣ 空格 (Space)";
                case ActionType.Tab: return "⇥ 制表符 (Tab)";
                case ActionType.HoldCurrentKey: return "✊ 物理持续按住当前键 (随手指松开释放)";
                case ActionType.ContinuousBackspace: return "⌫ 持续按住退格 (Continuous Backspace)";
                case ActionType.ContinuousDelete: return "⌦ 持续按住向后删除 (Continuous Delete)";
                case ActionType.HoldCtrl: return "🔲 持续按住 Ctrl 键 (Hold Ctrl)";
                case ActionType.HoldAlt: return "🔤 持续按住 Alt 键 (Hold Alt)";
                case ActionType.HoldShift: return "⇧ 持续按住 Shift 键 (Hold Shift)";
                case ActionType.HoldWin: return "🪟 持续按住 Win 键 (Hold Win)";
                case ActionType.HoldSpace: return "␣ 持续按住空格键 (Hold Space - 绘图抓手)";
                case ActionType.CustomKey: return "⌨️ 自定义按键组合 (Custom Key)...";
                default: return action.ToString();
            }
        }

        public static string GetActionShortLabel(ActionType action, string customKey = "")
        {
            if (action == ActionType.CustomKey && !string.IsNullOrEmpty(customKey))
            {
                return customKey.ToUpper();
            }

            switch (action)
            {
                case ActionType.None: return "无";
                case ActionType.Backspace: return "⌫ 退格";
                case ActionType.Copy: return "复制";
                case ActionType.Paste: return "粘贴";
                case ActionType.Screenshot: return "全屏截屏";
                case ActionType.SnippetScreenshot: return "区域截屏";
                case ActionType.VoiceTyping: return "🎙 语音";
                case ActionType.RightAlt: return "右Alt";
                case ActionType.Enter: return "回车";
                case ActionType.Undo: return "撤销";
                case ActionType.Redo: return "重做";
                case ActionType.SelectAll: return "全选";
                case ActionType.Cut: return "剪切";
                case ActionType.Save: return "保存";
                case ActionType.AltTab: return "切窗口";
                case ActionType.ShowDesktop: return "桌面";
                case ActionType.ClipboardHistory: return "剪贴板";
                case ActionType.CloseWindow: return "关闭";
                case ActionType.Refresh: return "刷新";
                case ActionType.Esc: return "Esc";
                case ActionType.Delete: return "Del";
                case ActionType.Space: return "空格";
                case ActionType.Tab: return "Tab";
                case ActionType.HoldCurrentKey: return "按住";
                case ActionType.ContinuousBackspace: return "⌫ 退格";
                case ActionType.ContinuousDelete: return "⌦ Del";
                case ActionType.HoldCtrl: return "按住Ctrl";
                case ActionType.HoldAlt: return "按住Alt";
                case ActionType.HoldShift: return "按住Shift";
                case ActionType.HoldWin: return "按住Win";
                case ActionType.HoldSpace: return "按住空格";
                default: return action.ToString();
            }
        }

        public static void Execute(ActionType action, string customKey = "")
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                ExecuteSync(action, customKey);
            });
        }

        private static readonly object _heldLock = new object();
        private static readonly List<byte> _currentlyHeldModifiers = new List<byte>();
        private static readonly List<byte> _currentlyHeldKeys = new List<byte>();
        private static volatile bool _isAutoRepeating = false;
        private static Thread _repeatThread = null;

        public static bool IsHoldingKeys
        {
            get
            {
                lock (_heldLock)
                {
                    return _currentlyHeldModifiers.Count > 0 || _currentlyHeldKeys.Count > 0 || _isAutoRepeating;
                }
            }
        }

        public static void StartHold(ActionType action, string customKey = "")
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                StartHoldSync(action, customKey);
            });
        }

        public static void StartHoldSync(ActionType action, string customKey = "")
        {
            lock (_heldLock)
            {
                ReleaseHoldInternal();

                List<byte> modifiers, keys;
                ResolveActionKeys(action, customKey, out modifiers, out keys);

                if (modifiers.Count == 0 && keys.Count == 0) return;

                AppLogger.Log("StartHold: action=" + action + (!string.IsNullOrEmpty(customKey) ? " custom=" + customKey : "") + " [Mods=" + modifiers.Count + " Keys=" + keys.Count + "]");

                // Check if this action should have hardware-like Typematic auto-repeat (like Backspace / Delete)
                bool shouldAutoRepeat = (action == ActionType.Backspace || action == ActionType.ContinuousBackspace ||
                                         action == ActionType.Delete || action == ActionType.ContinuousDelete ||
                                         (modifiers.Count == 0 && keys.Count == 1 && (keys[0] == NativeMethods.VK_BACK || keys[0] == NativeMethods.VK_DELETE)));

                if (shouldAutoRepeat && keys.Count > 0)
                {
                    byte repeatVk = keys[0];
                    _isAutoRepeating = true;
                    _repeatThread = new Thread(() =>
                    {
                        try
                        {
                            // Initial tap
                            SendSingleKey(repeatVk);
                            // Initial delay before continuous repeating
                            Thread.Sleep(250);

                            while (_isAutoRepeating)
                            {
                                SendSingleKey(repeatVk);
                                Thread.Sleep(55); // ~18 repeats per second, smooth & controllable
                            }
                        }
                        catch { }
                    })
                    {
                        IsBackground = true,
                        Name = "TouchAssist_AutoRepeatKey"
                    };
                    _repeatThread.Start();
                    return;
                }

                // 1. Modifiers down in order with physical delay
                foreach (byte m in modifiers)
                {
                    List<NativeMethods.INPUT> down = new List<NativeMethods.INPUT>();
                    down.Add(MakeKeyInput(m, false));
                    SendInputs(down);
                    _currentlyHeldModifiers.Add(m);
                    Thread.Sleep(15);
                }

                // 2. Main keys down in order with physical delay
                foreach (byte k in keys)
                {
                    List<NativeMethods.INPUT> down = new List<NativeMethods.INPUT>();
                    down.Add(MakeKeyInput(k, false));
                    SendInputs(down);
                    _currentlyHeldKeys.Add(k);
                    Thread.Sleep(20);
                }
            }
        }

        public static void ReleaseHold()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                ReleaseHoldSync();
            });
        }

        public static void ReleaseHoldSync()
        {
            lock (_heldLock)
            {
                ReleaseHoldInternal();
            }
        }

        public static void ReleaseAllHeldKeys()
        {
            ReleaseHoldSync();
        }

        private static void ReleaseHoldInternal()
        {
            if (_isAutoRepeating)
            {
                _isAutoRepeating = false;
                if (_repeatThread != null && _repeatThread.IsAlive)
                {
                    try { _repeatThread.Join(150); } catch { }
                    _repeatThread = null;
                }
            }

            if (_currentlyHeldKeys.Count == 0 && _currentlyHeldModifiers.Count == 0) return;

            AppLogger.Log("ReleaseHold (KeyUp): releasing " + _currentlyHeldKeys.Count + " keys and " + _currentlyHeldModifiers.Count + " modifiers");

            // 1. Release keys in reverse order with physical delay
            for (int i = _currentlyHeldKeys.Count - 1; i >= 0; i--)
            {
                byte k = _currentlyHeldKeys[i];
                List<NativeMethods.INPUT> up = new List<NativeMethods.INPUT>();
                up.Add(MakeKeyInput(k, true));
                SendInputs(up);
                Thread.Sleep(15);
            }
            _currentlyHeldKeys.Clear();

            // 2. Release modifiers in reverse order with physical delay
            for (int i = _currentlyHeldModifiers.Count - 1; i >= 0; i--)
            {
                byte m = _currentlyHeldModifiers[i];
                List<NativeMethods.INPUT> up = new List<NativeMethods.INPUT>();
                up.Add(MakeKeyInput(m, true));
                SendInputs(up);
                Thread.Sleep(15);
            }
            _currentlyHeldModifiers.Clear();
        }

        public static void ExecuteSync(ActionType action, string customKey = "")
        {
            try
            {
                AppLogger.Log("Execute tap (Press & Release): action=" + action + (!string.IsNullOrEmpty(customKey) ? " (custom=" + customKey + ")" : ""));

                if (action == ActionType.None) return;

                if (action == ActionType.VoiceTyping || action == ActionType.RightAlt)
                {
                    SendRightAltClick();
                    return;
                }

                if (action == ActionType.Screenshot)
                {
                    ExecuteDirectFullScreenScreenshot();
                    return;
                }

                List<byte> modifiers, keys;
                ResolveActionKeys(action, customKey, out modifiers, out keys);

                if (modifiers.Count == 0 && keys.Count == 0) return;

                // Authentic Physical Key Tap Simulation:
                // Step 1: Press all modifiers down in sequential order with delay
                foreach (byte m in modifiers)
                {
                    List<NativeMethods.INPUT> mDown = new List<NativeMethods.INPUT>();
                    mDown.Add(MakeKeyInput(m, false));
                    SendInputs(mDown);
                    Thread.Sleep(20);
                }

                // Step 2: Press all main keys down in sequential order with delay
                foreach (byte k in keys)
                {
                    List<NativeMethods.INPUT> kDown = new List<NativeMethods.INPUT>();
                    kDown.Add(MakeKeyInput(k, false));
                    SendInputs(kDown);
                    Thread.Sleep(25);
                }

                // Step 3: Dwell delay (simulate physical key contact duration)
                Thread.Sleep(30);

                // Step 4: Release all main keys up in reverse order with delay
                for (int i = keys.Count - 1; i >= 0; i--)
                {
                    List<NativeMethods.INPUT> kUp = new List<NativeMethods.INPUT>();
                    kUp.Add(MakeKeyInput(keys[i], true));
                    SendInputs(kUp);
                    Thread.Sleep(20);
                }

                // Step 5: Release all modifiers up in reverse order with delay
                for (int i = modifiers.Count - 1; i >= 0; i--)
                {
                    List<NativeMethods.INPUT> mUp = new List<NativeMethods.INPUT>();
                    mUp.Add(MakeKeyInput(modifiers[i], true));
                    SendInputs(mUp);
                    Thread.Sleep(20);
                }
            }
            catch { }
        }

        public static void ResolveActionKeys(ActionType action, string customKey, out List<byte> modifiers, out List<byte> keys)
        {
            modifiers = new List<byte>();
            keys = new List<byte>();

            switch (action)
            {
                case ActionType.None:
                    break;
                case ActionType.Backspace:
                case ActionType.ContinuousBackspace:
                    keys.Add(NativeMethods.VK_BACK);
                    break;
                case ActionType.Copy:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    keys.Add(NativeMethods.VK_KEY_C);
                    break;
                case ActionType.Paste:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    keys.Add(NativeMethods.VK_KEY_V);
                    break;
                case ActionType.Screenshot:
                    modifiers.Add(NativeMethods.VK_LWIN);
                    keys.Add(NativeMethods.VK_SNAPSHOT);
                    break;
                case ActionType.SnippetScreenshot:
                    modifiers.Add(NativeMethods.VK_LWIN);
                    modifiers.Add(NativeMethods.VK_SHIFT);
                    keys.Add(NativeMethods.VK_KEY_S);
                    break;
                case ActionType.VoiceTyping:
                case ActionType.RightAlt:
                    keys.Add(NativeMethods.VK_RMENU);
                    break;
                case ActionType.Enter:
                    keys.Add(NativeMethods.VK_RETURN);
                    break;
                case ActionType.Undo:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    keys.Add(NativeMethods.VK_KEY_Z);
                    break;
                case ActionType.Redo:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    keys.Add(NativeMethods.VK_KEY_Y);
                    break;
                case ActionType.SelectAll:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    keys.Add(NativeMethods.VK_KEY_A);
                    break;
                case ActionType.Cut:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    keys.Add(NativeMethods.VK_KEY_X);
                    break;
                case ActionType.Save:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    keys.Add(NativeMethods.VK_KEY_S);
                    break;
                case ActionType.AltTab:
                    modifiers.Add(NativeMethods.VK_MENU);
                    keys.Add(NativeMethods.VK_TAB);
                    break;
                case ActionType.ShowDesktop:
                    modifiers.Add(NativeMethods.VK_LWIN);
                    keys.Add(NativeMethods.VK_KEY_D);
                    break;
                case ActionType.ClipboardHistory:
                    modifiers.Add(NativeMethods.VK_LWIN);
                    keys.Add(NativeMethods.VK_KEY_V);
                    break;
                case ActionType.CloseWindow:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    keys.Add(NativeMethods.VK_KEY_W);
                    break;
                case ActionType.Refresh:
                    keys.Add(0x74); // VK_F5
                    break;
                case ActionType.Esc:
                    keys.Add(NativeMethods.VK_ESCAPE);
                    break;
                case ActionType.Delete:
                case ActionType.ContinuousDelete:
                    keys.Add(NativeMethods.VK_DELETE);
                    break;
                case ActionType.Space:
                case ActionType.HoldSpace:
                    keys.Add(NativeMethods.VK_SPACE);
                    break;
                case ActionType.Tab:
                    keys.Add(NativeMethods.VK_TAB);
                    break;
                case ActionType.HoldCtrl:
                    modifiers.Add(NativeMethods.VK_CONTROL);
                    break;
                case ActionType.HoldAlt:
                    modifiers.Add(NativeMethods.VK_MENU);
                    break;
                case ActionType.HoldShift:
                    modifiers.Add(NativeMethods.VK_SHIFT);
                    break;
                case ActionType.HoldWin:
                    modifiers.Add(NativeMethods.VK_LWIN);
                    break;
                case ActionType.CustomKey:
                    if (!string.IsNullOrEmpty(customKey))
                    {
                        string normStr = customKey.Trim().ToUpperInvariant();
                        if (normStr == "RALT" || normStr == "RIGHTALT" || normStr == "ALTGR")
                        {
                            keys.Add(NativeMethods.VK_RMENU);
                            break;
                        }

                        List<string> tokens = ParseKeyTokens(customKey);
                        foreach (string t in tokens)
                        {
                            string norm = t.Trim().ToUpperInvariant();
                            if (norm == "LCTRL" || norm == "LEFTCTRL") modifiers.Add(NativeMethods.VK_LCONTROL);
                            else if (norm == "RCTRL" || norm == "RIGHTCTRL") modifiers.Add(NativeMethods.VK_RCONTROL);
                            else if (norm == "CTRL" || norm == "CONTROL") modifiers.Add(NativeMethods.VK_CONTROL);
                            else if (norm == "LALT" || norm == "LEFTALT") modifiers.Add(NativeMethods.VK_LMENU);
                            else if (norm == "RALT" || norm == "RIGHTALT" || norm == "ALTGR") modifiers.Add(NativeMethods.VK_RMENU);
                            else if (norm == "ALT" || norm == "MENU") modifiers.Add(NativeMethods.VK_MENU);
                            else if (norm == "LSHIFT" || norm == "LEFTSHIFT") modifiers.Add(NativeMethods.VK_LSHIFT);
                            else if (norm == "RSHIFT" || norm == "RIGHTSHIFT") modifiers.Add(NativeMethods.VK_RSHIFT);
                            else if (norm == "SHIFT") modifiers.Add(NativeMethods.VK_SHIFT);
                            else if (norm == "LWIN" || norm == "LEFTWIN") modifiers.Add(NativeMethods.VK_LWIN);
                            else if (norm == "RWIN" || norm == "RIGHTWIN") modifiers.Add(NativeMethods.VK_RWIN);
                            else if (norm == "WIN" || norm == "WINDOWS") modifiers.Add(NativeMethods.VK_LWIN);
                            else
                            {
                                byte vk = ResolveVirtualKey(norm);
                                if (vk != 0) keys.Add(vk);
                            }
                        }
                    }
                    break;
            }
        }

        public static void SendRightAltClick()
        {
            // Simulate ONE clean physical Right Alt tap (E0-38 make, E0-F0-38 break).
            AppLogger.Log("SendRightAltClick: sending single VK_RMENU tap");

            List<NativeMethods.INPUT> down = new List<NativeMethods.INPUT>();
            down.Add(MakeKeyInput(NativeMethods.VK_RMENU, false));
            SendInputs(down);

            Thread.Sleep(60);

            List<NativeMethods.INPUT> up = new List<NativeMethods.INPUT>();
            up.Add(MakeKeyInput(NativeMethods.VK_RMENU, true));
            SendInputs(up);
        }

        public static void ExecuteDirectFullScreenScreenshot()
        {
            // Send Win + PrintScreen (Instant system full-screen capture auto-saved to Pictures\Screenshots)
            SendModifiedKey(NativeMethods.VK_LWIN, NativeMethods.VK_SNAPSHOT);
        }

        public static List<string> ParseKeyTokens(string keyStr)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(keyStr)) return tokens;

            string s = keyStr.Trim();
            if (s == "+" || s == "++")
            {
                tokens.Add("+");
                return tokens;
            }

            int i = 0;
            while (i < s.Length)
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
                if (i >= s.Length) break;

                if (s[i] == '+')
                {
                    tokens.Add("+");
                    i++;
                    continue;
                }

                int start = i;
                while (i < s.Length && s[i] != '+' && !char.IsWhiteSpace(s[i]))
                {
                    i++;
                }
                string token = s.Substring(start, i - start);
                if (!string.IsNullOrEmpty(token))
                {
                    tokens.Add(token);
                }

                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;

                if (i < s.Length && s[i] == '+')
                {
                    i++; // skip '+' separator
                }
            }

            return tokens;
        }

        public static void ExecuteCustomKeyString(string keyStr)
        {
            if (string.IsNullOrEmpty(keyStr)) return;

            string normStr = keyStr.Trim().ToUpperInvariant();
            if (normStr == "RALT" || normStr == "RIGHTALT" || normStr == "ALTGR")
            {
                SendRightAltClick();
                return;
            }

            List<string> tokens = ParseKeyTokens(keyStr);
            List<byte> modifiers = new List<byte>();
            List<byte> keys = new List<byte>();

            foreach (string t in tokens)
            {
                string norm = t.Trim().ToUpperInvariant();

                if (norm == "LCTRL" || norm == "LEFTCTRL") modifiers.Add(NativeMethods.VK_LCONTROL);
                else if (norm == "RCTRL" || norm == "RIGHTCTRL") modifiers.Add(NativeMethods.VK_RCONTROL);
                else if (norm == "CTRL" || norm == "CONTROL") modifiers.Add(NativeMethods.VK_CONTROL);
                else if (norm == "LALT" || norm == "LEFTALT") modifiers.Add(NativeMethods.VK_LMENU);
                else if (norm == "RALT" || norm == "RIGHTALT" || norm == "ALTGR") modifiers.Add(NativeMethods.VK_RMENU);
                else if (norm == "ALT" || norm == "MENU") modifiers.Add(NativeMethods.VK_MENU);
                else if (norm == "LSHIFT" || norm == "LEFTSHIFT") modifiers.Add(NativeMethods.VK_LSHIFT);
                else if (norm == "RSHIFT" || norm == "RIGHTSHIFT") modifiers.Add(NativeMethods.VK_RSHIFT);
                else if (norm == "SHIFT") modifiers.Add(NativeMethods.VK_SHIFT);
                else if (norm == "LWIN" || norm == "LEFTWIN") modifiers.Add(NativeMethods.VK_LWIN);
                else if (norm == "RWIN" || norm == "RIGHTWIN") modifiers.Add(NativeMethods.VK_RWIN);
                else if (norm == "WIN" || norm == "WINDOWS") modifiers.Add(NativeMethods.VK_LWIN);
                else
                {
                    byte vk = ResolveVirtualKey(norm);
                    if (vk != 0) keys.Add(vk);
                }
            }

            if (modifiers.Count == 0 && keys.Count == 0) return;

            if (modifiers.Count == 1 && keys.Count == 0)
            {
                if (modifiers[0] == NativeMethods.VK_RMENU)
                {
                    SendRightAltClick();
                }
                else
                {
                    SendSingleKey(modifiers[0]);
                }
                return;
            }

            if (modifiers.Count == 0 && keys.Count == 1)
            {
                SendSingleKey(keys[0]);
                return;
            }

            // 1. Press all modifiers down in sequential order with delay
            foreach (byte m in modifiers)
            {
                List<NativeMethods.INPUT> mDown = new List<NativeMethods.INPUT>();
                mDown.Add(MakeKeyInput(m, false));
                SendInputs(mDown);
                Thread.Sleep(20);
            }

            // 2. Press all main keys down in sequential order with delay
            foreach (byte k in keys)
            {
                List<NativeMethods.INPUT> kDown = new List<NativeMethods.INPUT>();
                kDown.Add(MakeKeyInput(k, false));
                SendInputs(kDown);
                Thread.Sleep(25);
            }

            // 3. Dwell delay so target window processes the shortcut
            Thread.Sleep(30);

            // 4. Release all main keys up in reverse order with delay
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                List<NativeMethods.INPUT> kUp = new List<NativeMethods.INPUT>();
                kUp.Add(MakeKeyInput(keys[i], true));
                SendInputs(kUp);
                Thread.Sleep(20);
            }

            // 5. Release all modifiers up in reverse order with delay
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                List<NativeMethods.INPUT> mUp = new List<NativeMethods.INPUT>();
                mUp.Add(MakeKeyInput(modifiers[i], true));
                SendInputs(mUp);
                Thread.Sleep(20);
            }
        }

        private static void SendInputs(List<NativeMethods.INPUT> inputs)
        {
            if (inputs == null || inputs.Count == 0) return;
            NativeMethods.INPUT[] arr = inputs.ToArray();
            int cb = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.INPUT));
            uint sent = NativeMethods.SendInput((uint)arr.Length, arr, cb);
            if (sent != arr.Length)
            {
                AppLogger.Log("SendInput FAILED: sent " + sent + "/" + arr.Length + ", err=" + System.Runtime.InteropServices.Marshal.GetLastWin32Error());
            }
        }

        public static byte ResolveVirtualKey(string keyName)
        {
            if (string.IsNullOrEmpty(keyName)) return 0;
            keyName = keyName.ToUpperInvariant();

            if (keyName.Length == 1 && keyName[0] >= 'A' && keyName[0] <= 'Z')
            {
                return (byte)keyName[0];
            }

            if (keyName.Length == 1 && keyName[0] >= '0' && keyName[0] <= '9')
            {
                return (byte)keyName[0];
            }

            if (Regex.IsMatch(keyName, @"^F\d{1,2}$"))
            {
                int fNum;
                if (int.TryParse(keyName.Substring(1), out fNum) && fNum >= 1 && fNum <= 24)
                {
                    return (byte)(0x70 + (fNum - 1));
                }
            }

            if (Regex.IsMatch(keyName, @"^(NUM|NUMPAD)\d$"))
            {
                int n = keyName[keyName.Length - 1] - '0';
                return (byte)(0x60 + n);
            }

            switch (keyName)
            {
                case "ENTER":
                case "RETURN": return NativeMethods.VK_RETURN;
                case "BACKSPACE":
                case "BACK": return NativeMethods.VK_BACK;
                case "TAB": return NativeMethods.VK_TAB;
                case "ESC":
                case "ESCAPE": return NativeMethods.VK_ESCAPE;
                case "SPACE":
                case "SPACEBAR": return NativeMethods.VK_SPACE;
                case "DEL":
                case "DELETE": return NativeMethods.VK_DELETE;
                case "INS":
                case "INSERT": return NativeMethods.VK_INSERT;
                case "HOME": return NativeMethods.VK_HOME;
                case "END": return NativeMethods.VK_END;
                case "PGUP":
                case "PAGEUP":
                case "PRIOR": return NativeMethods.VK_PRIOR;
                case "PGDN":
                case "PAGEDOWN":
                case "NEXT": return NativeMethods.VK_NEXT;
                case "LEFT":
                case "ARROWLEFT": return NativeMethods.VK_LEFT;
                case "UP":
                case "ARROWUP": return NativeMethods.VK_UP;
                case "RIGHT":
                case "ARROWRIGHT": return NativeMethods.VK_RIGHT;
                case "DOWN":
                case "ARROWDOWN": return NativeMethods.VK_DOWN;
                case "CAPSLOCK":
                case "CAPITAL": return NativeMethods.VK_CAPITAL;
                case "PRINTSCREEN":
                case "PRINTSCRN":
                case "SNAPSHOT":
                case "PRTSC":
                case "PRTSCN":
                case "PRINT": return NativeMethods.VK_SNAPSHOT;
                case "PAUSE":
                case "BREAK": return 0x13;
                case "NUMLOCK": return 0x90;
                case "SCROLLLOCK":
                case "SCROLL": return 0x91;
                case "APPS":
                case "APP":
                case "CONTEXTMENU": return 0x5D;

                case "+":
                case "PLUS":
                case "=":
                case "EQUALS":
                case "EQUAL":
                case "OEMPLUS": return 0xBB;
                case "OEM102": return 0xE2;
                case "-":
                case "MINUS":
                case "SUBTRACT":
                case "UNDERSCORE":
                case "_":
                case "OEMMINUS": return 0xBD;
                case "[":
                case "{":
                case "LBRACKET":
                case "OPENBRACKET":
                case "OEM4":
                case "OEMOPENBRACKETS": return 0xDB;
                case "]":
                case "}":
                case "RBRACKET":
                case "CLOSEBRACKET":
                case "OEM6":
                case "OEMCLOSEBRACKETS": return 0xDD;
                case "\\":
                case "|":
                case "BACKSLASH":
                case "PIPE":
                case "OEM5":
                case "OEMPIPE": return 0xDC;
                case ";":
                case ":":
                case "SEMICOLON":
                case "COLON":
                case "OEM1":
                case "OEMSEMICOLON": return 0xBA;
                case "'":
                case "\"":
                case "QUOTE":
                case "APOSTROPHE":
                case "DOUBLEQUOTE":
                case "OEM7":
                case "OEMQUOTES": return 0xDE;
                case ",":
                case "<":
                case "COMMA":
                case "LESS":
                case "OEMCOMMA": return 0xBC;
                case ".":
                case ">":
                case "PERIOD":
                case "DOT":
                case "GREATER":
                case "OEMPERIOD": return 0xBE;
                case "/":
                case "?":
                case "SLASH":
                case "FORWARDSLASH":
                case "QUESTION":
                case "OEM2":
                case "OEMQUESTION": return 0xBF;
                case "`":
                case "~":
                case "BACKQUOTE":
                case "GRAVE":
                case "TILDE":
                case "OEM3":
                case "OEMTILDE": return 0xC0;
                case "OEM8": return 0xDF;

                case "VOLUP":
                case "VOLUMEUP": return 0xAF;
                case "VOLDOWN":
                case "VOLUMEDOWN": return 0xAE;
                case "MUTE":
                case "VOLUMEMUTE": return 0xAD;
                case "MEDIANEXT":
                case "NEXTTRACK": return 0xB0;
                case "MEDIAPREV":
                case "PREVTRACK": return 0xB1;
                case "MEDIASTOP": return 0xB2;
                case "MEDIAPLAY":
                case "PLAYPAUSE":
                case "PLAY": return 0xB3;

                case "BROWSERBACK":
                case "BACKNAV": return 0xA6;
                case "BROWSERFORWARD": return 0xA7;
                case "BROWSERREFRESH": return 0xA8;
                case "BROWSERSTOP": return 0xA9;
                case "BROWSERSEARCH": return 0xAA;
                case "BROWSERFAVORITES": return 0xAB;
                case "BROWSERHOME": return 0xAC;

                case "MULTIPLY":
                case "NUMPADMULTIPLY": return 0x6A;
                case "ADD":
                case "NUMPADADD": return 0x6B;
                case "SEPARATOR": return 0x6C;
                case "NUMPADSUBTRACT": return 0x6D;
                case "DECIMAL":
                case "NUMPADDECIMAL": return 0x6E;
                case "DIVIDE":
                case "NUMPADDIVIDE": return 0x6F;

                default: return 0;
            }
        }

        private static void SendSingleKey(byte vk)
        {
            List<NativeMethods.INPUT> inputs = new List<NativeMethods.INPUT>();
            inputs.Add(MakeKeyInput(vk, false));
            SendInputs(inputs);

            Thread.Sleep(25);

            List<NativeMethods.INPUT> up = new List<NativeMethods.INPUT>();
            up.Add(MakeKeyInput(vk, true));
            SendInputs(up);
        }

        private static void SendModifiedKey(byte modVk, byte keyVk)
        {
            // Step 1: Press modifier down
            List<NativeMethods.INPUT> modDown = new List<NativeMethods.INPUT>();
            modDown.Add(MakeKeyInput(modVk, false));
            SendInputs(modDown);

            // Sequential delay so the OS and target window register the modifier state
            Thread.Sleep(25);

            // Step 2: Press main key down
            List<NativeMethods.INPUT> keyDown = new List<NativeMethods.INPUT>();
            keyDown.Add(MakeKeyInput(keyVk, false));
            SendInputs(keyDown);

            // Dwell delay so the target window processes the shortcut
            Thread.Sleep(30);

            // Step 3: Release main key up
            List<NativeMethods.INPUT> keyUp = new List<NativeMethods.INPUT>();
            keyUp.Add(MakeKeyInput(keyVk, true));
            SendInputs(keyUp);

            // Short delay before releasing modifier
            Thread.Sleep(20);

            // Step 4: Release modifier up
            List<NativeMethods.INPUT> modUp = new List<NativeMethods.INPUT>();
            modUp.Add(MakeKeyInput(modVk, true));
            SendInputs(modUp);
        }

        private static void SendTwoModifiedKey(byte mod1, byte mod2, byte keyVk)
        {
            // Step 1: First modifier down
            List<NativeMethods.INPUT> m1Down = new List<NativeMethods.INPUT>();
            m1Down.Add(MakeKeyInput(mod1, false));
            SendInputs(m1Down);

            Thread.Sleep(20);

            // Step 2: Second modifier down
            List<NativeMethods.INPUT> m2Down = new List<NativeMethods.INPUT>();
            m2Down.Add(MakeKeyInput(mod2, false));
            SendInputs(m2Down);

            Thread.Sleep(25);

            // Step 3: Main key down
            List<NativeMethods.INPUT> keyDown = new List<NativeMethods.INPUT>();
            keyDown.Add(MakeKeyInput(keyVk, false));
            SendInputs(keyDown);

            Thread.Sleep(30);

            // Step 4: Main key up
            List<NativeMethods.INPUT> keyUp = new List<NativeMethods.INPUT>();
            keyUp.Add(MakeKeyInput(keyVk, true));
            SendInputs(keyUp);

            Thread.Sleep(20);

            // Step 5: Second modifier up
            List<NativeMethods.INPUT> m2Up = new List<NativeMethods.INPUT>();
            m2Up.Add(MakeKeyInput(mod2, true));
            SendInputs(m2Up);

            Thread.Sleep(20);

            // Step 6: First modifier up
            List<NativeMethods.INPUT> m1Up = new List<NativeMethods.INPUT>();
            m1Up.Add(MakeKeyInput(mod1, true));
            SendInputs(m1Up);
        }

        private static NativeMethods.INPUT MakeKeyInput(byte vk, bool keyUp)
        {
            NativeMethods.INPUT input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.u.ki.wVk = vk;
            input.u.ki.wScan = (ushort)NativeMethods.MapVirtualKey(vk, 0);
            input.u.ki.dwFlags = (keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0);

            if (vk == NativeMethods.VK_LWIN || vk == NativeMethods.VK_RWIN ||
                vk == NativeMethods.VK_RMENU || vk == NativeMethods.VK_RCONTROL ||
                vk == NativeMethods.VK_SNAPSHOT ||
                vk == NativeMethods.VK_LEFT || vk == NativeMethods.VK_UP ||
                vk == NativeMethods.VK_RIGHT || vk == NativeMethods.VK_DOWN ||
                vk == NativeMethods.VK_PRIOR || vk == NativeMethods.VK_NEXT ||
                vk == NativeMethods.VK_END || vk == NativeMethods.VK_HOME ||
                vk == NativeMethods.VK_INSERT || vk == NativeMethods.VK_DELETE ||
                vk == 0x5D ||
                (vk >= 0xA6 && vk <= 0xB3))
            {
                input.u.ki.dwFlags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
            }

            input.u.ki.time = 0;
            input.u.ki.dwExtraInfo = IntPtr.Zero;
            return input;
        }
    }
}
