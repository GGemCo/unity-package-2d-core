using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingLayers
    {
        private const string Title = "Layer 추가하기";
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        public void OnGUI()
        {
            HelperEditorUI.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                AddLayers();
            }
        }

        public void AddLayers(EditorSetupContext ctx = null)
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(TagManagerPath)[0]);
            var layersProp = so.FindProperty("layers");
            if (layersProp == null)
            {
                HelperLog.Error("TagManager의 'layers' 속성을 찾지 못했습니다.", ctx);
                return;
            }

            int added = 0, skipped = 0, noSlot = 0;

            foreach (var kv in ConfigLayer.GetValues())
            {
                var name = kv.Value;
                if (string.IsNullOrWhiteSpace(name)) continue;

                bool exists = Enumerable.Range(0, layersProp.arraySize)
                    .Any(i => layersProp.GetArrayElementAtIndex(i).stringValue == name);
                if (exists) { skipped++; continue; }

                // 사용자 슬롯(8~31)에서 빈 슬롯 찾기
                int idx = Enumerable.Range(8, 24)
                    .FirstOrDefault(i => string.IsNullOrEmpty(layersProp.GetArrayElementAtIndex(i).stringValue));

                if (idx == 0) { noSlot++; continue; } // 빈 슬롯 없음

                layersProp.GetArrayElementAtIndex(idx).stringValue = name;
                added++;
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(so.targetObject);
            AssetDatabase.Refresh();
            
            if (noSlot > 0)
                HelperLog.Warn($"[Layers] 빈 슬롯 부족으로 {noSlot}개를 추가하지 못했습니다. (사용자 슬롯 8~31 범위)", ctx);

            if (ctx != null)
            {
                HelperLog.Info($"[Layers] 추가: {added}, 스킵: {skipped}", ctx);
            }
            else
            {
                EditorUtility.DisplayDialog(Title, "Layer 추가 완료", "OK");
            }
        }
    }
}
