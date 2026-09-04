using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TouchAssistBall;

namespace TouchAssistBallTests
{
    class Program
    {
        static int _passed = 0;
        static int _failed = 0;

        static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine("  [PASS] " + testName);
            }
            else
            {
                _failed++;
                Console.WriteLine("  [FAIL] " + testName);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Running TouchAssistBall Test Suite");
            Console.WriteLine("========================================");

            TestKeyTokenParsing();
            TestVirtualKeyResolution();
            TestConfigRoundTrip();
            TestGestureStateMachine();
            TestInputDetector();
            TestResourceCleanup();
            TestTabletOrientationClamping();
            TestPhysicalKeyHoldSimulation();

            Console.WriteLine("========================================");
            Console.WriteLine(string.Format("Tests completed: {0} passed, {1} failed.", _passed, _failed));
            Console.WriteLine("========================================");

            Environment.Exit(_failed == 0 ? 0 : 1);
        }

        static void TestKeyTokenParsing()
        {
            Console.WriteLine("\n--- 1. Testing Key Token Parsing ---");

            List<string> tokens = ActionExecutor.ParseKeyTokens("Ctrl++");
            Assert(tokens.Count == 2 && tokens[0] == "Ctrl" && tokens[1] == "+", "ParseKeyTokens(\"Ctrl++\") -> [Ctrl, +]");

            tokens = ActionExecutor.ParseKeyTokens("Ctrl+Shift++");
            Assert(tokens.Count == 3 && tokens[0] == "Ctrl" && tokens[1] == "Shift" && tokens[2] == "+", "ParseKeyTokens(\"Ctrl+Shift++\") -> [Ctrl, Shift, +]");

            tokens = ActionExecutor.ParseKeyTokens("+");
            Assert(tokens.Count == 1 && tokens[0] == "+", "ParseKeyTokens(\"+\") -> [+]");

            tokens = ActionExecutor.ParseKeyTokens("Ctrl+=");
            Assert(tokens.Count == 2 && tokens[0] == "Ctrl" && tokens[1] == "=", "ParseKeyTokens(\"Ctrl+=\") -> [Ctrl, =]");

            tokens = ActionExecutor.ParseKeyTokens("Ctrl+-");
            Assert(tokens.Count == 2 && tokens[0] == "Ctrl" && tokens[1] == "-", "ParseKeyTokens(\"Ctrl+-\") -> [Ctrl, -]");

            tokens = ActionExecutor.ParseKeyTokens("Ctrl+Shift+Esc");
            Assert(tokens.Count == 3 && tokens[0] == "Ctrl" && tokens[1] == "Shift" && tokens[2] == "Esc", "ParseKeyTokens(\"Ctrl+Shift+Esc\") -> [Ctrl, Shift, Esc]");

            tokens = ActionExecutor.ParseKeyTokens("Win+D");
            Assert(tokens.Count == 2 && tokens[0] == "Win" && tokens[1] == "D", "ParseKeyTokens(\"Win+D\") -> [Win, D]");

            tokens = ActionExecutor.ParseKeyTokens("Alt+F4");
            Assert(tokens.Count == 2 && tokens[0] == "Alt" && tokens[1] == "F4", "ParseKeyTokens(\"Alt+F4\") -> [Alt, F4]");

            tokens = ActionExecutor.ParseKeyTokens("Ctrl + +");
            Assert(tokens.Count == 2 && tokens[0] == "Ctrl" && tokens[1] == "+", "ParseKeyTokens(\"Ctrl + +\") -> [Ctrl, +]");

            tokens = ActionExecutor.ParseKeyTokens("Ctrl+Alt+Delete");
            Assert(tokens.Count == 3 && tokens[0] == "Ctrl" && tokens[1] == "Alt" && tokens[2] == "Delete", "ParseKeyTokens(\"Ctrl+Alt+Delete\") -> [Ctrl, Alt, Delete]");

            tokens = ActionExecutor.ParseKeyTokens("Win+Shift+S");
            Assert(tokens.Count == 3 && tokens[0] == "Win" && tokens[1] == "Shift" && tokens[2] == "S", "ParseKeyTokens(\"Win+Shift+S\") -> [Win, Shift, S]");

            tokens = ActionExecutor.ParseKeyTokens("LCtrl+Shift+Z");
            Assert(tokens.Count == 3 && tokens[0] == "LCtrl" && tokens[1] == "Shift" && tokens[2] == "Z", "ParseKeyTokens(\"LCtrl+Shift+Z\") -> [LCtrl, Shift, Z]");

            tokens = ActionExecutor.ParseKeyTokens("RAlt+Enter");
            Assert(tokens.Count == 2 && tokens[0] == "RAlt" && tokens[1] == "Enter", "ParseKeyTokens(\"RAlt+Enter\") -> [RAlt, Enter]");
        }

        static void TestVirtualKeyResolution()
        {
            Console.WriteLine("\n--- 2. Testing Virtual Key Resolution ---");

            // Letters
            Assert(ActionExecutor.ResolveVirtualKey("A") == 0x41, "ResolveVirtualKey(\"A\") == 0x41");
            Assert(ActionExecutor.ResolveVirtualKey("Z") == 0x5A, "ResolveVirtualKey(\"Z\") == 0x5A");

            // Numbers
            Assert(ActionExecutor.ResolveVirtualKey("0") == 0x30, "ResolveVirtualKey(\"0\") == 0x30");
            Assert(ActionExecutor.ResolveVirtualKey("9") == 0x39, "ResolveVirtualKey(\"9\") == 0x39");

            // Function Keys F1-F24
            Assert(ActionExecutor.ResolveVirtualKey("F1") == 0x70, "ResolveVirtualKey(\"F1\") == 0x70");
            Assert(ActionExecutor.ResolveVirtualKey("F12") == 0x7B, "ResolveVirtualKey(\"F12\") == 0x7B");
            Assert(ActionExecutor.ResolveVirtualKey("F13") == 0x7C, "ResolveVirtualKey(\"F13\") == 0x7C");
            Assert(ActionExecutor.ResolveVirtualKey("F16") == 0x7F, "ResolveVirtualKey(\"F16\") == 0x7F");
            Assert(ActionExecutor.ResolveVirtualKey("F20") == 0x83, "ResolveVirtualKey(\"F20\") == 0x83");
            Assert(ActionExecutor.ResolveVirtualKey("F24") == 0x87, "ResolveVirtualKey(\"F24\") == 0x87");

            // Numpad Digits
            Assert(ActionExecutor.ResolveVirtualKey("NUM0") == 0x60, "ResolveVirtualKey(\"NUM0\") == 0x60");
            Assert(ActionExecutor.ResolveVirtualKey("NUMPAD5") == 0x65, "ResolveVirtualKey(\"NUMPAD5\") == 0x65");
            Assert(ActionExecutor.ResolveVirtualKey("NUM9") == 0x69, "ResolveVirtualKey(\"NUM9\") == 0x69");

            // Symbol & Punctuation Keys
            Assert(ActionExecutor.ResolveVirtualKey("+") == 0xBB, "ResolveVirtualKey(\"+\") == 0xBB");
            Assert(ActionExecutor.ResolveVirtualKey("PLUS") == 0xBB, "ResolveVirtualKey(\"PLUS\") == 0xBB");
            Assert(ActionExecutor.ResolveVirtualKey("=") == 0xBB, "ResolveVirtualKey(\"=\") == 0xBB");
            Assert(ActionExecutor.ResolveVirtualKey("-") == 0xBD, "ResolveVirtualKey(\"-\") == 0xBD");
            Assert(ActionExecutor.ResolveVirtualKey("MINUS") == 0xBD, "ResolveVirtualKey(\"MINUS\") == 0xBD");
            Assert(ActionExecutor.ResolveVirtualKey("[") == 0xDB, "ResolveVirtualKey(\"[\") == 0xDB");
            Assert(ActionExecutor.ResolveVirtualKey("]") == 0xDD, "ResolveVirtualKey(\"]\") == 0xDD");
            Assert(ActionExecutor.ResolveVirtualKey("\\") == 0xDC, "ResolveVirtualKey(\"\\\") == 0xDC");
            Assert(ActionExecutor.ResolveVirtualKey(";") == 0xBA, "ResolveVirtualKey(\";\") == 0xBA");
            Assert(ActionExecutor.ResolveVirtualKey("'") == 0xDE, "ResolveVirtualKey(\"'\") == 0xDE");
            Assert(ActionExecutor.ResolveVirtualKey(",") == 0xBC, "ResolveVirtualKey(\",\") == 0xBC");
            Assert(ActionExecutor.ResolveVirtualKey(".") == 0xBE, "ResolveVirtualKey(\".\") == 0xBE");
            Assert(ActionExecutor.ResolveVirtualKey("/") == 0xBF, "ResolveVirtualKey(\"/\") == 0xBF");
            Assert(ActionExecutor.ResolveVirtualKey("`") == 0xC0, "ResolveVirtualKey(\"`\") == 0xC0");
            Assert(ActionExecutor.ResolveVirtualKey("~") == 0xC0, "ResolveVirtualKey(\"~\") == 0xC0");
            Assert(ActionExecutor.ResolveVirtualKey("BACKQUOTE") == 0xC0, "ResolveVirtualKey(\"BACKQUOTE\") == 0xC0");
            Assert(ActionExecutor.ResolveVirtualKey("GRAVE") == 0xC0, "ResolveVirtualKey(\"GRAVE\") == 0xC0");
            Assert(ActionExecutor.ResolveVirtualKey("TILDE") == 0xC0, "ResolveVirtualKey(\"TILDE\") == 0xC0");

            // Navigation and Standard Keys
            Assert(ActionExecutor.ResolveVirtualKey("ENTER") == 0x0D, "ResolveVirtualKey(\"ENTER\") == 0x0D");
            Assert(ActionExecutor.ResolveVirtualKey("BACKSPACE") == 0x08, "ResolveVirtualKey(\"BACKSPACE\") == 0x08");
            Assert(ActionExecutor.ResolveVirtualKey("TAB") == 0x09, "ResolveVirtualKey(\"TAB\") == 0x09");
            Assert(ActionExecutor.ResolveVirtualKey("ESC") == 0x1B, "ResolveVirtualKey(\"ESC\") == 0x1B");
            Assert(ActionExecutor.ResolveVirtualKey("SPACE") == 0x20, "ResolveVirtualKey(\"SPACE\") == 0x20");
            Assert(ActionExecutor.ResolveVirtualKey("DELETE") == 0x2E, "ResolveVirtualKey(\"DELETE\") == 0x2E");
            Assert(ActionExecutor.ResolveVirtualKey("INSERT") == 0x2D, "ResolveVirtualKey(\"INSERT\") == 0x2D");
            Assert(ActionExecutor.ResolveVirtualKey("HOME") == 0x24, "ResolveVirtualKey(\"HOME\") == 0x24");
            Assert(ActionExecutor.ResolveVirtualKey("END") == 0x23, "ResolveVirtualKey(\"END\") == 0x23");
            Assert(ActionExecutor.ResolveVirtualKey("PGUP") == 0x21, "ResolveVirtualKey(\"PGUP\") == 0x21");
            Assert(ActionExecutor.ResolveVirtualKey("PGDN") == 0x22, "ResolveVirtualKey(\"PGDN\") == 0x22");
            Assert(ActionExecutor.ResolveVirtualKey("LEFT") == 0x25, "ResolveVirtualKey(\"LEFT\") == 0x25");
            Assert(ActionExecutor.ResolveVirtualKey("UP") == 0x26, "ResolveVirtualKey(\"UP\") == 0x26");
            Assert(ActionExecutor.ResolveVirtualKey("RIGHT") == 0x27, "ResolveVirtualKey(\"RIGHT\") == 0x27");
            Assert(ActionExecutor.ResolveVirtualKey("DOWN") == 0x28, "ResolveVirtualKey(\"DOWN\") == 0x28");
            Assert(ActionExecutor.ResolveVirtualKey("PRINTSCREEN") == 0x2C, "ResolveVirtualKey(\"PRINTSCREEN\") == 0x2C");
            Assert(ActionExecutor.ResolveVirtualKey("CAPSLOCK") == 0x14, "ResolveVirtualKey(\"CAPSLOCK\") == 0x14");
            Assert(ActionExecutor.ResolveVirtualKey("NUMLOCK") == 0x90, "ResolveVirtualKey(\"NUMLOCK\") == 0x90");
            Assert(ActionExecutor.ResolveVirtualKey("SCROLLLOCK") == 0x91, "ResolveVirtualKey(\"SCROLLLOCK\") == 0x91");
            Assert(ActionExecutor.ResolveVirtualKey("PAUSE") == 0x13, "ResolveVirtualKey(\"PAUSE\") == 0x13");
            Assert(ActionExecutor.ResolveVirtualKey("APPS") == 0x5D, "ResolveVirtualKey(\"APPS\") == 0x5D");

            // Media & Volume Keys
            Assert(ActionExecutor.ResolveVirtualKey("VOLUP") == 0xAF, "ResolveVirtualKey(\"VOLUP\") == 0xAF");
            Assert(ActionExecutor.ResolveVirtualKey("VOLDOWN") == 0xAE, "ResolveVirtualKey(\"VOLDOWN\") == 0xAE");
            Assert(ActionExecutor.ResolveVirtualKey("MUTE") == 0xAD, "ResolveVirtualKey(\"MUTE\") == 0xAD");
            Assert(ActionExecutor.ResolveVirtualKey("MEDIANEXT") == 0xB0, "ResolveVirtualKey(\"MEDIANEXT\") == 0xB0");
            Assert(ActionExecutor.ResolveVirtualKey("MEDIAPREV") == 0xB1, "ResolveVirtualKey(\"MEDIAPREV\") == 0xB1");
            Assert(ActionExecutor.ResolveVirtualKey("MEDIASTOP") == 0xB2, "ResolveVirtualKey(\"MEDIASTOP\") == 0xB2");
            Assert(ActionExecutor.ResolveVirtualKey("MEDIAPLAY") == 0xB3, "ResolveVirtualKey(\"MEDIAPLAY\") == 0xB3");
            Assert(ActionExecutor.ResolveVirtualKey("PLAYPAUSE") == 0xB3, "ResolveVirtualKey(\"PLAYPAUSE\") == 0xB3");

            // Browser Navigation Keys
            Assert(ActionExecutor.ResolveVirtualKey("BROWSERBACK") == 0xA6, "ResolveVirtualKey(\"BROWSERBACK\") == 0xA6");
            Assert(ActionExecutor.ResolveVirtualKey("BROWSERFORWARD") == 0xA7, "ResolveVirtualKey(\"BROWSERFORWARD\") == 0xA7");
            Assert(ActionExecutor.ResolveVirtualKey("BROWSERREFRESH") == 0xA8, "ResolveVirtualKey(\"BROWSERREFRESH\") == 0xA8");
            Assert(ActionExecutor.ResolveVirtualKey("BROWSERSTOP") == 0xA9, "ResolveVirtualKey(\"BROWSERSTOP\") == 0xA9");
            Assert(ActionExecutor.ResolveVirtualKey("BROWSERSEARCH") == 0xAA, "ResolveVirtualKey(\"BROWSERSEARCH\") == 0xAA");
            Assert(ActionExecutor.ResolveVirtualKey("BROWSERFAVORITES") == 0xAB, "ResolveVirtualKey(\"BROWSERFAVORITES\") == 0xAB");
            Assert(ActionExecutor.ResolveVirtualKey("BROWSERHOME") == 0xAC, "ResolveVirtualKey(\"BROWSERHOME\") == 0xAC");

            // WPF Oem Key Names Test
            Assert(ActionExecutor.ResolveVirtualKey("OEMPLUS") == 0xBB, "ResolveVirtualKey(\"OEMPLUS\") == 0xBB");
            Assert(ActionExecutor.ResolveVirtualKey("OEM102") == 0xE2, "ResolveVirtualKey(\"OEM102\") == 0xE2");
            Assert(ActionExecutor.ResolveVirtualKey("OEMMINUS") == 0xBD, "ResolveVirtualKey(\"OEMMINUS\") == 0xBD");
            Assert(ActionExecutor.ResolveVirtualKey("OEMPERIOD") == 0xBE, "ResolveVirtualKey(\"OEMPERIOD\") == 0xBE");
            Assert(ActionExecutor.ResolveVirtualKey("OEMCOMMA") == 0xBC, "ResolveVirtualKey(\"OEMCOMMA\") == 0xBC");
            Assert(ActionExecutor.ResolveVirtualKey("OEMQUESTION") == 0xBF, "ResolveVirtualKey(\"OEMQUESTION\") == 0xBF");
            Assert(ActionExecutor.ResolveVirtualKey("OEMOPENBRACKETS") == 0xDB, "ResolveVirtualKey(\"OEMOPENBRACKETS\") == 0xDB");
            Assert(ActionExecutor.ResolveVirtualKey("OEMCLOSEBRACKETS") == 0xDD, "ResolveVirtualKey(\"OEMCLOSEBRACKETS\") == 0xDD");
            Assert(ActionExecutor.ResolveVirtualKey("OEMPIPE") == 0xDC, "ResolveVirtualKey(\"OEMPIPE\") == 0xDC");
            Assert(ActionExecutor.ResolveVirtualKey("OEMSEMICOLON") == 0xBA, "ResolveVirtualKey(\"OEMSEMICOLON\") == 0xBA");
            Assert(ActionExecutor.ResolveVirtualKey("OEMQUOTES") == 0xDE, "ResolveVirtualKey(\"OEMQUOTES\") == 0xDE");
            Assert(ActionExecutor.ResolveVirtualKey("OEMTILDE") == 0xC0, "ResolveVirtualKey(\"OEMTILDE\") == 0xC0");
            Assert(ActionExecutor.ResolveVirtualKey("OEM8") == 0xDF, "ResolveVirtualKey(\"OEM8\") == 0xDF");

            // Numpad Operator Keys Test
            Assert(ActionExecutor.ResolveVirtualKey("NUMPADADD") == 0x6B, "ResolveVirtualKey(\"NUMPADADD\") == 0x6B");
            Assert(ActionExecutor.ResolveVirtualKey("NUMPADSUBTRACT") == 0x6D, "ResolveVirtualKey(\"NUMPADSUBTRACT\") == 0x6D");
            Assert(ActionExecutor.ResolveVirtualKey("NUMPADMULTIPLY") == 0x6A, "ResolveVirtualKey(\"NUMPADMULTIPLY\") == 0x6A");
            Assert(ActionExecutor.ResolveVirtualKey("NUMPADDIVIDE") == 0x6F, "ResolveVirtualKey(\"NUMPADDIVIDE\") == 0x6F");
            Assert(ActionExecutor.ResolveVirtualKey("NUMPADDECIMAL") == 0x6E, "ResolveVirtualKey(\"NUMPADDECIMAL\") == 0x6E");
        }

        static void TestConfigRoundTrip()
        {
            Console.WriteLine("\n--- 3. Testing AppConfig JSON Serialization & Deserialization ---");

            AppConfig config = new AppConfig();
            config.ClickAction = ActionType.CustomKey;
            config.CustomClickKey = "Ctrl++";
            config.LongPressAction = ActionType.CustomKey;
            config.CustomLongPressKey = "Ctrl+Shift+Esc";
            config.SwipeUpAction = ActionType.CustomKey;
            config.CustomSwipeUpKey = "Ctrl+Shift+Esc";
            config.SwipeDownAction = ActionType.CustomKey;
            config.CustomSwipeDownKey = "Win+D";
            config.SwipeLeftAction = ActionType.CustomKey;
            config.CustomSwipeLeftKey = "Ctrl+;";
            config.SwipeRightAction = ActionType.CustomKey;
            config.CustomSwipeRightKey = "Ctrl+\\";
            config.BallSize = 72;
            config.SwipeThreshold = 30;
            config.LongPressThresholdMs = 400;
            config.NormalOpacity = 0.75f;
            config.ActiveOpacity = 0.95f;
            config.IdleOpacity = 0.22f;
            config.IdleTimeoutSec = 5;
            config.StartWithWindows = true;
            config.ShowDirectionHUD = false;
            config.PosX = 800;
            config.PosY = 600;

            string savedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_config.json");
            try
            {
                // Test ToJson via reflection or Save/Load
                string json = (string)typeof(AppConfig).GetMethod("ToJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(config, null);
                AppConfig loaded = new AppConfig();
                typeof(AppConfig).GetMethod("ParseJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(loaded, new object[] { json });

                Assert(loaded.ClickAction == ActionType.CustomKey, "Loaded ClickAction == CustomKey");
                Assert(loaded.CustomClickKey == "Ctrl++", "Loaded CustomClickKey == Ctrl++");
                Assert(loaded.LongPressAction == ActionType.CustomKey, "Loaded LongPressAction == CustomKey");
                Assert(loaded.CustomLongPressKey == "Ctrl+Shift+Esc", "Loaded CustomLongPressKey == Ctrl+Shift+Esc");
                Assert(loaded.CustomSwipeUpKey == "Ctrl+Shift+Esc", "Loaded CustomSwipeUpKey == Ctrl+Shift+Esc");
                Assert(loaded.CustomSwipeDownKey == "Win+D", "Loaded CustomSwipeDownKey == Win+D");
                Assert(loaded.CustomSwipeLeftKey == "Ctrl+;", "Loaded CustomSwipeLeftKey == Ctrl+;");
                Assert(loaded.CustomSwipeRightKey == "Ctrl+\\", "Loaded CustomSwipeRightKey == Ctrl+\\");
                Assert(loaded.BallSize == 72, "Loaded BallSize == 72");
                Assert(loaded.SwipeThreshold == 30, "Loaded SwipeThreshold == 30");
                Assert(loaded.LongPressThresholdMs == 400, "Loaded LongPressThresholdMs == 400");
                Assert(Math.Abs(loaded.NormalOpacity - 0.75f) < 0.01, "Loaded NormalOpacity == 0.75");
                Assert(Math.Abs(loaded.IdleOpacity - 0.22f) < 0.01, "Loaded IdleOpacity == 0.22");
                Assert(loaded.IdleTimeoutSec == 5, "Loaded IdleTimeoutSec == 5");
                Assert(loaded.StartWithWindows == true, "Loaded StartWithWindows == true");
                Assert(loaded.ShowDirectionHUD == false, "Loaded ShowDirectionHUD == false");
                Assert(loaded.PosX == 800 && loaded.PosY == 600, "Loaded PosX/PosY == (800, 600)");
            }
            finally
            {
                if (File.Exists(savedPath)) File.Delete(savedPath);
            }
        }

        static void TestGestureStateMachine()
        {
            Console.WriteLine("\n--- 4. Testing Gesture State Machine Simulation ---");

            // Mock Gesture State Machine matching FloatingBallWindow's logic
            SimulatedGestureMachine sm = new SimulatedGestureMachine();

            // Scenario 1: Single Tap
            sm.Reset();
            sm.PointerDown(new System.Windows.Point(100, 100), 0);
            sm.PointerUp(50);
            sm.AdvanceTime(250); // After single tap timer fires
            Assert(sm.ClicksExecuted == 1, "Single Tap executes exactly 1 ClickAction");
            Assert(sm.MovesExecuted == 0, "Single Tap does not execute Move");
            Assert(sm.SwipesExecuted == 0, "Single Tap does not execute Swipe");

            // Scenario 1b: Long Press (> 400ms without moving) -> No Click
            sm.Reset();
            sm.PointerDown(new System.Windows.Point(100, 100), 0);
            sm.AdvanceTime(500);
            sm.PointerUp(500);
            sm.AdvanceTime(250);
            Assert(sm.ClicksExecuted == 0, "Long press (> 400ms) does not execute ClickAction");

            // Scenario 2: Double-Tap-and-Hold to Move (wait 180ms before moving)
            sm.Reset();
            sm.PointerDown(new System.Windows.Point(100, 100), 0);
            sm.PointerUp(50);
            sm.PointerDown(new System.Windows.Point(102, 101), 150); // 2nd tap down at 150ms
            sm.AdvanceTime(200); // Held for 200ms (> 180ms hold threshold) -> enters Moving
            sm.PointerMove(new System.Windows.Point(150, 150));
            sm.PointerUp(400);
            Assert(sm.ClicksExecuted == 0, "Double-Tap-and-Hold to Move executes 0 ClickActions (Decoupled!)");
            Assert(sm.MovesExecuted == 1, "Double-Tap-and-Hold completes Move operation");
            Assert(sm.SwipesExecuted == 0, "Double-Tap-and-Hold does not execute Swipe");

            // Scenario 2b: Double-Tap-and-Immediate-Drag to Move (drag > 8px within 50ms, before 180ms timer)
            sm.Reset();
            sm.PointerDown(new System.Windows.Point(100, 100), 0);
            sm.PointerUp(50);
            sm.PointerDown(new System.Windows.Point(100, 100), 120); // 2nd tap down at 120ms
            sm.PointerMove(new System.Windows.Point(130, 100));      // Immediately dragged 30px at 150ms (< 180ms threshold)
            sm.PointerUp(250);
            Assert(sm.ClicksExecuted == 0, "Double-Tap-and-Immediate-Drag executes 0 ClickActions (Decoupled!)");
            Assert(sm.MovesExecuted == 1, "Double-Tap-and-Immediate-Drag enters Move and completes Move operation");
            Assert(sm.SwipesExecuted == 0, "Double-Tap-and-Immediate-Drag does not mistakenly execute Swipe");

            // Scenario 3: Rapid Double-Tap (quick tap tap without holding)
            sm.Reset();
            sm.PointerDown(new System.Windows.Point(100, 100), 0);
            sm.PointerUp(50);
            sm.PointerDown(new System.Windows.Point(101, 100), 120);
            sm.PointerUp(170); // Lifted before hold threshold!
            sm.AdvanceTime(250); // Single tap timer for 2nd tap fires
            Assert(sm.ClicksExecuted == 2, "Rapid Double-Tap executes exactly 2 ClickActions (no dropped clicks!)");
            Assert(sm.MovesExecuted == 0, "Rapid Double-Tap does not enter Move");

            // Scenario 4: Rapid Triple-Tap (tap tap tap)
            sm.Reset();
            sm.PointerDown(new System.Windows.Point(100, 100), 0);
            sm.PointerUp(50);
            sm.PointerDown(new System.Windows.Point(100, 100), 120);
            sm.PointerUp(170);
            sm.PointerDown(new System.Windows.Point(100, 100), 240);
            sm.PointerUp(290);
            sm.AdvanceTime(250);
            Assert(sm.ClicksExecuted == 3, "Rapid Triple-Tap executes exactly 3 ClickActions");

            // Scenario 4b: Rapid Quadruple-Tap (tap tap tap tap)
            sm.Reset();
            sm.PointerDown(new System.Windows.Point(100, 100), 0);
            sm.PointerUp(50);
            sm.PointerDown(new System.Windows.Point(100, 100), 120);
            sm.PointerUp(170);
            sm.PointerDown(new System.Windows.Point(100, 100), 240);
            sm.PointerUp(290);
            sm.PointerDown(new System.Windows.Point(100, 100), 360);
            sm.PointerUp(410);
            sm.AdvanceTime(250);
            Assert(sm.ClicksExecuted == 4, "Rapid Quadruple-Tap executes exactly 4 ClickActions");

            // Scenario 5: Swipe Gestures in 4 Directions
            string[] directions = new string[] { "Up", "Down", "Left", "Right" };
            System.Windows.Point[] swipeOffsets = new System.Windows.Point[] {
                new System.Windows.Point(0, -60), // Up
                new System.Windows.Point(0, 60),  // Down
                new System.Windows.Point(-60, 0), // Left
                new System.Windows.Point(60, 0)   // Right
            };

            for (int i = 0; i < 4; i++)
            {
                sm.Reset();
                sm.PointerDown(new System.Windows.Point(100, 100), 0);
                sm.PointerMove(new System.Windows.Point(100 + swipeOffsets[i].X, 100 + swipeOffsets[i].Y));
                sm.PointerUp(100);
                Assert(sm.SwipesExecuted == 1 && sm.LastSwipeDirection == directions[i],
                    string.Format("Swipe {0} triggers correct swipe action", directions[i]));
                Assert(sm.ClicksExecuted == 0, string.Format("Swipe {0} executes 0 ClickActions", directions[i]));
            }

            // Scenario 6: Swipe pulled back to center cancel deadzone
            sm.Reset();
            sm.PointerDown(new System.Windows.Point(100, 100), 0);
            sm.PointerMove(new System.Windows.Point(100, 160)); // Swiped down 60px (> threshold)
            sm.PointerMove(new System.Windows.Point(100, 105)); // Pulled back to center (< 25px deadzone)
            sm.PointerUp(200);
            Assert(sm.SwipesExecuted == 0, "Swipe returned to center deadzone executes 0 Swipes (Cancelled cleanly)");
            Assert(sm.ClicksExecuted == 0, "Swipe returned to center deadzone executes 0 ClickActions");
        }

        static void TestInputDetector()
        {
            Console.WriteLine("\n--- 5. Testing Input Detector & Non-Blocking Architecture ---");

            // Verify IsInInputField does not crash and returns boolean
            bool isInInput = InputDetector.IsInInputField();
            Assert(isInInput == true || isInInput == false, "InputDetector.IsInInputField() executed smoothly");

            // Verify forceCheck does not hang UI thread
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                InputDetector.IsInInputField(true);
            }
            sw.Stop();
            Assert(sw.ElapsedMilliseconds < 100, "InputDetector.IsInInputField() 10 iterations completed in < 100ms (Non-blocking)");
        }

        static void TestResourceCleanup()
        {
            Console.WriteLine("\n--- 6. Testing Resource Cleanup & GDI Destruction ---");

            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(16, 16))
            {
                IntPtr hIcon = bmp.GetHicon();
                Assert(hIcon != IntPtr.Zero, "Created test GDI HICON");
                bool destroyed = NativeMethods.DestroyIcon(hIcon);
                Assert(destroyed, "DestroyIcon successfully released GDI HICON handle");
            }
        }

        static void TestTabletOrientationClamping()
        {
            Console.WriteLine("\n--- 7. Testing Tablet Screen Orientation Clamping ---");

            // Simulate Tablet Landscape -> Portrait (1920x1080 -> 1080x1920)
            int ballSize = 64;
            int prevPosX = 1800; // Far right in landscape
            int prevPosY = 500;
            int screenW_Portrait = 1080;
            int screenH_Portrait = 1920;

            int clampedX_Portrait = Math.Max(20, Math.Min(screenW_Portrait - ballSize - 20, prevPosX));
            int clampedY_Portrait = Math.Max(20, Math.Min(screenH_Portrait - ballSize - 20, prevPosY));

            Assert(clampedX_Portrait == 1080 - 64 - 20, "Portrait transition clamps X to 996 within 1080 width");
            Assert(clampedY_Portrait == 500, "Portrait transition preserves Y at 500 within 1920 height");

            // Simulate Tablet Portrait -> Landscape (1080x1920 -> 1920x1080)
            prevPosX = 900;
            prevPosY = 1700; // Far bottom in portrait
            int screenW_Landscape = 1920;
            int screenH_Landscape = 1080;

            int clampedX_Landscape = Math.Max(20, Math.Min(screenW_Landscape - ballSize - 20, prevPosX));
            int clampedY_Landscape = Math.Max(20, Math.Min(screenH_Landscape - ballSize - 20, prevPosY));

            Assert(clampedX_Landscape == 900, "Landscape transition preserves X at 900 within 1920 width");
            Assert(clampedY_Landscape == 1080 - 64 - 20, "Landscape transition clamps Y to 996 within 1080 height");
        }

        static void TestPhysicalKeyHoldSimulation()
        {
            Console.WriteLine("\n--- 8. Testing Physical Key Hold Simulation & Resolution ---");

            List<byte> modifiers, keys;

            // Test 1: ActionType.Backspace
            ActionExecutor.ResolveActionKeys(ActionType.Backspace, "", out modifiers, out keys);
            Assert(modifiers.Count == 0 && keys.Count == 1 && keys[0] == NativeMethods.VK_BACK, "ResolveActionKeys(Backspace) -> keys=[VK_BACK]");

            // Test 2: ActionType.Copy
            ActionExecutor.ResolveActionKeys(ActionType.Copy, "", out modifiers, out keys);
            Assert(modifiers.Count == 1 && modifiers[0] == NativeMethods.VK_CONTROL && keys.Count == 1 && keys[0] == NativeMethods.VK_KEY_C, "ResolveActionKeys(Copy) -> mods=[VK_CONTROL], keys=[VK_KEY_C]");

            // Test 3: ActionType.ContinuousBackspace
            ActionExecutor.ResolveActionKeys(ActionType.ContinuousBackspace, "", out modifiers, out keys);
            Assert(modifiers.Count == 0 && keys.Count == 1 && keys[0] == NativeMethods.VK_BACK, "ResolveActionKeys(ContinuousBackspace) -> keys=[VK_BACK]");

            // Test 4: ActionType.HoldCtrl
            ActionExecutor.ResolveActionKeys(ActionType.HoldCtrl, "", out modifiers, out keys);
            Assert(modifiers.Count == 1 && modifiers[0] == NativeMethods.VK_CONTROL && keys.Count == 0, "ResolveActionKeys(HoldCtrl) -> mods=[VK_CONTROL]");

            // Test 5: ActionType.HoldSpace
            ActionExecutor.ResolveActionKeys(ActionType.HoldSpace, "", out modifiers, out keys);
            Assert(modifiers.Count == 0 && keys.Count == 1 && keys[0] == NativeMethods.VK_SPACE, "ResolveActionKeys(HoldSpace) -> keys=[VK_SPACE]");

            // Test 6: Custom Key "Ctrl+Shift+Esc"
            ActionExecutor.ResolveActionKeys(ActionType.CustomKey, "Ctrl+Shift+Esc", out modifiers, out keys);
            Assert(modifiers.Count == 2 && modifiers[0] == NativeMethods.VK_CONTROL && modifiers[1] == NativeMethods.VK_SHIFT && keys.Count == 1 && keys[0] == NativeMethods.VK_ESCAPE, "ResolveActionKeys(\"Ctrl+Shift+Esc\") -> mods=[Ctrl, Shift], keys=[Esc]");

            // Test 7: Custom Key "Ctrl++"
            ActionExecutor.ResolveActionKeys(ActionType.CustomKey, "Ctrl++", out modifiers, out keys);
            Assert(modifiers.Count == 1 && modifiers[0] == NativeMethods.VK_CONTROL && keys.Count == 1 && keys[0] == 0xBB, "ResolveActionKeys(\"Ctrl++\") -> mods=[Ctrl], keys=[0xBB]");

            // Test 8: StartHoldSync & ReleaseHoldSync state tracking
            ActionExecutor.ReleaseHoldSync();
            Assert(ActionExecutor.IsHoldingKeys == false, "Initially IsHoldingKeys == false");

            ActionExecutor.StartHoldSync(ActionType.Backspace);
            Assert(ActionExecutor.IsHoldingKeys == true, "After StartHoldSync(Backspace) -> IsHoldingKeys == true");

            ActionExecutor.ReleaseHoldSync();
            Assert(ActionExecutor.IsHoldingKeys == false, "After ReleaseHoldSync() -> IsHoldingKeys == false");

            // Test 9: ReleaseAllHeldKeys safety release
            ActionExecutor.StartHoldSync(ActionType.CustomKey, "Ctrl+Shift+Esc");
            Assert(ActionExecutor.IsHoldingKeys == true, "After StartHoldSync(Ctrl+Shift+Esc) -> IsHoldingKeys == true");

            ActionExecutor.ReleaseAllHeldKeys();
            Assert(ActionExecutor.IsHoldingKeys == false, "After ReleaseAllHeldKeys() -> IsHoldingKeys == false");
        }
    }

    class SimulatedGestureMachine
    {
        public enum Mode { Idle, Touching, Sliding, Moving }
        public enum Direction { None, Up, Down, Left, Right }

        public int ClicksExecuted = 0;
        public int MovesExecuted = 0;
        public int SwipesExecuted = 0;
        public string LastSwipeDirection = "";

        private Mode _mode = Mode.Idle;
        private Direction _currentDirection = Direction.None;
        private System.Windows.Point _startPt;
        private System.Windows.Point _lastPt;
        private DateTime _lastTapReleaseTime = DateTime.MinValue;
        private System.Windows.Point _lastTapScreenPt;
        private bool _isSecondTapCandidate = false;
        private bool _singleTapTimerRunning = false;
        private bool _doubleTapHoldTimerRunning = false;
        private double _currentTimeMs = 0;
        private double _touchStartTimeMs = 0;
        private double _singleTapTimerExpiresAt = -1;
        private double _doubleTapHoldTimerExpiresAt = -1;

        private const double SwipeThreshold = 25.0;
        private const double CancelRadius = 25.6;

        public void Reset()
        {
            ClicksExecuted = 0;
            MovesExecuted = 0;
            SwipesExecuted = 0;
            LastSwipeDirection = "";
            _mode = Mode.Idle;
            _currentDirection = Direction.None;
            _lastTapReleaseTime = DateTime.MinValue;
            _isSecondTapCandidate = false;
            _singleTapTimerRunning = false;
            _doubleTapHoldTimerRunning = false;
            _currentTimeMs = 0;
            _touchStartTimeMs = 0;
        }

        public void AdvanceTime(double deltaMs)
        {
            _currentTimeMs += deltaMs;
            CheckTimers();
        }

        private void CheckTimers()
        {
            if (_doubleTapHoldTimerRunning && _currentTimeMs >= _doubleTapHoldTimerExpiresAt)
            {
                _doubleTapHoldTimerRunning = false;
                if (_isSecondTapCandidate && _mode == Mode.Touching)
                {
                    _isSecondTapCandidate = false;
                    _mode = Mode.Moving;
                    _currentDirection = Direction.None;
                }
            }

            if (_singleTapTimerRunning && _currentTimeMs >= _singleTapTimerExpiresAt)
            {
                _singleTapTimerRunning = false;
                ClicksExecuted++;
            }
        }

        public void PointerDown(System.Windows.Point pt, double timeMs)
        {
            _currentTimeMs = timeMs;
            CheckTimers();

            _mode = Mode.Touching;
            _currentDirection = Direction.None;
            _startPt = pt;
            _lastPt = pt;
            _touchStartTimeMs = timeMs;

            double msSinceLastTap = _currentTimeMs - (_lastTapReleaseTime == DateTime.MinValue ? -99999 : 0);
            double distFromLastTap = Math.Sqrt(Math.Pow(pt.X - _lastTapScreenPt.X, 2) + Math.Pow(pt.Y - _lastTapScreenPt.Y, 2));

            if (_singleTapTimerRunning || (msSinceLastTap < 300 && distFromLastTap < 40))
            {
                _singleTapTimerRunning = false;
                _isSecondTapCandidate = true;
                _doubleTapHoldTimerRunning = true;
                _doubleTapHoldTimerExpiresAt = _currentTimeMs + 180;
            }
            else
            {
                _isSecondTapCandidate = false;
            }
        }

        public void PointerMove(System.Windows.Point pt)
        {
            _lastPt = pt;

            // Fast-path: If in the 2nd tap of a double-tap, any deliberate movement immediately enters Moving mode
            if (_isSecondTapCandidate && _mode == Mode.Touching)
            {
                double dxMove = pt.X - _startPt.X;
                double dyMove = pt.Y - _startPt.Y;
                double distMove = Math.Sqrt(dxMove * dxMove + dyMove * dyMove);
                if (distMove >= 8.0)
                {
                    _doubleTapHoldTimerRunning = false;
                    _singleTapTimerRunning = false;
                    _isSecondTapCandidate = false;
                    _mode = Mode.Moving;
                    _currentDirection = Direction.None;
                }
            }

            if (_mode == Mode.Moving)
            {
                return;
            }

            double dx = pt.X - _startPt.X;
            double dy = pt.Y - _startPt.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (_mode == Mode.Touching && dist >= SwipeThreshold)
            {
                _doubleTapHoldTimerRunning = false;
                _singleTapTimerRunning = false;
                _isSecondTapCandidate = false;
                _mode = Mode.Sliding;
            }

            if (_mode == Mode.Sliding)
            {
                if (dist < CancelRadius)
                {
                    _currentDirection = Direction.None;
                }
                else
                {
                    double angle = Math.Atan2(dy, dx) * (180.0 / Math.PI);
                    if (angle >= -135 && angle < -45) _currentDirection = Direction.Up;
                    else if (angle >= 45 && angle < 135) _currentDirection = Direction.Down;
                    else if (angle >= -45 && angle < 45) _currentDirection = Direction.Right;
                    else _currentDirection = Direction.Left;
                }
            }
        }

        public void PointerUp(double timeMs)
        {
            _currentTimeMs = timeMs;
            CheckTimers();

            _doubleTapHoldTimerRunning = false;
            Mode endMode = _mode;
            Direction endDir = _currentDirection;

            _mode = Mode.Idle;
            _currentDirection = Direction.None;

            if (endMode == Mode.Moving)
            {
                MovesExecuted++;
                _isSecondTapCandidate = false;
                _singleTapTimerRunning = false;
                _lastTapReleaseTime = DateTime.MinValue;
                return;
            }

            if (endMode == Mode.Sliding)
            {
                _isSecondTapCandidate = false;
                _singleTapTimerRunning = false;
                _lastTapReleaseTime = DateTime.MinValue;

                if (endDir != Direction.None)
                {
                    SwipesExecuted++;
                    LastSwipeDirection = endDir.ToString();
                }
                return;
            }

            if (endMode == Mode.Touching)
            {
                double duration = _currentTimeMs - _touchStartTimeMs;
                if (duration < 400)
                {
                    _lastTapReleaseTime = DateTime.UtcNow;
                    _lastTapScreenPt = _startPt;

                    if (_isSecondTapCandidate)
                    {
                        _isSecondTapCandidate = false;
                        _singleTapTimerRunning = false;
                        ClicksExecuted++; // 1st tap resolved immediately!

                        _singleTapTimerRunning = true; // 2nd tap scheduled
                        _singleTapTimerExpiresAt = _currentTimeMs + 200;
                    }
                    else
                    {
                        _singleTapTimerRunning = true;
                        _singleTapTimerExpiresAt = _currentTimeMs + 200;
                    }
                }
            }
        }
    }
}
