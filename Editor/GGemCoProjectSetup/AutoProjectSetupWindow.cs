#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public class AutoProjectSetupWindow : EditorWindow
    {
        /// <summary>
        /// 기본 윈도우 타이틀입니다. 파생 클래스에서 재정의하여 제품/프로젝트 별 타이틀을 지정합니다.
        /// </summary>
        protected virtual string Title => "GGemCo Project Setup";
        /// <summary>
        /// 파생 클래스에서 옵션 변경 가능 여부를 제어할 수 있습니다.
        /// 일반적으로 실행 중에는 false가 되어야 합니다.
        /// </summary>
        protected virtual bool CanChangeOptions => _isRunning == false;
        private bool _isRunning;
        
        /// <summary>
        /// 네이버 나눔고딕 한글 폰트를 프로젝트에 셋업할지 여부입니다.
        /// </summary>
        private bool _setKoreanFont;

        /// <summary>
        /// 샘플 RPG 프로젝트에 맞는 샘플 데이터/리소스를 모두 셋업할지 여부입니다.
        /// </summary>
        protected bool setAllSampleData;

        /// <summary>
        /// 실행할 SetupStep 파이프라인입니다.
        /// 매 실행 시 옵션 상태를 바탕으로 다시 구성되며, EditorWindow 인스턴스와 분리된 순수 데이터 목록으로 유지합니다.
        /// </summary>
        protected readonly List<SetupStepBase> setupSteps = new List<SetupStepBase>();

        #region Menu

        /// <summary>
        /// 프로젝트 셋업 윈도우를 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolSettingAuto, false, (int)ConfigEditor.ToolOrdering.AutoSetting)]
        public static void Open()
        {
            var window = GetWindow<AutoProjectSetupWindow>();
            window.titleContent = new GUIContent(window.Title);
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
                EditorGUILayout.Space(4);

                using (new EditorGUI.DisabledScope(!CanChangeOptions))
                {
                    // 기본 옵션 영역(파생에서 커스터마이즈 가능)
                    DrawOptionsArea();
                }
            }
        }

        /// <summary>
        /// 옵션 영역(UI)을 그립니다. 파생 클래스에서 재정의하여 옵션 구성을 변경/추가합니다.
        /// </summary>
        protected virtual void DrawOptionsArea()
        {
            // 나눔고딕 폰트 셋업
            _setKoreanFont = HelperEditorUI.ToggleLeft(
                "한글 폰트(나눔 고딕) 셋팅",
                _setKoreanFont,
                "네이버 나눔고딕 폰트를 프로젝트에 셋업합니다."
            );

            // 샘플 데이터/리소스 셋업
            setAllSampleData = HelperEditorUI.ToggleLeft(
                "샘플 RPG 셋팅",
                setAllSampleData,
                "샘플 RPG 프로젝트에 맞는 데이터 테이블 및 리소스가 복사/셋업됩니다."
            );
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
        protected virtual void BuildStepPipeline()
        {
            bool needKoreanFontStep = _setKoreanFont || setAllSampleData;
            bool needSampleResources = setAllSampleData;
            setupSteps.Clear();

            // AssetDatabase.StartAssetEditing();
            
            // 1) 공통 필수 스텝
            setupSteps.Add(new StepAddLayers());
            setupSteps.Add(new StepAddSortingLayers());
            setupSteps.Add(new StepAddTags());

            setupSteps.Add(new StepCreateDefaultScenes());
            setupSteps.Add(new StepCreateSettingScriptableObject());

            if (!needSampleResources)
            {
                setupSteps.Add(new StepCopyEmptyDataTable());
            }

            // 순서 중요: Localization은 옵션 윈도우 프리팹이 복사될 때 사용된다.
            setupSteps.Add(new StepCopyDefaultLocalization());

            // 순서 중요: StepSetSceneRequireObject 에서 Popup Default 프리팹을 사용한다.
            setupSteps.Add(new StepCopyPackageResources());

            // 순서 중요: 필수 UI 윈도우 복사하기. 옵션 윈도우 프리팹을 Intro 씬에서 사용한다.
            setupSteps.Add(new StepCopyDefaultUIWindowPrefab());

            // DataAddressable 폴더에서 디폴트로 복사해야하는 리소스
            setupSteps.Add(new StepCopyDefaultDataAddressable());

            // 2) 옵션: 폰트 셋업(단일 스텝으로만 추가 - 중복 방지)
            if (needKoreanFontStep)
            {
                setupSteps.Add(new StepCopyKoreanFonts());
            }

            // 3) 옵션: 샘플 RPG 리소스/데이터 셋업
            if (needSampleResources)
            {
                setupSteps.Add(new StepCopyAllSampleData());
            }
            
            // 씬 셋업 하기
            setupSteps.Add(new StepSetSceneRequireObject(needSampleResources));
            
            // Addressables는 마지막에 등록
            setupSteps.Add(new StepSetDefaultAddressableData());
            if (needSampleResources)
            {
                setupSteps.Add(new StepSetAddressableData());
                setupSteps.Add(new StepSetCamera());
                setupSteps.Add(new StepInstantiateUIWindowsFromTable());
                setupSteps.Add(new StepSetSettingScriptableObject());
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

            if (setupSteps.Count <= 0)
            {
                EditorUtility.DisplayDialog(Title, "활성화된 스텝이 없습니다.", "OK");
                return;
            }

            _isRunning = true;

            int progressId = Progress.Start("GGemCo Project Setup", "Initializing...");

            using var logger = new EditorSetupLogger();
            var addressableEditor = ScriptableObject.CreateInstance<AddressableEditor>();
            var ctx = new EditorSetupContext(logger, addressableEditor);
            logger.Info($"Steps: {setupSteps.Count}");

            try
            {
                // 1) Validate Phase
                int stepCount = setupSteps.Count;
                for (int i = 0; i < stepCount; i++)
                {
                    var step = setupSteps[i];
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
                    var step = setupSteps[i];
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

                EditorApplication.delayCall += () =>
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    EditorApplication.RepaintProjectWindow();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                };
                
                logger.Info($"[Done] Log: {logger.LogPath}");
                EditorUtility.DisplayDialog(Title, $"완료\nLog: {logger.LogPath}", "OK");
            }
            finally
            {
                Progress.Remove(progressId);
                EditorUtility.ClearProgressBar();
                _isRunning = false;
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
