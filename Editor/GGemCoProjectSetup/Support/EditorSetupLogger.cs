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

        /// <summary>로그 파일 경로</summary>
        public string LogPath => _logPath;

        /// <summary>
        /// 로그 라인이 추가될 때마다 호출되는 이벤트입니다. (실시간 UI 표시 용도)
        /// </summary>
        public event Action<string> OnLineAppended;

        public EditorSetupLogger(string fileNamePrefix = "GGemCo_ProjectSetup")
        {
            var dir = ConfigProjectSetup.DirLog;
            Directory.CreateDirectory(dir);
            _logPath = Path.GetFullPath(Path.Combine(dir, $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.log"));
            Info($"[Start] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        public void Info(string msg)  => Write("INFO", msg, Debug.Log);
        public void Warn(string msg)  => Write("WARN", msg, Debug.LogWarning);
        public void Error(string msg) => Write("ERROR", msg, Debug.LogError);

        private void Write(string level, string msg, Action<string> console)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {msg}";
            _sb.AppendLine(line);

            try { console?.Invoke(msg); } catch { /* ignore */ }

            try { OnLineAppended?.Invoke(line); } catch { /* ignore */ }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                File.WriteAllText(_logPath, _sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SetupLogger Dispose 실패: {ex}");
            }

            _disposed = true;
        }
    }
}
#endif
