#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class EffectFactory
    {
        public static GameObject CreateDefault(MenuCommand cmd)
        {
            var root = ObjectFactoryBase.NewRoot("Effect_Default", cmd);
            // 예시: 파티클/라인/스프라이트 등 시각 요소는 팀 표준에 맞게 추가
            // var ps = ObjectFactoryBase.Add<ParticleSystem>(root);
            return root;
        }
    }
}
#endif