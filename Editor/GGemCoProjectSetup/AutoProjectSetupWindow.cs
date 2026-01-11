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
    /// GGemCo 프로젝트의 기본 초기 구성을 자동으로 수행하는 에디터 윈도우입니다.
    /// 레이어/태그/정렬 레이어, 기본 씬/Settings 에셋, 데이터/로컬라이제이션/Addressables 등을 파이프라인(SetupStep)으로 실행합니다.
    /// </summary>
    /// <remarks>
    /// 실행 방식:
    /// - 옵션(한글 폰트/샘플 RPG)에 따라 파이프라인 스텝 구성이 달라집니다.
    /// - Validate 단계는 실패해도 경고만 남기고 계속 진행합니다(프로젝트 상태에 따라 일부 누락을 허용).
    /// - Execute 단계는 개별 스텝 예외를 로그로 남기고 다음 스텝을 계속 수행합니다.
    /// </remarks>
    public sealed class AutoProjectSetupWindow : EditorWindow
    {
        private const string Title = "GGemCo Project Setup";

        /// <summary>
        /// 네이버 나눔고딕 한글 폰트를 프로젝트에 셋업할지 여부입니다.
        /// </summary>
        private bool _setKoreanFont;

        /// <summary>
        /// 샘플 RPG 프로젝트에 맞는 샘플 데이터/리소스를 모두 셋업할지 여부입니다.
        /// </summary>
        private bool _setAllSampleData;

        /// <summary>
        /// 실행할 SetupStep 파이프라인입니다.
        /// 매 실행 시 옵션 상태를 바탕으로 다시 구성되며, EditorWindow 인스턴스와 분리된 순수 데이터 목록으로 유지합니다.
        /// </summary>
        private readonly List<SetupStepBase> _setupSteps = new List<SetupStepBase>();

        #region Menu

        /// <summary>
        /// 프로젝트 셋업 윈도우를 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolSettingAuto, false, (int)ConfigEditor.ToolOrdering.AutoSetting)]
        public static void Open()
        {
            var window = GetWindow<AutoProjectSetupWindow>(Title);
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// 에디터 윈도우 UI를 그립니다.
        /// </summary>
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

        /// <summary>
        /// 상단 안내 문구를 표시합니다.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("프로젝트에 필요한 필수 초기 구성을 자동으로 셋업합니다.");
        }

        /// <summary>
        /// 셋업 옵션(한글 폰트/샘플 RPG)을 표시하고 값을 갱신합니다.
        /// </summary>
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

        /// <summary>
        /// 실행 버튼(유효성 검사/자동 셋팅/로그 폴더 열기)을 표시합니다.
        /// 에디터 GUI 루프 안정성을 위해 delayCall로 실제 실행을 지연합니다.
        /// </summary>
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
        /// <remarks>
        /// 구성 규칙:
        /// - 공통 필수 스텝은 항상 포함합니다.
        /// - 한글 폰트 스텝은 (한글 폰트 옵션) 또는 (샘플 RPG 옵션)일 때만 1회 추가합니다.
        /// - Addressables 등록 스텝은 가능한 마지막에 실행되도록 배치합니다.
        /// </remarks>
        private void BuildStepPipeline()
        {
            _setupSteps.Clear();

            // 1) 공통 필수 스텝
            _setupSteps.Add(new StepAddLayers());
            _setupSteps.Add(new StepAddSortingLayers());
            _setupSteps.Add(new StepAddTags());

            _setupSteps.Add(new StepCreateDefaultScenes());
            _setupSteps.Add(new StepCreateSettingScriptableObject());

            // 순서 중요: StepSetSceneRequireObject 에서 Popup Default 프리팹을 사용한다.
            _setupSteps.Add(new StepCopyPackageResources());

            // 순서 중요: 필수 UI 윈도우 복사하기. 옵션 윈도우 프리팹을 Intro 씬에서 사용한다.
            _setupSteps.Add(new StepCopyDefaultUIWindowPrefab());

            _setupSteps.Add(new StepSetSceneRequireObject());
            _setupSteps.Add(new StepCopyEmptyDataTable());
            _setupSteps.Add(new StepCopyDefaultLocalization());

            // DataAddressable 폴더에서 디폴트로 복사해야하는 리소스
            _setupSteps.Add(new StepCopyDefaultDataAddressable());

            // 2) 옵션: 폰트 셋업(단일 스텝으로만 추가 - 중복 방지)
            bool needKoreanFontStep = _setKoreanFont || _setAllSampleData;
            if (needKoreanFontStep)
            {
                _setupSteps.Add(new StepCopyKoreanFonts());
            }

            // 3) 옵션: 샘플 RPG 리소스/데이터 셋업
            bool needSampleResources = _setAllSampleData;
            if (needSampleResources)
            {
                _setupSteps.Add(new StepCopyAllSampleData());
                _setupSteps.Add(new StepInstantiateUIWindowsFromTable());
                _setupSteps.Add(new StepSetSettingScriptableObject());
                _setupSteps.Add(new StepSetCamera());
            }

            // Addressables는 마지막에 등록
            _setupSteps.Add(new StepSetDefaultAddressableData());
            if (needSampleResources)
            {
                _setupSteps.Add(new StepSetAddressableData());
            }
        }

        #endregion

        #region Run & Validate

        /// <summary>
        /// 구성된 파이프라인을 실행하거나(Execute), 유효성 검사만 수행합니다(Validate).
        /// </summary>
        /// <param name="validateOnly">true면 Validate 단계만 수행하고 종료합니다.</param>
        private void Run(bool validateOnly)
        {
            // 파이프라인 구성
            BuildStepPipeline();

            // enabledStep이 true인 스텝만 대상으로 하며, order 오름차순으로 실행합니다.
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

        /// <summary>
        /// 프로젝트 셋업 로그가 저장되는 폴더를 OS 파일 탐색기로 엽니다.
        /// </summary>
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
