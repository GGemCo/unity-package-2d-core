using System.Reflection;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Editor.Scene
{
    /// <summary>
    /// 인트로 씬 설정 툴
    /// </summary>
    public class SceneIntroEditor : DefaultEditorWindow
    {
        private const string Title = "인트로 씬 셋팅하기";
        
        [MenuItem(ConfigEditor.NameToolSettingSceneIntro, false, (int)ConfigEditor.ToolOrdering.SettingSceneIntro)]
        public static void ShowWindow()
        {
            GetWindow<SceneIntroEditor>(Title);
        }

        protected override void OnEnable()
        {
        }

        private void OnGUI()
        {
            DrawRequiredSection();
            Common.GUILine();
            DrawOptionalSection();
        }
        private void DrawRequiredSection()
        {
            Common.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"{ConfigDefine.NameSDK}.Scripts.SceneIntro 오브젝트와 게임 시작 버튼을 설정합니다.", MessageType.Info);

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
            // GGemCo.Scripts.SceneIntro GameObject 만들기
            GGemCo.Scripts.SceneIntro sceneIntro = CreateGameObjectSceneIntro();
            
            // 새 게임 버튼 만들고 연결하기
            FieldInfo fieldInfo = typeof(GGemCo.Scripts.SceneIntro)
                .GetField("buttonNewGame", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                Debug.LogError("buttonNewGame 필드를 찾을 수 없습니다.");
                return;
            }
            Button button = fieldInfo.GetValue(sceneIntro) as Button;
            
            if (!button)
            {
                // 버튼 생성
                Button createdButton = CreateUIComponent.CreateObjectButton(fieldInfo, "New Game");
                Undo.RecordObject(sceneIntro, "Assign Button");
                // 필드에 버튼 연결
                fieldInfo.SetValue(sceneIntro, createdButton);
                EditorUtility.SetDirty(sceneIntro);
                Debug.Log($"{fieldInfo.Name} 버튼이 생성되어 SceneIntro에 연결되었습니다.");
            }
            else
            {
                Debug.Log($"{fieldInfo.Name} 버튼이 이미 설정되어 있습니다.");
            }
        }
        /// <summary>
        /// GGemCo.Scripts.SceneIntro 오브젝트 만들기
        /// </summary>
        private GGemCo.Scripts.SceneIntro CreateGameObjectSceneIntro()
        {
            return CreateOrAddComponent<GGemCo.Scripts.SceneIntro>("SceneIntro");
        }
        
        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
            Common.OnGUITitle("옵션 항목");
            EditorGUILayout.HelpBox("불러오기 UI 관련 오브젝트를 셋팅합니다.", MessageType.Info);

            if (GUILayout.Button("불러오기 UI 셋팅하기"))
            {
                SetupLoadUIObjects();
            }
        }
        /// <summary>
        /// 불러오기 UI 셋팅하기
        /// </summary>
        private void SetupLoadUIObjects()
        {
            // 불러오기 UI 에서 팝업을 사용하기때문에, PopupManager 셋팅
            CreateGameObjectPopupManager();
        }
        private void CreateGameObjectPopupManager()
        {
            PopupManager popupManager = CreateOrAddComponent<PopupManager>("PopupManager");
        }
    }
}