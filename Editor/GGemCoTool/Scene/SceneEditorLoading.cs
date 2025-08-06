using System.Reflection;
using GGemCo2DCore;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 로딩 씬 설정 툴
    /// </summary>
    public class SceneEditorLoading : DefaultSceneEditor
    {
        private const string Title = "로딩 씬 셋팅하기";
        
        [MenuItem(ConfigEditor.NameToolSettingSceneLoading, false, (int)ConfigEditor.ToolOrdering.SettingSceneLoading)]
        public static void ShowWindow()
        {
            GetWindow<SceneEditorLoading>(Title);
        }

        private void OnGUI()
        {
            if (!CheckCurrentLoadedScene(ConfigDefine.SceneNameLoading))
            {
                EditorGUILayout.HelpBox($"로딩 씬을 불러와 주세요.", MessageType.Error);
            }
            else
            {
                DrawRequiredSection();
                Common.GUILine();
                DrawOptionalSection();
            }
        }
        private void DrawRequiredSection()
        {
            Common.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"* 로딩 씬 오브젝트\n* 로딩 진행률을 보여주는 텍스트", MessageType.Info);

            if (GUILayout.Button("필수 항목 셋팅하기"))
            {
                SetupRequiredObjects();
            }
        }
        /// <summary>
        /// 필수 항목 셋팅
        /// </summary>
        private void SetupRequiredObjects()
        {
            // GGemCo2DCore.SceneLoading GameObject 만들기
            GGemCo2DCore.SceneLoading scene = CreateOrAddComponent<GGemCo2DCore.SceneLoading>("SceneLoading");
            
            // 진행률 텍스트 만들고 연결하기
            TextMeshProUGUI textMeshProUGUI = CreateLoadingText();
            scene.SetTextLoadingPercent(textMeshProUGUI);
            
            EditorUtility.SetDirty(scene);
        }
        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
        }
    }
}