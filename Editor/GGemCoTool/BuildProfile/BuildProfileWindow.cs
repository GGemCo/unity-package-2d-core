using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터에서 개발 모드와 릴리즈 유사 테스트 모드를 전환하는 Build Profile 관리 창입니다.
    /// </summary>
    public class BuildProfileWindow : EditorWindow
    {
        private const string WindowTitle = "Build 프로파일";
        private Vector2 _scrollPosition;
        private string _lastValidationMessage;
        private MessageType _lastValidationMessageType = MessageType.Info;

        /// <summary>
        /// Build Profile 관리 창을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolBuildProfile, false, (int)ConfigEditor.ToolOrdering.BuildProfile)]
        public static void Open()
        {
            BuildProfileWindow window = GetWindow<BuildProfileWindow>(WindowTitle);
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        /// <summary>
        /// Build Profile 상태와 전환 버튼을 그립니다.
        /// </summary>
        private void OnGUI()
        {
            using (EditorGUILayout.ScrollViewScope scope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scope.scrollPosition;
                DrawHeader();
                DrawCurrentStatus();
                DrawModeButtons();
                DrawValidationButtons();
                DrawPlayModeButton();
                DrawValidationMessage();
            }
        }

        /// <summary>
        /// 창 상단 안내 문구를 그립니다.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField("Build 프로파일", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Development는 작업자별 Development Settings와 디버그 기능을 허용합니다. " +
                "Release Simulation은 에디터 Play Mode에서 서비스용 Settings를 사용하고 디버그 기능을 차단합니다. " +
                "Release는 실제 배포 빌드 기준으로 Unity Development Build 옵션도 비활성화합니다.",
                MessageType.Info);
        }

        /// <summary>
        /// 현재 선택된 빌드 모드와 연동된 Settings/Unity 빌드 옵션 상태를 표시합니다.
        /// </summary>
        private static void DrawCurrentStatus()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Build Mode", BuildProfileEditorPrefs.CurrentMode);
                EditorGUILayout.EnumPopup("Settings Profile", SettingsProfileEditorPrefs.CurrentProfile);
                EditorGUILayout.Toggle("Allow Debug Features", GGemCoBuildFlags.AllowDebugFeatures);
                EditorGUILayout.Toggle("Unity Development Build", EditorUserBuildSettings.development);
            }
        }

        /// <summary>
        /// 각 빌드 모드로 전환하는 버튼을 그립니다.
        /// </summary>
        private void DrawModeButtons()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("모드 전환", EditorStyles.boldLabel);

            if (GUILayout.Button("Development 모드 적용", GUILayout.Height(28f)))
            {
                ApplyMode(GGemCoBuildMode.Development);
            }

            if (GUILayout.Button("Release Simulation 모드 적용", GUILayout.Height(28f)))
            {
                ApplyMode(GGemCoBuildMode.ReleaseSimulation);
            }

            if (GUILayout.Button("Release 모드 적용", GUILayout.Height(28f)))
            {
                ApplyMode(GGemCoBuildMode.Release);
            }
        }

        /// <summary>
        /// 릴리즈 안전 검증과 디버그 옵션 정리 버튼을 그립니다.
        /// </summary>
        private void DrawValidationButtons()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("릴리즈 안전 검증", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("릴리즈 검증 실행", GUILayout.Height(26f)))
                {
                    ValidateReleaseSafety();
                }

                if (GUILayout.Button("릴리즈 후보 디버그 옵션 끄기", GUILayout.Height(26f)))
                {
                    DebugOptionMenu.DisableReleaseBuildDebugOptions();
                    ValidateReleaseSafety();
                }
            }
        }

        /// <summary>
        /// 현재 모드로 Play Mode를 시작하는 버튼을 그립니다.
        /// </summary>
        private void DrawPlayModeButton()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Play Mode", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("현재 모드로 Play 실행", GUILayout.Height(28f)))
                {
                    EditorApplication.isPlaying = true;
                }
            }
        }

        /// <summary>
        /// 마지막 릴리즈 검증 결과 메시지를 표시합니다.
        /// </summary>
        private void DrawValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(_lastValidationMessage))
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(_lastValidationMessage, _lastValidationMessageType);
        }

        /// <summary>
        /// 지정한 빌드 모드를 적용하고 창을 다시 그립니다.
        /// </summary>
        /// <param name="mode">적용할 빌드 모드입니다.</param>
        private void ApplyMode(GGemCoBuildMode mode)
        {
            BuildProfileApplier.Apply(mode);
            _lastValidationMessage = $"{mode} 모드를 적용했습니다.";
            _lastValidationMessageType = MessageType.Info;
            Debug.Log($"[GGemCo] Build Profile 적용: mode={mode}, settingsProfile={SettingsProfileEditorPrefs.CurrentProfile}, developmentBuild={EditorUserBuildSettings.development}");
            Repaint();
        }

        /// <summary>
        /// 릴리즈 빌드 후보 안전 검증을 실행하고 결과를 표시합니다.
        /// </summary>
        private void ValidateReleaseSafety()
        {
            bool passed = ReleaseDebugOptionBuildValidator.TryValidateReleaseBuild(out string message);
            _lastValidationMessage = message;
            _lastValidationMessageType = passed ? MessageType.Info : MessageType.Error;

            if (passed)
            {
                Debug.Log($"[GGemCo] {message}");
                EditorUtility.DisplayDialog("GGemCo Release Validation", message, "확인");
                return;
            }

            Debug.LogError($"[GGemCo] {message}");
            EditorUtility.DisplayDialog("GGemCo Release Validation", message, "확인");
        }
    }
}
