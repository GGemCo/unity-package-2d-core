using System.Text;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Physics2D 객체 수와 시뮬레이션 설정을 수집하는 HUD Provider 입니다.
    /// </summary>
    [DebugHudProvider(300)]
    public sealed class Physics2DHud : IDebugHudProvider
    {
        private readonly StringBuilder _builder = new(160);

        private int _rigidbodies;
        private int _colliders;
        private int _triggers;

        public bool IsEnabled(GGemCoSettings settings)
        {
            return settings != null && settings.EnableDebugHud && settings.enablePhysics2DHud;
        }

        public float GetUpdateInterval(GGemCoSettings settings)
        {
            return settings != null ? Mathf.Max(0.1f, settings.debugHudPhysics2DUpdateInterval) : 0.5f;
        }

        public void Reset()
        {
            _rigidbodies = 0;
            _colliders = 0;
            _triggers = 0;
        }

        public void Tick(float elapsedSeconds)
        {
            Rigidbody2D[] rigidbodies = Object.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
            Collider2D[] colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

            _rigidbodies = rigidbodies.Length;
            _colliders = colliders.Length;
            _triggers = 0;

            foreach (Collider2D collider in colliders)
            {
                if (collider.enabled && collider.isTrigger)
                {
                    _triggers++;
                }
            }
        }

        public bool TryBuildContent(StringBuilder builder)
        {
            _builder.Clear();
            _builder.AppendLine("[Physics2D]");
            _builder.Append("Rigidbodies:  ").AppendLine(_rigidbodies.ToString());
            _builder.Append("Colliders:    ").Append(_colliders).Append("  (Triggers: ").Append(_triggers).AppendLine(")");
            _builder.Append("FixedDelta:   ").Append((Time.fixedDeltaTime * 1000f).ToString("0.##")).AppendLine(" ms");
            _builder.Append("Sim Mode:     ").Append(Physics2D.simulationMode);

            if (_builder.Length <= 0)
            {
                return false;
            }

            builder.Append(_builder);
            return true;
        }
    }
}
