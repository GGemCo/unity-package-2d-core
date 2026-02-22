#if UNITY_EDITOR
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// ProjectileBase에 생성된 CapsuleCollider2D(= TableProjectile.ColliderSize 반영 결과)를
    /// SceneView에서 Gizmo로 시각화합니다.
    ///
    /// - TableProjectile.ColliderSize 값은 ProjectileBase.Initialize()에서 CapsuleCollider2D.size로 적용됩니다.
    /// - 따라서 '테이블 값이 실제로 어떻게 적용되었는지'를 가장 정확하게 확인하려면,
    ///   테이블을 다시 읽어 그리는 것보다 실제 Collider2D를 그리는 방식이 안정적입니다.
    /// </summary>
    internal static class ProjectileColliderGizmoDrawer
    {
        // Selected: Hierarchy에서 선택한 경우
        // Active:   SceneView에서 활성 상태
        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.NonSelected)]
        private static void Draw(ProjectileBase projectile, GizmoType gizmoType)
        {
            if (projectile == null)
                return;

            // ProjectileBase는 Initialize()에서 CapsuleCollider2D를 추가합니다.
            var col = projectile.GetComponent<CapsuleCollider2D>();
            if (col == null)
                return;

            // SceneView에서 Wire를 그리기 위해 Handles 사용
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = Color.green;

            Transform t = col.transform;

            // Collider2D는 로컬 값이므로 lossyScale을 반영하여 월드 기준으로 시각화
            Vector2 lossy = t.lossyScale;
            Vector2 size = new Vector2(Mathf.Abs(lossy.x) * col.size.x, Mathf.Abs(lossy.y) * col.size.y);
            Vector2 offset = new Vector2(lossy.x * col.offset.x, lossy.y * col.offset.y);

            Vector3 center = t.position + (Vector3)offset;

            DrawCapsule2DWire(center, size, col.direction);
        }

        private static void DrawCapsule2DWire(Vector3 center, Vector2 size, CapsuleDirection2D direction)
        {
            if (size.x <= 0f || size.y <= 0f)
            {
                Handles.DrawWireCube(center, size);
                return;
            }

            if (direction == CapsuleDirection2D.Horizontal)
            {
                float radius = size.y * 0.5f;
                float halfLine = Mathf.Max(0f, size.x - size.y) * 0.5f;

                Vector3 left = center + Vector3.left * halfLine;
                Vector3 right = center + Vector3.right * halfLine;

                // 양 끝 원호(원) + 상/하 직선
                Handles.DrawWireDisc(left, Vector3.forward, radius);
                Handles.DrawWireDisc(right, Vector3.forward, radius);

                Handles.DrawLine(left + Vector3.up * radius, right + Vector3.up * radius);
                Handles.DrawLine(left + Vector3.down * radius, right + Vector3.down * radius);
            }
            else // Vertical
            {
                float radius = size.x * 0.5f;
                float halfLine = Mathf.Max(0f, size.y - size.x) * 0.5f;

                Vector3 bottom = center + Vector3.down * halfLine;
                Vector3 top = center + Vector3.up * halfLine;

                Handles.DrawWireDisc(bottom, Vector3.forward, radius);
                Handles.DrawWireDisc(top, Vector3.forward, radius);

                Handles.DrawLine(bottom + Vector3.left * radius, top + Vector3.left * radius);
                Handles.DrawLine(bottom + Vector3.right * radius, top + Vector3.right * radius);
            }
        }
    }
}
#endif
