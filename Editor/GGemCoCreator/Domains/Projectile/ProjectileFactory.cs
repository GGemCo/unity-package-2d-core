#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class ProjectileFactory
    {
        public static GameObject CreateDefault(MenuCommand cmd)
        {
            var root = ObjectFactoryBase.NewRoot("Projectile_Default", cmd);
            // 기본 구성: 루트 + 히트 트리거 + (선택) 이동 테스트용 컴포넌트
            var col = ObjectFactoryBase.EnsureTriggerBox(root);
            col.size = new Vector2(0.2f, 0.2f);
            // TODO: ProjectileBase 컴포넌트 부착, 레이어/태그 설정 등
            return root;
        }
    }
}
#endif