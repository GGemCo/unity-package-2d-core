#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// GGemCo 프로젝트 기본 셋업을 수행하는 에디터 윈도우.
    /// - 레이어/태그/정렬 레이어 셋업
    /// - 기본 씬 및 설정용 ScriptableObject 생성
    /// - 기본 데이터 테이블/로컬라이제이션/Addressables 셋업
    /// - 옵션에 따라 한글 폰트, 샘플 UI, 샘플 데이터까지 셋업
    /// </summary>
    public sealed class AutoProjectSetupWindow : EditorWindow
    {
        private const string Title = "GGemCo Project Setup";

        [Tooltip("네이버 나눔고딕 한글 폰트를 프로젝트에 셋업합니다.")]
        private bool _setKoreanFont;

        [Tooltip("예제 RPG 프로젝트에 맞는 샘플 데이터/리소스를 모두 셋업합니다.")]
        private bool _setAllSampleData;

        /// <summary>
        /// 실제로 실행될 SetupStep 들의 파이프라인.
        /// 매 실행마다 빌드되며, EditorWindow 인스턴스와 분리된 순수 데이터 구조로 유지합니다.
        /// </summary>
        private readonly List<SetupStepBase> _setupSteps = new List<SetupStepBase>();

        #region Menu

        [MenuItem(ConfigEditor.NameToolSettingAuto, false, (int)ConfigEditor.ToolOrdering.AutoSetting)]
        public static void Open()
        {
            var window = GetWindow<AutoProjectSetupWindow>(Title);
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        #endregion

        #region Unity Callbacks

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(4);

            DrawOptions();
            EditorGUILayout.Space(10);

            DrawButtons();
        }

        #endregion

        #region GUI

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("프로젝트에 필요한 필수 초기 구성을 자동으로 셋업합니다.");
        }

        private void DrawOptions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("옵션 선택", EditorStyles.boldLabel);

                // 나눔고딕 폰트 셋업
                _setKoreanFont = HelperEditorUI.ToggleLeft(
                    "한글 폰트(나눔 고딕) 셋팅",
                    _setKoreanFont,
                    "네이버 나눔고딕 폰트를 프로젝트에 셋업합니다."
                );

                // 샘플 데이터/리소스 셋업
                _setAllSampleData = HelperEditorUI.ToggleLeft(
                    "샘플 RPG 셋팅",
                    _setAllSampleData,
                    "샘플 RPG 프로젝트에 맞는 데이터 테이블 및 리소스가 복사/셋업됩니다."
                );
            }
        }

        private void DrawButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("유효성 검사"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        Run(validateOnly: true);
                    };
                    GUIUtility.ExitGUI(); // 현재 OnGUI 루프 안전 종료
                }

                if (GUILayout.Button("자동 셋팅 시작"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        Run(validateOnly: false);
                    };
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("로그파일 폴더 열기"))
                {
                    OpenGameDataFolder();
                }
            }
        }
        #endregion

        #region Pipeline Build

        /// <summary>
        /// 현재 옵션 상태를 기반으로 SetupStep 파이프라인을 구성합니다.
        /// </summary>
        private void BuildStepPipeline()
        {
            _setupSteps.Clear();

            // 1. 공통 필수 스텝
            _setupSteps.Add(new StepAddLayers());
            _setupSteps.Add(new StepAddSortingLayers());
            _setupSteps.Add(new StepAddTags());

            _setupSteps.Add(new StepCreateDefaultScenes());
            _setupSteps.Add(new StepCreateSettingScriptableObject());

            _setupSteps.Add(new StepSetSceneRequireObject());
            _setupSteps.Add(new StepCopyEmptyDataTable());
            _setupSteps.Add(new StepCopyDefaultLocalization());

            // 2. 옵션: 폰트 셋업(단일 스텝으로만 추가 - 중복 방지)
            bool needKoreanFontStep = _setKoreanFont || _setAllSampleData;
            if (needKoreanFontStep)
            {
                _setupSteps.Add(new StepCopyKoreanFonts());
            }

            // 3. 옵션: 샘플 RPG 리소스/데이터 셋업
            bool needSampleResources = _setAllSampleData;
            if (needSampleResources)
            {
                _setupSteps.Add(new StepCopyPackageResources());
                _setupSteps.Add(new StepCopyAllSampleData());
                _setupSteps.Add(new StepInstantiateUIWindowsFromTable());
                _setupSteps.Add(new StepSetSettingScriptableObject());
                _setupSteps.Add(new StepSetCamera());
            }
            // Addressables를 마지막에 등록
            _setupSteps.Add(new StepSetDefaultAddressableData());
            if (needSampleResources)
            {
                _setupSteps.Add(new StepSetAddressableData());
            }
        }

        #endregion

        #region Run & Validate

        private void Run(bool validateOnly)
        {
            // 파이프라인 구성
            BuildStepPipeline();

            var steps = _setupSteps
                .Where(s => s is { enabledStep: true })
                .OrderBy(s => s.order)
                .ToArray();

            if (steps.Length == 0)
            {
                EditorUtility.DisplayDialog(Title, "활성화된 스텝이 없습니다.", "OK");
                return;
            }

            int progressId = Progress.Start("GGemCo Project Setup", "Initializing...");

            using var logger = new EditorSetupLogger();
            var ctx = new EditorSetupContext(logger);
            logger.Info($"Steps: {steps.Length}");

            try
            {
                // 1) Validate Phase
                int stepCount = steps.Length;
                for (int i = 0; i < stepCount; i++)
                {
                    var step = steps[i];
                    float pct = (float)i / stepCount;
                    Progress.Report(progressId, pct, $"Validate: {step}");

                    if (!step.Validate(ctx, out var msg))
                    {
                        // Validate 실패는 경고로만 남기고 계속 진행 (설정 상황에 따라 허용)
                        logger.Warn($"[Validate] {step} :: {msg}");
                    }
                }

                if (validateOnly)
                {
                    logger.Info("[Result] Validate Only 완료");
                    EditorUtility.DisplayDialog(Title, "Validate Only 완료(자세한 내용은 로그 참조)", "OK");
                    return;
                }

                // 2) Execute Phase
                for (int i = 0; i < stepCount; i++)
                {
                    var step = steps[i];
                    float pct = (float)(i + 1) / stepCount;
                    Progress.Report(progressId, pct, $"Run: {step}");

                    try
                    {
                        logger.Info($"[Run] {step}");
                        step.Execute(ctx);
                        logger.Info($"[OK ] {step}");
                    }
                    catch (Exception ex)
                    {
                        // 개별 스텝 실패는 로그에 남기고 다음 스텝 계속 수행
                        logger.Error($"[FAIL] {step} :: {ex}");
                    }
                }

                // 3) 열린 씬 저장
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                logger.Info("[Save] Open Scenes saved.");

                logger.Info($"[Done] Log: {logger.LogPath}");
                EditorUtility.DisplayDialog(Title, $"완료\nLog: {logger.LogPath}", "OK");
            }
            finally
            {
                Progress.Remove(progressId);
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion

        private void OpenGameDataFolder()
        {
            string path = ConfigProjectSetup.DirLog;

            if (Directory.Exists(path))
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true
                });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                Process.Start("open", path);
#endif
            }
            else
            {
                UnityEngine.Debug.LogError($"폴더를 찾을 수 없습니다: {path}");
            }
        }

    }
}
#endif
