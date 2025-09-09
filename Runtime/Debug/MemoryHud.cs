#if UNITY_EDITOR
using System;
using UnityEngine;
#if UNITY_2019_4_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace GGemCo2DCore
{
    /// <summary>에디터/개발 빌드에서 메모리 지표 출력</summary>
    public class MemoryHud : MonoBehaviour
    {
        [Range(0.1f, 2f)] public float updateInterval = 0.5f;

        private float _next;
        private string _text = "";
        private long _monoUsed, _totalAlloc, _totalReserved, _gcBytes;
        private float _allocPerFrameAvg;
        private float _accumAlloc; private int _frames;
        private GGemCoDebugHudRoot _root;

        private void Awake() { _root = FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include); }

        private void Update()
        {
            // GC 할당량 추정(프레임 평균): Time.deltaTime 내 GC.Allocate 추정은 불가 → 누적 평균으로 단순화
            _accumAlloc += (float)GC.GetTotalMemory(false);
            _frames++;

            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + updateInterval;

#if UNITY_2019_4_OR_NEWER
            _monoUsed      = Profiler.GetMonoUsedSizeLong();
            _totalAlloc    = Profiler.GetTotalAllocatedMemoryLong();
            _totalReserved = Profiler.GetTotalReservedMemoryLong();
#else
            _monoUsed = GC.GetTotalMemory(false);
            _totalAlloc = _monoUsed; _totalReserved = 0;
#endif
            _gcBytes = GC.GetTotalMemory(false);
            _allocPerFrameAvg = (_frames > 0) ? (_accumAlloc/_frames) : 0f;

            _text = $"[Memory]\n" +
                    $"Total Alloc:  {FormatBytes(_totalAlloc)}\n" +
                    $"Total Reserv: {FormatBytes(_totalReserved)}\n" +
                    $"Mono Used:    {FormatBytes(_monoUsed)}\n" +
                    $"GC Snapshot:  {FormatBytes(_gcBytes)}\n" +
                    $"GC/Frame(avg est): {FormatBytes((long)_allocPerFrameAvg)}";
        }

        private static string FormatBytes(long b)
        {
            const long KB=1024, MB=KB*1024, GB=MB*1024;
            if (b>=GB) return $"{(double)b/GB:0.00} GB";
            if (b>=MB) return $"{(double)b/MB:0.00} MB";
            if (b>=KB) return $"{(double)b/KB:0.00} KB";
            return $"{b} B";
        }

        private void OnGUI()
        {
            var content = new GUIContent(_text);
            // ★ 변경: _root.Style -> _root.GetStyle()
            var style = _root ? _root.GetStyle() : GUI.skin.box;
            Vector2 pad = _root ? _root.padding : new Vector2(8,8);
            Vector2 size = style.CalcSize(content);
            var r = new Rect(Screen.width - size.x - pad.x, Screen.height - size.y - pad.y, size.x + 10, size.y + 10);
            if (_root) _root.DrawBox(r, content); else GUI.Box(r, content, style);
        }
    }
}
#endif
