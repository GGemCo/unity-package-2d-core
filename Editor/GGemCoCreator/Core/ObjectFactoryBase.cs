#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 생성 처리 공통 베이스
    /// - 부모 정렬(SetParentAndAlign)
    /// - Undo 등록
    /// - Selection 포커스
    /// - Prefab 템플릿 우선 + 코드 폴백
    /// </summary>
    internal abstract class ObjectFactoryBase
    {
        public static GameObject NewRoot(string name, MenuCommand cmd, int layer = -1)
        {
            var go = new GameObject(name);
            if (layer >= 0) go.layer = layer;

            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            Selection.activeObject = go;
            return go;
        }

        public static T Add<T>(GameObject go) where T : Component
        {
            var c = Undo.AddComponent<T>(go);
            return c;
        }

        /// <summary>패키지 상대 경로로 Prefab 로드 후 인스턴스화. 실패 시 null</summary>
        public static GameObject TryInstantiatePrefab(string packageRelativePath, MenuCommand cmd)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(packageRelativePath);
            if (!prefab)
            { 
                Debug.LogError($"프리팹이 없습니다. 경로: {packageRelativePath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            GameObjectUtility.SetParentAndAlign(instance, cmd.context as GameObject);
            Undo.RegisterCreatedObjectUndo(instance, $"Create {instance.name}");
            Selection.activeObject = instance;
            return instance;
        }

        /// <summary>BoxCollider2D 기본값 도우미</summary>
        public static BoxCollider2D EnsureTriggerBox(GameObject go, bool isTrigger = true)
        {
            var col = go.GetComponent<BoxCollider2D>() ?? Undo.AddComponent<BoxCollider2D>(go);
            col.isTrigger = isTrigger;
            return col;
        }
    }
}
#endif