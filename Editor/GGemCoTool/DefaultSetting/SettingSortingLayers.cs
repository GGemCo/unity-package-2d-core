using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingSortingLayers
    {
        private const string Title = "Sorting Layer 추가하기";
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        public void OnGUI()
        {
            HelperEditorUI.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                AddSortingLayers();
            }
        }

        public void AddSortingLayers(EditorSetupContext ctx = null)
        {
            // Sorting Layer 추가를 위해 TagManager 가져오기
            var tagManagerObj = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(TagManagerPath)[0]);
            var sortingLayersProp = tagManagerObj.FindProperty("m_SortingLayers");
            if (sortingLayersProp == null)
            {
                HelperLog.Warn("m_SortingLayers 속성을 찾지 못했습니다. Unity 버전/프로젝트 상태를 확인하세요.", ctx);
                return;
            }
            
            // 현재 uniqueID 최댓값 계산 (기존 코드와 동일한 전략) 
            // ref: SettingSortingLayers.cs(GetHighestSortingLayerID)
            int highestId = 0;
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var idProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID");
                highestId = Mathf.Max(highestId, idProp.intValue);
            }

            int added = 0, skipped = 0;

            foreach (var kv in ConfigSortingLayer.GetValues()) // ref: SettingSortingLayers.cs
            {
                string layerName = kv.Value;
                if (string.IsNullOrWhiteSpace(layerName))
                    continue;

                // 중복 검사 (ref: SortingLayerExists)
                bool exists = false;
                for (int i = 0; i < sortingLayersProp.arraySize; i++)
                {
                    var elem = sortingLayersProp.GetArrayElementAtIndex(i);
                    if (elem.FindPropertyRelative("name").stringValue == layerName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    skipped++;
                    continue;
                }

                // 새 항목 추가 + uniqueID 부여
                sortingLayersProp.InsertArrayElementAtIndex(sortingLayersProp.arraySize);
                var newProp = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
                newProp.FindPropertyRelative("name").stringValue = layerName;
                newProp.FindPropertyRelative("uniqueID").intValue = ++highestId;
                added++;
            }

            // 저장/리프레시 (원본도 Apply/Save/Refresh 수행) 
            // ref: SettingSortingLayers.cs
            tagManagerObj.ApplyModifiedProperties();
            if (ctx == null)
            {
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(tagManagerObj.targetObject);
                AssetDatabase.Refresh();
            }

            if (ctx != null)
            {
                HelperLog.Info($"[SortingLayers] 추가: {added}, 스킵: {skipped}", ctx);
            }
            else
            {
                EditorUtility.DisplayDialog(Title, "Sorting Layer 추가 완료", "OK");
            }
        }

        private bool SortingLayerExists(SerializedProperty sortingLayersProp, string layer)
        {
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                if (sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue.Equals(layer))
                {
                    return true;
                }
            }
            return false;
        }

        private int GetHighestSortingLayerID(SerializedProperty sortingLayersProp)
        {
            int highestID = 0;

            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                int id = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").intValue;
                if (id > highestID)
                {
                    highestID = id;
                }
            }

            return highestID;
        }
    }
}
