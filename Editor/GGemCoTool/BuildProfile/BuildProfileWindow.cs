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
                DrawCheatToolsOptions();
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
                EditorGUILayout.LabelField("Active Build Target", BuildProfileScriptingDefineUtility.GetActiveBuildTargetGroupName());
                EditorGUILayout.Toggle("Cheat Tools Symbol", BuildProfileScriptingDefineUtility.HasCheatToolsSymbolInActiveTarget());
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
        /// 치트 도구 컴파일 심볼 관리 UI를 그립니다.
        /// Development 모드에서만 사용자가 활성화할 수 있고, 릴리즈 계열 모드에서는 자동 제거됩니다.
        /// </summary>
        private void DrawCheatToolsOptions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Cheat Tools", EditorStyles.boldLabel);

            bool isDevelopmentMode = BuildProfileEditorPrefs.CurrentMode == GGemCoBuildMode.Development;
            bool requestedEnabled = BuildProfileEditorPrefs.CheatToolsEnabled;
            bool actualEnabled = BuildProfileScriptingDefineUtility.HasCheatToolsSymbolInActiveTarget();

            EditorGUILayout.HelpBox(
                $"{GGemCoScriptingDefineSymbols.EnableCheatTools} 심볼은 골드 추가, 레벨업, 데이터 초기화 같은 치트/QA 도구 코드를 컴파일에 포함할 때만 사용합니다. " +
                "Release Simulation과 Release 모드에서는 자동으로 제거되며, 릴리즈 검증에서도 금지 심볼로 검사됩니다.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!isDevelopmentMode))
            {
                bool nextValue = EditorGUILayout.ToggleLeft(
                    $"{GGemCoScriptingDefineSymbols.EnableCheatTools} 컴파일 포함",
                    isDevelopmentMode && requestedEnabled);

                if (isDevelopmentMode && nextValue != requestedEnabled)
                {
                    ApplyCheatToolsEnabled(nextValue);
                }
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("현재 타겟 심볼 등록 상태", actualEnabled);
            }

            if (!isDevelopmentMode && actualEnabled)
            {
                EditorGUILayout.HelpBox(
                    "현재 모드는 릴리즈 계열이지만 활성 빌드 타겟에 치트 도구 심볼이 남아 있습니다. 모드 적용 버튼을 다시 누르거나 아래 제거 버튼을 실행해주세요.",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!actualEnabled))
            {
                if (GUILayout.Button("Cheat Tools 심볼 제거", GUILayout.Height(24f)))
                {
                    ApplyCheatToolsEnabled(false);
                }
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
        /// 치트 도구 컴파일 심볼 사용 여부를 적용하고 안내 메시지를 갱신합니다.
        /// </summary>
        /// <param name="enabled">치트 도구 심볼을 활성화하려면 true입니다.</param>
        private void ApplyCheatToolsEnabled(bool enabled)
        {
            BuildProfileApplier.SetCheatToolsEnabled(enabled);
            bool actualEnabled = BuildProfileScriptingDefineUtility.HasCheatToolsSymbolInActiveTarget();
            _lastValidationMessage = actualEnabled
                ? $"{GGemCoScriptingDefineSymbols.EnableCheatTools} 심볼을 현재 빌드 타겟에 추가했습니다. 스크립트 재컴파일 후 치트 도구 코드가 활성화됩니다."
                : $"{GGemCoScriptingDefineSymbols.EnableCheatTools} 심볼을 현재 빌드 타겟에서 제거했습니다.";
            _lastValidationMessageType = actualEnabled ? MessageType.Warning : MessageType.Info;
            Debug.Log($"[GGemCo] Cheat Tools 심볼 상태 변경: requested={enabled}, actual={actualEnabled}, target={BuildProfileScriptingDefineUtility.GetActiveBuildTargetGroupName()}");
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
