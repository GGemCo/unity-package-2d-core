using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class SettingLayers
    {
        private readonly string title = "Layer 추가하기";

        // 추가할 Layer 목록

        public void OnGUI()
        {
            Common.OnGUITitle(title);

            if (GUILayout.Button(title))
            {
                AddLayers();
            }
        }

        private void AddLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            bool addedAnyLayer = false;

            foreach (var layerNames in ConfigLayer.GetValues())
            {
                string layerName = layerNames.Value;
                if (!LayerExists(layersProp, layerName))
                {
                    int emptySlot = FindEmptyLayerSlot(layersProp);
                    if (emptySlot != -1)
                    {
                        layersProp.GetArrayElementAtIndex(emptySlot).stringValue = layerName;
                        addedAnyLayer = true;
                    }
                    else
                    {
                        Debug.LogWarning($"Layer '{layerName}'를 추가할 빈 슬롯이 없습니다.");
                    }
                }
                else
                {
                    Debug.Log($"Layer '{layerName}'는 이미 존재합니다.");
                }
            }

            if (addedAnyLayer)
            {
                // 변경 사항 저장
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(tagManager.targetObject);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(title, "Layer 추가 완료", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(title, "추가된 Layer가 없습니다.", "OK");
            }
        }

        private bool LayerExists(SerializedProperty layersProp, string layerName)
        {
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return true;
                }
            }
            return false;
        }

        private int FindEmptyLayerSlot(SerializedProperty layersProp)
        {
            for (int i = 8; i < 32; i++) // User Layers (8~31)
            {
                SerializedProperty layerProperty = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProperty.stringValue)) // 빈 슬롯 찾기
                {
                    return i;
                }
            }
            return -1; // 빈 슬롯 없음
        }
    }
}
