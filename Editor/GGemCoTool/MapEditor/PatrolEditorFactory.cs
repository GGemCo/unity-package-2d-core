#if UNITY_EDITOR
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class PatrolEditorFactory
    {
        internal static ObjectPatrol CreateOrLinkPatrol(DefaultMap defaultMap, Monster monster, PatrolData patrolData)
        {
            if (!defaultMap || !monster || patrolData == null) return null;

            // 1) 프리팹 로드
            GameObject patrolPrefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(ConfigAddressableMap.ObjectPatrol.Path);
            if (!patrolPrefab)
            {
                Debug.LogError("패트롤 프리팹이 없습니다.");
                return null;
            }

            // 2) 생성(Undo 포함)
            var patrolGo = Object.Instantiate(patrolPrefab, defaultMap.transform);
            if (!patrolGo) return null;

            Undo.RegisterCreatedObjectUndo(patrolGo, "Create Patrol");

            // 3) 초기화/연결
            var patrol = patrolGo.GetComponent<ObjectPatrol>();
            if (!patrol) return null;

            Undo.RecordObject(patrol, "Init Patrol");
            patrol.PatrolData = patrolData;
            patrol.InitializeByMapEditor();
            patrol.SetParentMonster(monster.gameObject);

            // Monster에 연결
            Undo.RecordObject(monster, "Link Patrol");
            monster.SetPatrolObject(patrolGo);

            EditorUtility.SetDirty(patrol);
            EditorUtility.SetDirty(monster);
            EditorUtility.SetDirty(defaultMap);

            return patrol;
        }
    }
}
#endif