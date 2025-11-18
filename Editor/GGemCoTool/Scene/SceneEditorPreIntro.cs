using GGemCo2DCore;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 인트로 씬 설정 툴
    /// </summary>
    public class SceneEditorPreIntro : DefaultSceneEditor
    {
        private const string Title = "Pre 인트로 씬 셋팅하기";
        
        [MenuItem(ConfigEditor.NameToolSettingScenePreIntro, false, (int)ConfigEditor.ToolOrdering.SettingScenePreIntro)]
        public static void ShowWindow()
        {
            GetWindow<SceneEditorPreIntro>(Title);
        }

        private void OnGUI()
        {
            if (!CheckCurrentLoadedScene(ConfigDefine.SceneNamePreIntro))
            {
                EditorGUILayout.HelpBox($"Pre 인트로 씬을 불러와 주세요.", MessageType.Error);
            }
            else
            {
                DrawRequiredSection();
            }
        }
        private void DrawRequiredSection()
        {
            HelperEditorUI.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"* Pre 인트로 씬 오브젝트\n* 로딩 진행률을 보여주는 텍스트", MessageType.Info);
            if (GUILayout.Button("필수 항목 셋팅하기"))
            {
                SetupRequiredObjects();
            }
        }
        /// <summary>
        /// 필수 항목 셋팅
        /// </summary>
        public void SetupRequiredObjects()
        {
            GGemCo2DCore.ScenePreIntro scene = CreateOrAddComponent<ScenePreIntro>(nameof(ScenePreIntro));

            CreateUIComponent.CreateObjectCanvas(packageType);

            // 진행률 텍스트 만들고 연결하기
            TextMeshProUGUI textMeshProUGUI = CreateLoadingText();
            scene.SetTextLoadingPercent(textMeshProUGUI);
            
            EditorUtility.SetDirty(scene);
        }
    }
}