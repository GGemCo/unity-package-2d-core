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
            window.minSize = new Vector2(560f, 460f);
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
                "반복 테스트 시간을 줄이기 위해 모드 전환만으로 Scripting Define Symbol은 변경하지 않습니다.",
                MessageType.Info);
        }

        /// <summary>
        /// 현재 선택된 빌드 모드와 연동된 Settings/Unity 빌드 옵션 상태를 표시합니다.
        /// </summary>
        private static void DrawCurrentStatus()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);

            bool isAndroidTarget =
                EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Android;
            bool hasCalculatedVersionCode = false;
            int calculatedVersionCode = 0;
            string versionCodeError = string.Empty;
            string versionCodeStatus = string.Empty;
            if (isAndroidTarget)
            {
                hasCalculatedVersionCode =
                    BuildProfileVersionCodeUtility.TryCalculateAndroidBundleVersionCode(
                        PlayerSettings.bundleVersion,
                        out calculatedVersionCode,
                        out versionCodeError);
                if (hasCalculatedVersionCode)
                {
                    int currentVersionCode = PlayerSettings.Android.bundleVersionCode;
                    versionCodeStatus = calculatedVersionCode == currentVersionCode
                        ? "동기화됨"
                        : calculatedVersionCode > currentVersionCode
                            ? "업데이트 필요"
                            : "현재 코드보다 계산값이 낮음";
                }
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Build Mode", BuildProfileEditorPrefs.CurrentMode);
                EditorGUILayout.EnumPopup("Settings Profile", SettingsProfileEditorPrefs.CurrentProfile);
                EditorGUILayout.Toggle("Allow Debug Features", GGemCoBuildFlags.AllowDebugFeatures);
                EditorGUILayout.Toggle("Unity Development Build", EditorUserBuildSettings.development);
                EditorGUILayout.LabelField("Active Build Target", BuildProfileScriptingDefineUtility.GetActiveBuildTargetGroupName());
                EditorGUILayout.Toggle("Cheat Tools Compile Symbol", BuildProfileScriptingDefineUtility.HasCheatToolsSymbolInActiveTarget());
                EditorGUILayout.TextField("Player Version", PlayerSettings.bundleVersion);

                if (isAndroidTarget)
                {
                    EditorGUILayout.IntField(
                        "Android Bundle Version Code",
                        PlayerSettings.Android.bundleVersionCode);
                    EditorGUILayout.TextField(
                        "Calculated Android Code",
                        hasCalculatedVersionCode
                            ? calculatedVersionCode.ToString()
                            : "계산 불가");
                    EditorGUILayout.TextField(
                        "Version Code Status",
                        hasCalculatedVersionCode
                            ? versionCodeStatus
                            : "검증 실패");
                }
            }

            if (isAndroidTarget && !hasCalculatedVersionCode)
            {
                EditorGUILayout.HelpBox(versionCodeError, MessageType.Error);
            }
        }

        /// <summary>
        /// 각 빌드 모드로 전환하는 버튼을 그립니다.
        /// 모드 전환은 Settings 프로파일과 Unity Development Build 옵션만 바꾸고, 치트 도구 심볼은 변경하지 않습니다.
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
        /// 심볼 변경은 재컴파일을 유발할 수 있으므로 모드 전환과 분리하고, 사용자가 명시적으로 변경할 때만 적용합니다.
        /// </summary>
        private void DrawCheatToolsOptions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Cheat Tools", EditorStyles.boldLabel);

            GGemCoBuildMode currentMode = BuildProfileEditorPrefs.CurrentMode;
            bool actualEnabled = BuildProfileScriptingDefineUtility.HasCheatToolsSymbolInActiveTarget();

            EditorGUILayout.HelpBox(
                $"{GGemCoScriptingDefineSymbols.EnableCheatTools} 심볼은 치트/QA 도구 코드의 컴파일 포함 여부만 결정합니다. " +
                "Development와 Release Simulation을 반복 전환할 때는 심볼을 유지하고, " +
                "실제 실행/표시는 GGemCoBuildFlags.AllowDebugFeatures와 GGemCoCheatToolGate에서 차단하세요. " +
                "실제 Release 빌드 전에는 아래 Release 빌드 준비 버튼으로 심볼을 제거합니다.",
                MessageType.Info);

            bool nextValue = EditorGUILayout.ToggleLeft(
                $"{GGemCoScriptingDefineSymbols.EnableCheatTools} 컴파일 포함",
                actualEnabled);

            if (nextValue != actualEnabled)
            {
                ApplyCheatToolsEnabled(nextValue);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("현재 타겟 심볼 등록 상태", actualEnabled);
            }

            if (actualEnabled && currentMode == GGemCoBuildMode.ReleaseSimulation)
            {
                EditorGUILayout.HelpBox(
                    "Release Simulation에서는 치트 심볼을 유지할 수 있습니다. 단, 치트 UI와 치트 실행 코드는 GGemCoCheatToolGate.CanUseCheatTools를 통과해야 합니다.",
                    MessageType.Info);
            }
            else if (actualEnabled && currentMode == GGemCoBuildMode.Release)
            {
                EditorGUILayout.HelpBox(
                    "Release 모드에서 실제 빌드를 만들기 전에는 치트 도구 심볼을 제거해야 합니다. 아래 Release 빌드 준비 실행을 사용해주세요.",
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
        /// 릴리즈 빌드 준비, 릴리즈 안전 검증, 디버그 옵션 정리 버튼을 그립니다.
        /// </summary>
        private void DrawValidationButtons()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("릴리즈 안전 검증", EditorStyles.boldLabel);

            if (GUILayout.Button("Release 빌드 준비 실행", GUILayout.Height(28f)))
            {
                PrepareReleaseBuild();
            }

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
            _lastValidationMessage = CreateApplyModeMessage(mode);
            _lastValidationMessageType = MessageType.Info;
            Debug.Log($"[GGemCo] Build Profile 적용: mode={mode}, settingsProfile={SettingsProfileEditorPrefs.CurrentProfile}, developmentBuild={EditorUserBuildSettings.development}, cheatSymbol={BuildProfileScriptingDefineUtility.HasCheatToolsSymbolInActiveTarget()}");
            Repaint();
        }

        /// <summary>
        /// 모드 적용 후 사용자에게 표시할 안내 메시지를 생성합니다.
        /// Release Simulation에서는 심볼을 제거하지 않는 정책을 명확히 안내합니다.
        /// </summary>
        /// <param name="mode">적용한 빌드 모드입니다.</param>
        /// <returns>모드 적용 결과 안내 메시지입니다.</returns>
        private static string CreateApplyModeMessage(GGemCoBuildMode mode)
        {
            if (mode == GGemCoBuildMode.ReleaseSimulation)
            {
                return "Release Simulation 모드를 적용했습니다. 반복 테스트 시간을 줄이기 위해 Cheat Tools 심볼은 변경하지 않았으며, 치트 실행은 AllowDebugFeatures=false로 차단됩니다.";
            }

            if (mode == GGemCoBuildMode.Release)
            {
                return "Release 모드를 적용했습니다. 실제 빌드 전에는 Release 빌드 준비 실행으로 Cheat Tools 심볼과 릴리즈 후보 디버그 옵션을 정리해주세요.";
            }

            return $"{mode} 모드를 적용했습니다.";
        }

        /// <summary>
        /// 치트 도구 컴파일 심볼 사용 여부를 적용하고 안내 메시지를 갱신합니다.
        /// 이 함수가 호출되면 Unity 스크립트 재컴파일이 발생할 수 있습니다.
        /// </summary>
        /// <param name="enabled">치트 도구 심볼을 활성화하려면 true입니다.</param>
        private void ApplyCheatToolsEnabled(bool enabled)
        {
            bool changed = BuildProfileApplier.SetCheatToolsEnabled(enabled);
            bool actualEnabled = BuildProfileScriptingDefineUtility.HasCheatToolsSymbolInActiveTarget();
            _lastValidationMessage = actualEnabled
                ? $"{GGemCoScriptingDefineSymbols.EnableCheatTools} 심볼을 현재 빌드 타겟에 추가했습니다. 스크립트 재컴파일 후 치트 도구 코드가 활성화됩니다."
                : $"{GGemCoScriptingDefineSymbols.EnableCheatTools} 심볼을 현재 빌드 타겟에서 제거했습니다.";

            if (!changed)
            {
                _lastValidationMessage += " 이미 같은 상태였기 때문에 심볼 목록은 변경되지 않았습니다.";
            }

            _lastValidationMessageType = actualEnabled ? MessageType.Warning : MessageType.Info;
            Debug.Log($"[GGemCo] Cheat Tools 심볼 상태 변경: requested={enabled}, changed={changed}, actual={actualEnabled}, target={BuildProfileScriptingDefineUtility.GetActiveBuildTargetGroupName()}");
            Repaint();
        }

        /// <summary>
        /// 실제 Release 빌드 후보를 준비합니다.
        /// Android 활성 타겟에서는 Player Version으로 Bundle Version Code를 동기화한 뒤,
        /// Release 모드, 서비스 Settings, Unity Release 빌드 옵션을 적용하고 치트 심볼과 릴리즈 후보 디버그 옵션을 정리합니다.
        /// </summary>
        private void PrepareReleaseBuild()
        {
            if (!BuildProfileApplier.TryPrepareReleaseBuild(
                    out AndroidBundleVersionCodeSyncResult? versionCodeResult,
                    out string errorMessage))
            {
                _lastValidationMessage = errorMessage;
                _lastValidationMessageType = MessageType.Error;
                Debug.LogError($"[GGemCo] Release 빌드 준비 실패: {errorMessage}");
                EditorUtility.DisplayDialog(
                    "GGemCo Release Preparation",
                    errorMessage,
                    "확인");
                Repaint();
                return;
            }

            string versionCodeMessage = versionCodeResult.HasValue
                ? BuildProfileVersionCodeUtility.BuildSynchronizationMessage(versionCodeResult.Value)
                : "활성 빌드 타겟이 Android가 아니므로 Android Bundle Version Code 동기화를 건너뛰었습니다.";

            DebugOptionMenu.DisableReleaseBuildDebugOptions();
            ValidateReleaseSafety(versionCodeMessage);
            Debug.Log(
                $"[GGemCo] Release 빌드 준비 실행: mode={BuildProfileEditorPrefs.CurrentMode}, " +
                $"settingsProfile={SettingsProfileEditorPrefs.CurrentProfile}, " +
                $"developmentBuild={EditorUserBuildSettings.development}, " +
                $"cheatSymbol={BuildProfileScriptingDefineUtility.HasCheatToolsSymbolInActiveTarget()}, " +
                $"versionCodeResult={versionCodeMessage}");
            Repaint();
        }

        /// <summary>
        /// 릴리즈 빌드 후보 안전 검증을 실행하고 결과를 표시합니다.
        /// </summary>
        /// <param name="prefixMessage">검증 결과 앞에 추가할 준비 단계 안내입니다.</param>
        private void ValidateReleaseSafety(string prefixMessage = null)
        {
            bool passed =
                ReleaseDebugOptionBuildValidator.TryValidateReleaseBuild(out string validationMessage);
            string message = string.IsNullOrWhiteSpace(prefixMessage)
                ? validationMessage
                : prefixMessage + "\n\n" + validationMessage;

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
