using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace TouchAssistBall
{
    public class AppConfig
    {
        // Default Action Mapping (When in input box, click and long-press automatically trigger Backspace/Continuous Backspace)
        public ActionType ClickAction = ActionType.None;
        public ActionType LongPressAction = ActionType.HoldCurrentKey;

        public ActionType SwipeLeftAction = ActionType.Copy;
        public ActionType SwipeRightAction = ActionType.Paste;
        public ActionType SwipeUpAction = ActionType.VoiceTyping; // Default: Right Alt for Quark Qianwen / IME Voice Input
        public ActionType SwipeDownAction = ActionType.Screenshot;

        public string CustomClickKey = "";
        public string CustomLongPressKey = "";
        public string CustomSwipeLeftKey = "";
        public string CustomSwipeRightKey = "";
        public string CustomSwipeUpKey = "RAlt";
        public string CustomSwipeDownKey = "";

        public int BallSize = 64;
        public int SwipeThreshold = 25; // Crisp & accurate swipe distance in px
        public int LongPressThresholdMs = 350; // Long press threshold in ms
        public float NormalOpacity = 0.85f;
        public float ActiveOpacity = 1.0f;
        public float IdleOpacity = 0.22f; // Very low opacity when untouched for long time to reduce obstruction
        public int IdleTimeoutSec = 5;    // Seconds of inactivity before fading to IdleOpacity
        public bool StartWithWindows = false;
        public bool ShowDirectionHUD = true;

        public int PosX = -1;
        public int PosY = -1;

        private static string ConfigFilePath
        {
            get
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(appDir, "config.json");
            }
        }

        public static AppConfig Load()
        {
            AppConfig config = new AppConfig();
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string content = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                    config.ParseJson(content);
                }
            }
            catch { }
            return config;
        }

        public void Save()
        {
            try
            {
                string json = ToJson();
                File.WriteAllText(ConfigFilePath, json, Encoding.UTF8);
            }
            catch { }
        }

        public void ApplyAutoStart()
        {
            try
            {
                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    if (key != null)
                    {
                        string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (StartWithWindows)
                        {
                            key.SetValue("TouchAssistBall", "\"" + appPath + "\"");
                        }
                        else
                        {
                            if (key.GetValue("TouchAssistBall") != null)
                            {
                                key.DeleteValue("TouchAssistBall");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private string ToJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"ClickAction\": " + (int)ClickAction + ",");
            sb.AppendLine("  \"LongPressAction\": " + (int)LongPressAction + ",");
            sb.AppendLine("  \"SwipeLeftAction\": " + (int)SwipeLeftAction + ",");
            sb.AppendLine("  \"SwipeRightAction\": " + (int)SwipeRightAction + ",");
            sb.AppendLine("  \"SwipeUpAction\": " + (int)SwipeUpAction + ",");
            sb.AppendLine("  \"SwipeDownAction\": " + (int)SwipeDownAction + ",");
            sb.AppendLine("  \"CustomClickKey\": \"" + Escape(CustomClickKey) + "\",");
            sb.AppendLine("  \"CustomLongPressKey\": \"" + Escape(CustomLongPressKey) + "\",");
            sb.AppendLine("  \"CustomSwipeLeftKey\": \"" + Escape(CustomSwipeLeftKey) + "\",");
            sb.AppendLine("  \"CustomSwipeRightKey\": \"" + Escape(CustomSwipeRightKey) + "\",");
            sb.AppendLine("  \"CustomSwipeUpKey\": \"" + Escape(CustomSwipeUpKey) + "\",");
            sb.AppendLine("  \"CustomSwipeDownKey\": \"" + Escape(CustomSwipeDownKey) + "\",");
            sb.AppendLine("  \"BallSize\": " + BallSize + ",");
            sb.AppendLine("  \"SwipeThreshold\": " + SwipeThreshold + ",");
            sb.AppendLine("  \"LongPressThresholdMs\": " + LongPressThresholdMs + ",");
            sb.AppendLine("  \"NormalOpacity\": " + NormalOpacity.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
            sb.AppendLine("  \"ActiveOpacity\": " + ActiveOpacity.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
            sb.AppendLine("  \"IdleOpacity\": " + IdleOpacity.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
            sb.AppendLine("  \"IdleTimeoutSec\": " + IdleTimeoutSec + ",");
            sb.AppendLine("  \"StartWithWindows\": " + (StartWithWindows ? "true" : "false") + ",");
            sb.AppendLine("  \"ShowDirectionHUD\": " + (ShowDirectionHUD ? "true" : "false") + ",");
            sb.AppendLine("  \"PosX\": " + PosX + ",");
            sb.AppendLine("  \"PosY\": " + PosY);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private void ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            string[] lines = json.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (!trimmed.Contains(":")) continue;
                int colonIdx = trimmed.IndexOf(':');
                string key = trimmed.Substring(0, colonIdx).Trim(' ', '\"', '\'');
                string rawVal = trimmed.Substring(colonIdx + 1).Trim();
                if (rawVal.EndsWith(",")) rawVal = rawVal.Substring(0, rawVal.Length - 1).Trim();
                if (rawVal.EndsWith("}")) rawVal = rawVal.Substring(0, rawVal.Length - 1).Trim();

                string val = rawVal;
                if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2)
                {
                    val = val.Substring(1, val.Length - 2);
                    val = Unescape(val);
                }
                else
                {
                    val = val.Trim('\"', '\'');
                }

                switch (key)
                {
                    case "ClickAction":
                    case "ClickActionNormal":
                        ClickAction = (ActionType)ParseInt(val, (int)ClickAction);
                        break;
                    case "LongPressAction":
                    case "LongPressActionNormal":
                        LongPressAction = (ActionType)ParseInt(val, (int)LongPressAction);
                        break;
                    case "SwipeLeftAction": SwipeLeftAction = (ActionType)ParseInt(val, (int)SwipeLeftAction); break;
                    case "SwipeRightAction": SwipeRightAction = (ActionType)ParseInt(val, (int)SwipeRightAction); break;
                    case "SwipeUpAction": SwipeUpAction = (ActionType)ParseInt(val, (int)SwipeUpAction); break;
                    case "SwipeDownAction": SwipeDownAction = (ActionType)ParseInt(val, (int)SwipeDownAction); break;
                    case "CustomClickKey":
                    case "CustomClickNormalKey":
                        CustomClickKey = val;
                        break;
                    case "CustomLongPressKey":
                    case "CustomLongPressNormalKey":
                        CustomLongPressKey = val;
                        break;
                    case "CustomSwipeLeftKey": CustomSwipeLeftKey = val; break;
                    case "CustomSwipeRightKey": CustomSwipeRightKey = val; break;
                    case "CustomSwipeUpKey": CustomSwipeUpKey = val; break;
                    case "CustomSwipeDownKey": CustomSwipeDownKey = val; break;
                    case "BallSize": BallSize = ParseInt(val, 64); break;
                    case "SwipeThreshold": SwipeThreshold = ParseInt(val, 25); break;
                    case "LongPressThresholdMs": LongPressThresholdMs = ParseInt(val, 350); break;
                    case "NormalOpacity": NormalOpacity = ParseFloat(val, 0.85f); break;
                    case "ActiveOpacity": ActiveOpacity = ParseFloat(val, 1.0f); break;
                    case "IdleOpacity": IdleOpacity = ParseFloat(val, 0.22f); break;
                    case "IdleTimeoutSec": IdleTimeoutSec = ParseInt(val, 5); break;
                    case "AutoSnapToEdge": break; // Removed feature, ignore gracefully
                    case "StartWithWindows": StartWithWindows = ParseBool(val, false); break;
                    case "ShowDirectionHUD": ShowDirectionHUD = ParseBool(val, true); break;
                    case "PosX": PosX = ParseInt(val, -1); break;
                    case "PosY": PosY = ParseInt(val, -1); break;
                }
            }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private static int ParseInt(string s, int def)
        {
            int r;
            return int.TryParse(s, out r) ? r : def;
        }

        private static float ParseFloat(string s, float def)
        {
            float r;
            return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out r) ? r : def;
        }

        private static bool ParseBool(string s, bool def)
        {
            bool r;
            return bool.TryParse(s, out r) ? r : def;
        }
    }
}
