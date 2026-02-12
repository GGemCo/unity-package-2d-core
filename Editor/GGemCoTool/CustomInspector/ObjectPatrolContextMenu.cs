#if UNITY_EDITOR
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public static class ObjectPatrolContextMenu
    {
        [MenuItem("CONTEXT/Monster/" + ConfigDefine.NameSDK + "/Patrol 생성하기")]
        private static void CreatePatrol(MenuCommand command)
        {
            var monster = command.context as Monster;
            if (!monster)
            {
                GcLogger.LogError($"연결할 몬스터가 없습니다.");
                return;
            }

            if (monster.patrolObject)
            {
                GcLogger.LogWarning("이미 연결된 패트롤이 있습니다.");
                Selection.activeObject = monster.patrolObject;
                return;
            }

            var defaultMap = monster.GetComponentInParent<DefaultMap>();
            if (!defaultMap)
            {
                Debug.LogError("상위에 DefaultMap이 없어 Patrol을 생성할 수 없습니다.");
                return;
            }

            // 기본 PatrolData(원하시면 기본 크기/오프셋을 Settings로 빼는 것을 추천)
            var pos = monster.transform.position;
            var patrolData = new PatrolData(
                position: pos,
                rotation: Vector3.zero,
                boxColliderSize: new Vector2(10f, 10f),
                boxColliderOffset: Vector2.zero
            );

            var patrol = PatrolEditorFactory.CreateOrLinkPatrol(defaultMap, monster, patrolData);
            if (patrol)
            {
                Selection.activeObject = patrol.gameObject;
                monster.SetPatrolObject(patrol.gameObject);
            }
        }
    }
}
#endif