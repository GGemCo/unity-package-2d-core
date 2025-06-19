using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class SettingTags
    {
        private readonly string title = "태그 추가하기";
        
        public void OnGUI()
        {
            Common.OnGUITitle(title);

            if (GUILayout.Button(title))
            {
                AddTags();
            }
        }

        private void AddTags()
        {
            // Tag 추가를 위해 Unity의 TagManager를 가져옴
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // 원하는 태그 목록
            foreach (var tags in ConfigTags.GetValues())
            {
                string tag = tags.Value;
                if (!TagExists(tagsProp, tag))
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                }
            }

            // 변경 사항 저장
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets(); // 변경 사항 저장
            
            // Inspector 갱신
            EditorUtility.SetDirty(tagManager.targetObject); // TargetObject를 '더럽힘' 상태로 만들어 갱신 유도
            AssetDatabase.Refresh(); // 에디터 갱신
            EditorUtility.DisplayDialog(title, "태그 추가 완료", "OK");
        }

        private bool TagExists(SerializedProperty tagsProp, string tag)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag))
                {
                    return true;
                }
            }
            return false;
        }
    }
}