#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>FPS/FrameTime HUD</summary>
    public class FpsHud : MonoBehaviour
    {
        [Range(0.1f, 2f)] public float updateInterval = 0.5f;
        public int historySize = 100;

        private float _accum, _timeLeft;
        private int _frames;
        private readonly Queue<float> _frameTimes = new();
        private float _avgMs, _minMs = float.MaxValue, _maxMs;
        private GGemCoDebugHudRoot _root;

        private void Awake()
        {
            _root = FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include);
            _timeLeft = updateInterval;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime * 1000f;
            _accum += dt;
            _frames++;
            _timeLeft -= Time.unscaledDeltaTime;

            _frameTimes.Enqueue(dt);
            while (_frameTimes.Count > historySize) _frameTimes.Dequeue();

            if (_timeLeft <= 0f)
            {
                float sum = 0f, min = float.MaxValue, max = 0f;
                foreach (var ms in _frameTimes) { sum += ms; if (ms < min) min = ms; if (ms > max) max = ms; }
                _avgMs = (_frameTimes.Count > 0) ? sum / _frameTimes.Count : 0f;
                _minMs = (min == float.MaxValue) ? 0f : min;
                _maxMs = max;
                _timeLeft = updateInterval;
                _accum = 0f; _frames = 0;
            }
        }

        private void OnGUI()
        {
            string text = $"[FPS]\n" +
                          $"Avg: {(_avgMs>0? 1000f/_avgMs:0f):0.0} fps ({_avgMs:0.0} ms)\n" +
                          $"Best: {( _minMs>0? 1000f/_minMs:0f):0.0} fps ({_minMs:0.0} ms)\n" +
                          $"Worst: {( _maxMs>0? 1000f/_maxMs:0f):0.0} fps ({_maxMs:0.0} ms)";

            var content = new GUIContent(text);
            // ★ 변경: _root.Style -> _root.GetStyle()
            var style = _root ? _root.GetStyle() : GUI.skin.box;
            Vector2 pad = _root ? _root.padding : new Vector2(8,8);
            Vector2 size = style.CalcSize(content);
            var r = new Rect(Screen.width - size.x - pad.x, pad.y, size.x + 10, size.y + 10);
            if (_root) _root.DrawBox(r, content); else GUI.Box(r, content, style);
        }
    }
}
#endif