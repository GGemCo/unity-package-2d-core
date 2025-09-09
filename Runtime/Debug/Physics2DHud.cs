#if UNITY_EDITOR
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>간이 Physics2D 런타임 지표</summary>
    public class Physics2DHud : MonoBehaviour
    {
        [Range(0.1f, 2f)] public float updateInterval = 0.5f;

        private float _next;
        private int _rigidbodies, _colliders, _triggers;
        private GGemCoDebugHudRoot _root;

        private void Awake() { _root = FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include); }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + updateInterval;

            var rbs = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
            var cols = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            _rigidbodies = rbs.Length;
            _colliders = cols.Length;
            _triggers = 0;
            foreach (var c in cols) if (c.enabled && c.isTrigger) _triggers++;
        }

        private void OnGUI()
        {
            string text = $"[Physics2D]\n" +
                          $"Rigidbodies:  {_rigidbodies}\n" +
                          $"Colliders:    {_colliders}  (Triggers: {_triggers})\n" +
                          $"FixedDelta:   {Time.fixedDeltaTime * 1000f:0.##} ms\n" +
                          $"Sim Mode:     {Physics2D.simulationMode}";

            var content = new GUIContent(text);
            // ★ 변경: _root.Style -> _root.GetStyle()
            var style = _root ? _root.GetStyle() : GUI.skin.box;
            Vector2 pad = _root ? _root.padding : new Vector2(8,8);
            Vector2 size = style.CalcSize(content);
            var r = new Rect(pad.x, Screen.height - size.y - pad.y, size.x + 10, size.y + 10);
            if (_root) _root.DrawBox(r, content); else GUI.Box(r, content, style);
        }
    }
}
#endif