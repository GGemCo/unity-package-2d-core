#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>콘솔 + 파일 동시 기록</summary>
    public sealed class EditorSetupLogger : IDisposable
    {
        private readonly StringBuilder _sb = new StringBuilder(1024);
        private readonly string _logPath;
        private bool _disposed;

        public string LogPath => _logPath;

        public EditorSetupLogger(string fileNamePrefix = "GGemCo_ProjectSetup")
        {
            var dir = ConfigProjectSetup.DirLog;
            Directory.CreateDirectory(dir);
            _logPath = Path.GetFullPath(Path.Combine(dir, $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.log"));
            Info($"[Start] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        public void Info(string msg)  { Append("INFO",  msg); Debug.Log(msg); }
        public void Warn(string msg)  { Append("WARN",  msg); Debug.LogWarning(msg); }
        public void Error(string msg) { Append("ERROR", msg); Debug.LogError(msg); }

        private void Append(string level, string msg)
        {
            _sb.AppendLine($"{DateTime.Now:HH:mm:ss.fff} [{level}] {msg}");
        }

        public void Dispose()
        {
            if (_disposed) return;
            File.WriteAllText(_logPath, _sb.ToString(), Encoding.UTF8);
            _disposed = true;
        }
    }
}
#endif