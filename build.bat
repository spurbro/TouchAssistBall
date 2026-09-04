@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set WPF=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF

"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:app.ico /out:TouchAssistBall.exe /r:%WPF%\PresentationFramework.dll /r:%WPF%\PresentationCore.dll /r:%WPF%\WindowsBase.dll /r:%WPF%\UIAutomationClient.dll /r:%WPF%\UIAutomationTypes.dll /r:System.Xaml.dll /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll AppLogger.cs NativeMethods.cs InputDetector.cs ActionExecutor.cs AppConfig.cs SettingsWindow.cs FloatingBallWindow.cs App.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] Built TouchAssistBall.exe with Gemini application icon successfully!
) else (
    echo [ERROR] Build failed!
)
