using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingTags
    {
        private const string Title = "태그 추가하기";

        public void OnGUI()
        {
            HelperEditorUI.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                AddTags();
            }
        }

        public void AddTags(EditorSetupContext ctx = null)
        {
            // Tag 추가를 위해 Unity의 TagManager를 가져옴
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            if (tagsProp == null)
            {
                HelperLog.Error("TagManager의 'tags' 속성을 찾지 못했습니다.", ctx);    
                return;
            }
            
            int added = 0, skipped = 0;
            // 원하는 태그 목록
            foreach (var kv in ConfigTags.GetValues())
            {
                var tag = kv.Value;
                if (string.IsNullOrWhiteSpace(tag)) continue;

                bool exists = Enumerable.Range(0, tagsProp.arraySize)
                    .Any(i => tagsProp.GetArrayElementAtIndex(i).stringValue == tag);
                if (exists) { skipped++; continue; }

                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                added++;
            }

            // 변경 사항 저장
            tagManager.ApplyModifiedProperties();
            if (ctx == null)
            {
                AssetDatabase.SaveAssets(); // 변경 사항 저장
            
                // Inspector 갱신
                EditorUtility.SetDirty(tagManager.targetObject); // TargetObject를 '더럽힘' 상태로 만들어 갱신 유도
                AssetDatabase.Refresh(); // 에디터 갱신
            }
            
            if (ctx != null)
            {
                HelperLog.Info($"[Tags] 추가: {added}, 스킵: {skipped}", ctx);
            }
            else
            {
                EditorUtility.DisplayDialog(Title, "태그 추가 완료", "OK");
            }
        }
    }
}