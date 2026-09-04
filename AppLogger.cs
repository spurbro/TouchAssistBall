using System;
using System.IO;
using System.Text;

namespace TouchAssistBall
{
    public static class AppLogger
    {
        private static string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_run.log");

        private static readonly object _logLock = new object();
        private const long MAX_LOG_BYTES = 1024 * 1024; // 1 MB limit

        public static void Log(string msg)
        {
            try
            {
                lock (_logLock)
                {
                    FileInfo fi = new FileInfo(LogFile);
                    if (fi.Exists && fi.Length > MAX_LOG_BYTES)
                    {
                        string bak = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_run.bak");
                        try { if (File.Exists(bak)) File.Delete(bak); File.Move(LogFile, bak); } catch { }
                    }
                    File.AppendAllText(LogFile, string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] {1}\r\n", DateTime.Now, msg), Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
