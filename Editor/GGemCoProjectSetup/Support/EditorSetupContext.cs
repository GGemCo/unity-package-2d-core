#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace GGemCo2DCoreEditor
{
    public sealed class EditorSetupContext
    {
        public readonly EditorSetupLogger Logger;
        public readonly DateTime StartedAt = DateTime.Now;

        // 실행 중 공유 데이터 버스
        private readonly Dictionary<string, UnityEngine.Object> _shared = new();
        
        public void SetShared(string key, UnityEngine.Object obj) => _shared[key] = obj;
        public T GetShared<T>(string key) where T : UnityEngine.Object =>
            _shared.TryGetValue(key, out var o) ? o as T : null;
        
        public EditorSetupContext(EditorSetupLogger logger)
        {
            Logger = logger;
        }
    }
}
#endif