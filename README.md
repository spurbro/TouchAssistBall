<div align="center">

<img src="app.png" alt="TouchAssistBall Logo" width="128" height="128" />

# TouchAssistBall (触屏桌面悬浮球)

**A high-performance, zero-intrusive, fully customizable assistive floating ball for Windows touch & tablet devices.**  
**专为 Windows 触屏与二合一设备打造的高性能、零焦点打扰、全键位自定义桌面辅助悬浮球。**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![Framework](https://img.shields.io/badge/.NET-4.0%2B-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23%20%2F%20WPF-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/spurbro/TouchAssistBall/pulls)

[English](#-english) • [简体中文](#-简体中文)

</div>

---

## 🌐 English

### 💡 Overview
**TouchAssistBall** is an ultra-lightweight, hardware-accelerated assistive touch ball specifically engineered for Windows touchscreen devices (Microsoft Surface, 2-in-1 laptops, rugged tablets). Built with pure C# and WPF Direct3D/DWM compositing, it provides an intuitive floating touch controller with zero focus stealing, instant typing input detection, radial gestures, and deep idle transparency.

### ✨ Key Features

- **🛡️ Zero Focus Stealing (`WS_EX_NOACTIVATE`)**: Clicking, dragging, or swiping the ball never deactivates or steals focus from underlying foreground windows (Word, Edge, VS Code, WeChat, etc.).
- **👻 Smart Idle Auto-Fade**: Automatically fades into a subtle 22% translucent glass state after 5 seconds of inactivity to eliminate screen occlusion. Instantly awakens to 100% upon the lightest touch.
- **🎯 Intelligent Input Field Sensing**: Actively detects text caret/input focus without UI thread blocking. Automatically switches to backspace mode (`⌫`) when typing and remains passive when browsing.
- **⚡ Hardware-Grade Typematic Auto-Repeat**: Long-pressing the ball in an input field triggers continuous rapid keystrokes (250ms initial delay, followed by 55ms rapid pulse rate) for smooth deleting.
- **🧭 True Radial 4-Direction Gestures**: Swipe out from the center in any direction to trigger assigned actions:
  - 👈 **Swipe Left**: Copy (`Ctrl + C`)
  - 👉 **Swipe Right**: Paste (`Ctrl + V`)
  - 👇 **Swipe Down**: Screenshot Snipping (`Win + Shift + S`)
  - 👆 **Swipe Up**: Enter Key (`Enter`)
  - 🔄 **Center Deadzone Cancel**: Drag back into the center dashed ring to cancel cleanly.
- **🎮 100% Free Key Remapping**: Bind any action to custom keyboard shortcuts (including `Win+Shift+S`, `Ctrl+Shift+Esc`, `Alt+F4`, `Ctrl++`, modifier keys, and media navigation).
- **🧲 Free Anywhere Hover (Zero Edge Snapping)**: Complete freedom of positioning anywhere on single or multi-monitor setups without forced edge-docking.
- **🖊️ Surface Stylus & Pen Compatible**: Seamlessly supports touch screen fingers, electromagnetic digitizer pens, and standard mice.

### 🚀 Quick Start

1. Download or compile `TouchAssistBall.exe`.
2. Double-click `TouchAssistBall.exe` (or your desktop shortcut).
3. The assistive ball appears on your screen and the notification tray icon is activated.
4. **Move the Ball**: Rapid double-tap, then hold and drag on the 2nd tap (or right-click the ball/tray and select "Drag Position").
5. **Open Settings**: Right-click the floating ball, or double-click the system tray icon to configure custom keys, gesture thresholds, and idle timers.

### 🛠️ Building from Source

No heavy Visual Studio installation required! Build in 2 seconds using the built-in Windows .NET Framework compiler:

```cmd
# Clone repository
git clone https://github.com/spurbro/TouchAssistBall.git
cd TouchAssistBall

# Build standalone executable with embedded app icon
build.bat
```

---

## 🇨🇳 简体中文

### 💡 项目简介
**TouchAssistBall (触屏桌面悬浮球)** 是专为 Windows 触控屏设备（微软 Surface、二合一笔记本、工控触控平板）量身打造的高性能、低延迟、零焦点抢占的桌面手势增强助手。基于原生 C# 与 WPF Direct3D/DWM 硬件加速图层构建，完美融合现代 Windows 11 Fluent 视觉与极致顺滑的指尖操控。

### ✨ 核心特性

- **🛡️ 免激活防抢焦点（`WS_EX_NOACTIVATE`）**：在文档、浏览器、终端或聊天软件中，触碰悬浮球绝不夺取焦点，当前文本光标位置 100% 保持原样。
- **👻 自动闲置深度淡化防遮挡**：停止操作 5 秒后，悬浮球通过平滑缓动动画淡化至 **22% 磨砂超低不透明度**，彻底杜绝视线遮挡；指尖轻触瞬间 100% 满血亮起。
- **🎯 智能输入框感知**：智能探查当前前台光标状态，位于输入框时光标球亮起并变为 `⌫` 退格标识，非输入框时保持静默防误触。
- **⚡ 硬件级连续击键脉冲（Typematic Repeat）**：长按退格键时模拟真实键盘重复逻辑（初始 250ms 延迟，随后以 55ms 频率连续输入退格，抬手即停），连删极为跟手顺畅。
- **🧭 几何中心真球心四向手势**：
  - 👈 **往左滑**：复制 (`Ctrl + C`)
  - 👉 **往右滑**：粘贴 (`Ctrl + V`)
  - 👇 **往下滑**：系统截屏 (`Win + Shift + S`)
  - 👆 **往上滑**：回车换行 (`Enter`)
  - 🔄 **中途防误触撤销**：滑动中途拉回中心虚线圆环松手即可取消触发。
- **🎮 100% 自由按键映射**：支持任意组合键（如 `Ctrl+Alt+A`、`Ctrl+Shift+Esc`、`Win+D`、`Ctrl++`、左右修饰键区分），支持键盘一键录制与触屏虚拟按键助手。
- **🧲 全屏自由就地悬停（彻底删除强制贴边）**：拖拽到屏幕任意角落松手即停，支持多显示器跨屏拖拽，四周留出充足手势交互空间。
- **🖊️ Surface 手写笔与触控屏深度兼容**：原生区分手指电容触控与电磁手写笔（Stylus），数位板笔尖点击与拖拽不再失灵。

### 📋 默认手势对照表

| 触发操作 | 场景 | 默认执行动作 | 说明 |
| :--- | :--- | :--- | :--- |
| **轻点一下 (Tap)** | 输入框内 | 退格删除 (`Backspace`) | 0 延迟即点即删，可自定义 |
| **轻点一下 (Tap)** | 非输入框 | 静默无动作 | 杜绝平时误触 |
| **长按保持 (Hold)** | 输入框内 | 硬件级连续退格 | 55ms 高频重复脉冲，抬手即止 |
| **左滑动 (Swipe Left)** | 全局 | 复制 (`Ctrl + C`) | 划出球体边缘即触发 |
| **右滑动 (Swipe Right)** | 全局 | 粘贴 (`Ctrl + V`) | 划出球体边缘即触发 |
| **下滑动 (Swipe Down)** | 全局 | 区域截屏 (`Win + Shift + S`) | 快速调出 Windows 截屏工具 |
| **上滑动 (Swipe Up)** | 全局 | 回车换行 (`Enter`) | 快速确认或换行 |
| **双击并在第 2 下按住** | 全局 | ✥ 自由拖拽移动悬浮球 | 1:1 跟手移动，松开即就地停靠 |

### 🚀 运行与使用

1. **直接运行**：双击运行生成的 `TouchAssistBall.exe`（单文件绿色程序，零依赖外部运行时）。
2. **移动位置**：
   - 方式一：快速轻点第 1 下，紧接着第 2 下按住变微标 `✥` 即可拖动；
   - 方式二：右键点击悬浮球，选择【✥ 拖动悬浮球位置】或【📍 重置位置到屏幕右侧】。
3. **个性化设置**：右键点击悬浮球选择【⚙ 设置】，或双击右下角任务栏托盘图标即可打开可视化设置面板。

### 🛠️ 源码极速构建

项目使用 Windows 自带的 .NET 4.0 `csc.exe` 极速单命令编译，无需配置大型 Visual Studio IDE：

```cmd
# 1. 克隆代码仓库
git clone https://github.com/spurbro/TouchAssistBall.git
cd TouchAssistBall

# 2. 执行编译批处理（自动内嵌 Gemini 设计应用图标）
build.bat
```

编译成功后将在根目录生成自带多分辨率系统图标的 `TouchAssistBall.exe`。

---

## 📂 Project Structure (项目目录结构)

```text
TouchAssistBall/
├── ActionExecutor.cs      # Win32 SendInput 硬件级按键模拟与脉冲连删
├── App.cs                 # 应用程序入口、任务栏托盘常驻与生命周期
├── AppConfig.cs           # 配置模型与 JSON 持久化管理
├── AppLogger.cs           # 轻量日志记录器（支持 1MB 自动轮转）
├── FloatingBallWindow.cs  # WPF Direct3D 悬浮球窗体、手势状态机与闲置动画
├── InputDetector.cs       # 智能前台窗口与文本输入焦点检测（防卡顿节流）
├── NativeMethods.cs       # Win32 原生 P/Invoke API 声明
├── SettingsWindow.cs      # WPF 全可视化按键自定义与触控外观设置面板
├── Tests.cs               # 包含 159 项自动化断言的单元测试套件
├── app.ico                # 包含 256x256 ~ 16x16 规格的 Windows PE 多分辨率图标
├── app.png                # 512x512 高清流体发光科技球图标
├── build.bat              # 一键编译脚本
├── config.json            # 用户按键映射与手势参数配置文件
└── README.md              # 中英文双语项目使用与开发文档
```

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
