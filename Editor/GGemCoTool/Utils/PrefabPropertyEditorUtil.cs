#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Prefab Asset에 포함된 특정 컴포넌트의 Serialized Property 값을 수정하는 공용 유틸리티
    /// </summary>
    public static class PrefabPropertyEditorUtil
    {
        /// <summary>
        /// 지정한 이름의 Prefab Asset을 찾아, 특정 컴포넌트의 프로퍼티 값을 변경한다.
        /// </summary>
        /// <typeparam name="TComponent"> 수정 대상 컴포넌트 타입 </typeparam>
        /// <typeparam name="TValue"> 프로퍼티 값 타입 (int, float, bool, string, Color, enum 등) </typeparam>
        /// <param name="prefabName"> Project 탭에 존재하는 Prefab 이름 </param>
        /// <param name="propertyName"> SerializedProperty 이름 </param>
        /// <param name="value"> 변경할 값 </param>
        /// <param name="refresh"> AssetDatabase.SaveAssets, AssetDatabase.Refresh </param>
        public static void SetPrefabPropertyValue<TComponent, TValue>(
            string prefabName,
            string propertyName,
            TValue value,
            bool refresh = true)
            where TComponent : Component
        {
            if (string.IsNullOrEmpty(prefabName))
                throw new ArgumentException("Prefab name is null or empty.");

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name is null or empty.");

            string[] guids = AssetDatabase.FindAssets(
                $"{prefabName} t:Prefab");

            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning(
                    $"Prefab not found: {prefabName}");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    var component =
                        root.GetComponentInChildren<TComponent>(true);

                    if (component == null)
                    {
                        Debug.LogWarning(
                            $"Component {typeof(TComponent).Name} not found in prefab: {path}");
                        continue;
                    }

                    var so = new SerializedObject(component);
                    var prop = so.FindProperty(propertyName);

                    if (prop == null)
                    {
                        Debug.LogWarning(
                            $"Property '{propertyName}' not found in {typeof(TComponent).Name}: {path}");
                        continue;
                    }

                    if (!TrySetSerializedPropertyValue(prop, value))
                    {
                        Debug.LogWarning(
                            $"Unsupported property type: {prop.propertyType} ({path})");
                        continue;
                    }

                    so.ApplyModifiedProperties();
                    PrefabUtility.SaveAsPrefabAsset(root, path);

                    Debug.Log(
                        $"Prefab updated: {prefabName} | {propertyName} = {value}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            if (!refresh) return;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// SerializedProperty 타입에 따라 값을 안전하게 설정한다.
        /// </summary>
        private static bool TrySetSerializedPropertyValue<TValue>(
            SerializedProperty prop,
            TValue value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = Convert.ToInt32(value);
                    return true;

                case SerializedPropertyType.Boolean:
                    prop.boolValue = Convert.ToBoolean(value);
                    return true;

                case SerializedPropertyType.Float:
                    prop.floatValue = Convert.ToSingle(value);
                    return true;

                case SerializedPropertyType.String:
                    prop.stringValue = value?.ToString();
                    return true;

                case SerializedPropertyType.Color:
                    if (value is Color color)
                    {
                        prop.colorValue = color;
                        return true;
                    }
                    return false;

                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = Convert.ToInt32(value);
                    return true;

                case SerializedPropertyType.ObjectReference:
                    if (value is UnityEngine.Object obj)
                    {
                        prop.objectReferenceValue = obj;
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}
#endif
