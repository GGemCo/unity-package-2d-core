#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Physics2D HUD 데이터를 계산하는 런타임 프로바이더입니다.
    /// </summary>
    public sealed class Physics2DHud : IDebugHudProvider
    {
        private float _remainingTime;
        private int _rigidbodies;
        private int _colliders;
        private int _triggers;
        private bool _initialized;
        private string _text = "[Physics2D]\nCollecting...";

        public DebugHudAnchor Anchor => DebugHudAnchor.BottomLeft;

        public bool IsEnabled(GGemCoSettings settings)
        {
            return settings != null && DebugOptionRuntimeUtility.Resolve(settings.enableDebugHud) && DebugOptionRuntimeUtility.Resolve(settings.enablePhysics2DHud);
        }

        public void Initialize(GGemCoSettings settings)
        {
            _remainingTime = Mathf.Max(0.1f, settings != null ? settings.debugHudPhysics2DUpdateInterval : 0.5f);
            _text = "[Physics2D]\nCollecting...";
            _initialized = true;
        }

        public void Tick(float unscaledDeltaTime, GGemCoSettings settings)
        {
            if (!_initialized)
            {
                Initialize(settings);
            }

            _remainingTime -= Mathf.Max(0f, unscaledDeltaTime);
            if (_remainingTime > 0f)
            {
                return;
            }

            Rigidbody2D[] rigidbodies = Object.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
            Collider2D[] colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

            _rigidbodies = rigidbodies.Length;
            _colliders = colliders.Length;
            _triggers = 0;
            foreach (Collider2D collider in colliders)
            {
                if (collider != null && collider.enabled && collider.isTrigger)
                {
                    _triggers++;
                }
            }

            _remainingTime = Mathf.Max(0.1f, settings != null ? settings.debugHudPhysics2DUpdateInterval : 0.5f);
            _text = $"[Physics2D]\n" +
                    $"Rigidbodies:  {_rigidbodies}\n" +
                    $"Colliders:    {_colliders}  (Triggers: {_triggers})\n" +
                    $"FixedDelta:   {Time.fixedDeltaTime * 1000f:0.##} ms\n" +
                    $"Sim Mode:     {Physics2D.simulationMode}";
        }

        public string GetText() => _text;
    }
}
#endif
