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
    /// 본 윈도우는 "설치 마법사(Installer)" UX를 위해 다음을 지원합니다.
    /// - 단계(Validate/Execute)별 진행률 표시
    /// - 스텝 목록(대기/진행/완료/실패) 표시
    /// - 실시간 로그 스트리밍
    /// - 취소(스텝 단위 중단)
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
        protected virtual bool CanChangeOptions => _runner == null || !_runner.IsRunning;

        /// <summary>
        /// 설치 마법사 UI 페이지 구분입니다.
        /// </summary>
        private enum WizardPage
        {
            Options,
            Installing,
            Finish
        }

        /// <summary>
        /// 스텝 리스트의 UI 상태 표현입니다.
        /// </summary>
        private enum StepUiState
        {
            Pending,
            Validating,
            Executing,
            Succeeded,
            Failed,
            Skipped
        }

        /// <summary>
        /// 설치 스텝과 그 진행 상태/메시지를 UI에 표시하기 위한 항목입니다.
        /// </summary>
        [Serializable]
        private sealed class StepUiItem
        {
            /// <summary>
            /// 표시 대상 스텝 인스턴스입니다.
            /// </summary>
            public SetupStepBase Step;

            /// <summary>
            /// 현재 UI 상태입니다.
            /// </summary>
            public StepUiState State = StepUiState.Pending;

            /// <summary>
            /// 스텝 상태에 대한 보조 메시지(검증 결과/에러/스킵 사유 등)입니다.
            /// </summary>
            public string Message;
        }

        /// <summary>
        /// 네이버 나눔고딕 한글 폰트를 프로젝트에 셋업할지 여부입니다.
        /// </summary>
        private bool _setKoreanFont;

        /// <summary>
        /// 샘플 RPG 프로젝트에 맞는 샘플 데이터/리소스를 모두 셋업할지 여부입니다.
        /// </summary>
        protected bool _setAllSampleData;

        /// <summary>
        /// 실행할 SetupStep 파이프라인입니다.
        /// 매 실행 시 옵션 상태를 바탕으로 다시 구성됩니다.
        /// </summary>
        protected readonly List<SetupStepBase> _setupSteps = new List<SetupStepBase>();

        private WizardPage _page = WizardPage.Options;

        private SetupRunner _runner;
        private StepUiItem[] _uiSteps = Array.Empty<StepUiItem>();

        private Vector2 _scrollSteps;
        private Vector2 _scrollLogs;
        private readonly List<string> _logLines = new List<string>(256);
        private const int MaxLogLines = 400;

        private GUIStyle _styleBox;
        private GUIStyle _styleTitle;
        private GUIStyle _styleSmall;

        #region Menu

        /// <summary>
        /// 프로젝트 셋업 윈도우를 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolSettingAuto, false, (int)ConfigEditor.ToolOrdering.AutoSetting)]
        public static void Open()
        {
            var window = GetWindow<AutoProjectSetupWindow>();
            window.titleContent = new GUIContent(window.Title);
            window.minSize = new Vector2(720f, 520f);
            window.Show();
        }

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// 윈도우가 활성화될 때 초기 상태(페이지/스타일)를 준비합니다.
        /// </summary>
        private void OnEnable()
        {
            _page = WizardPage.Options;
            
            // 타이틀은 파생 클래스 재정의 값을 사용합니다.
            titleContent = new GUIContent(Title);
        }

        /// <summary>
        /// 윈도우가 비활성화될 때 실행 중인 Runner를 안전하게 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            StopRunner();
        }

        /// <summary>
        /// 에디터 윈도우 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            EnsureStyles();

            DrawTopBar();
            EditorGUILayout.Space(8);

            switch (_page)
            {
                case WizardPage.Options:
                    DrawOptionsPage();
                    break;
                case WizardPage.Installing:
                    DrawInstallingPage();
                    break;
                case WizardPage.Finish:
                    DrawFinishPage();
                    break;
            }
        }

        #endregion

        #region GUI

        /// <summary>
        /// GUI 스타일을 1회 초기화합니다(HelpBox/Title/Small).
        /// </summary>
        private void EnsureStyles()
        {
            if (_styleBox != null) return;

            _styleBox = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _styleTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            _styleSmall = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
        }

        /// <summary>
        /// 상단 타이틀/설명 영역을 그립니다.
        /// </summary>
        private void DrawTopBar()
        {
            using (new EditorGUILayout.VerticalScope(_styleBox))
            {
                EditorGUILayout.LabelField(Title, _styleTitle);

                string subtitle = _page switch
                {
                    WizardPage.Options => "설치 옵션을 선택한 뒤, 자동 셋팅을 시작합니다.",
                    WizardPage.Installing => "자동 셋팅이 진행 중입니다. (스텝 단위로 UI가 갱신됩니다.)",
                    WizardPage.Finish => "자동 셋팅 결과를 확인합니다.",
                    _ => string.Empty
                };

                EditorGUILayout.LabelField(subtitle, _styleSmall);
            }
        }

        /// <summary>
        /// 옵션 선택 페이지(UI)입니다.
        /// </summary>
        private void DrawOptionsPage()
        {
            using (new EditorGUILayout.VerticalScope(_styleBox))
            {
                EditorGUILayout.LabelField("옵션", _styleTitle);
                EditorGUILayout.Space(4);

                using (new EditorGUI.DisabledScope(!CanChangeOptions))
                {
                    // 기본 옵션 영역(파생에서 커스터마이즈 가능)
                    DrawOptionsArea();
                }
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.VerticalScope(_styleBox))
            {
                EditorGUILayout.LabelField("실행", _styleTitle);
                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("유효성 검사(Validate Only)", GUILayout.Height(28)))
                    {
                        StartRun(validateOnly: true);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("자동 셋팅 시작", GUILayout.Height(28)))
                    {
                        StartRun(validateOnly: false);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("로그파일 폴더 열기", GUILayout.Height(28)))
                    {
                        OpenGameDataFolder();
                    }
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("참고: 진행 중에는 옵션을 변경할 수 없습니다. 취소는 스텝 단위로 중단됩니다.", _styleSmall);
            }
        }

        /// <summary>
        /// 옵션 영역(UI)을 그립니다. 파생 클래스에서 재정의하여 옵션 구성을 변경/추가합니다.
        /// </summary>
        protected virtual void DrawOptionsArea()
        {
            _setKoreanFont = HelperEditorUI.ToggleLeft(
                "한글 폰트 셋팅",
                _setKoreanFont,
                "네이버 나눔고딕 폰트를 프로젝트에 셋업합니다."
            );

            _setAllSampleData = HelperEditorUI.ToggleLeft(
                "샘플 RPG 셋팅",
                _setAllSampleData,
                "샘플 RPG 프로젝트에 맞는 데이터 테이블 및 리소스가 복사/셋업됩니다."
            );
        }

        /// <summary>
        /// 설치 진행 페이지(UI)입니다. 진행률/스텝 리스트/로그와 제어 버튼을 표시합니다.
        /// </summary>
        private void DrawInstallingPage()
        {
            if (_runner == null)
            {
                EditorGUILayout.HelpBox("실행 컨텍스트가 없습니다. 옵션 페이지로 돌아갑니다.", MessageType.Warning);
                if (GUILayout.Button("돌아가기")) _page = WizardPage.Options;
                return;
            }

            DrawProgressArea();
            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStepListArea();
                GUILayout.Space(8);
                DrawLogArea();
            }

            EditorGUILayout.Space(8);
            DrawInstallingButtons();
        }

        /// <summary>
        /// 설치 완료/중단 결과 페이지(UI)입니다.
        /// </summary>
        private void DrawFinishPage()
        {
            using (new EditorGUILayout.VerticalScope(_styleBox))
            {
                EditorGUILayout.LabelField("결과", _styleTitle);
                EditorGUILayout.Space(6);

                if (_runner != null)
                {
                    string statusText = _runner.State switch
                    {
                        SetupRunner.RunState.Succeeded => "완료",
                        SetupRunner.RunState.Failed => "실패(일부 스텝 실패 포함)",
                        SetupRunner.RunState.Canceled => "취소됨(부분 적용 가능)",
                        _ => "완료"
                    };

                    EditorGUILayout.LabelField($"상태: {statusText}");
                    EditorGUILayout.LabelField($"로그: {_runner.LogPath}", _styleSmall);
                }
                else
                {
                    EditorGUILayout.LabelField("상태: (세션이 종료되었습니다)");
                }
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("로그파일 폴더 열기", GUILayout.Height(28)))
                {
                    OpenGameDataFolder();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("닫기", GUILayout.Height(28), GUILayout.Width(120)))
                {
                    Close();
                }
            }

            EditorGUILayout.Space(8);
            DrawStepResultSummary();
        }

        /// <summary>
        /// 전체 진행률(OverallProgress01)과 현재 단계 정보를 표시합니다.
        /// </summary>
        private void DrawProgressArea()
        {
            using (new EditorGUILayout.VerticalScope(_styleBox))
            {
                EditorGUILayout.LabelField("진행 상황", _styleTitle);
                EditorGUILayout.Space(4);

                float pct = Mathf.Clamp01(_runner.OverallProgress01);
                string phase = _runner.PhaseDisplay;
                string current = _runner.CurrentStepDisplay;

                var rect = GUILayoutUtility.GetRect(10, 22, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(rect, pct, $"{Mathf.RoundToInt(pct * 100f)}%  |  {phase}  |  {current}");

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(_runner.Description, _styleSmall);
            }
        }

        /// <summary>
        /// 스텝 목록 패널(좌측)을 그립니다.
        /// </summary>
        private void DrawStepListArea()
        {
            using (new EditorGUILayout.VerticalScope(_styleBox, GUILayout.Width(position.width * 0.45f)))
            {
                EditorGUILayout.LabelField("스텝", _styleTitle);
                EditorGUILayout.Space(4);

                _scrollSteps = EditorGUILayout.BeginScrollView(_scrollSteps);
                for (int i = 0; i < _uiSteps.Length; i++)
                {
                    var item = _uiSteps[i];
                    DrawStepRow(i, item);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 스텝 한 줄(아이콘/이름/메시지)을 그립니다.
        /// </summary>
        /// <param name="index">스텝의 표시 인덱스(0 기반)입니다.</param>
        /// <param name="item">표시할 스텝 UI 항목입니다.</param>
        private void DrawStepRow(int index, StepUiItem item)
        {
            string icon = item.State switch
            {
                StepUiState.Pending => "○",
                StepUiState.Validating => "▶",
                StepUiState.Executing => "▶",
                StepUiState.Succeeded => "✔",
                StepUiState.Failed => "✖",
                StepUiState.Skipped => "–",
                _ => "○"
            };

            string name = item.Step != null ? item.Step.DisplayName : "(null)";
            string msg = string.IsNullOrEmpty(item.Message) ? string.Empty : $"  -  {item.Message}";

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField($"{icon}  {index + 1:00}. {name}");
                if (!string.IsNullOrEmpty(msg))
                    EditorGUILayout.LabelField(msg, _styleSmall);
                EditorGUILayout.Space(2);
            }
        }

        /// <summary>
        /// 로그 패널(우측)을 그립니다.
        /// </summary>
        private void DrawLogArea()
        {
            using (new EditorGUILayout.VerticalScope(_styleBox, GUILayout.ExpandWidth(true)))
            {
                EditorGUILayout.LabelField("로그", _styleTitle);
                EditorGUILayout.Space(4);

                _scrollLogs = EditorGUILayout.BeginScrollView(_scrollLogs);

                for (int i = 0; i < _logLines.Count; i++)
                {
                    EditorGUILayout.SelectableLabel(_logLines[i], EditorStyles.miniLabel, GUILayout.Height(14));
                }

                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 설치 진행 중 하단 버튼(취소/로그 폴더)을 그립니다.
        /// </summary>
        private void DrawInstallingButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _runner != null && _runner.IsRunning;

                if (GUILayout.Button("취소(Stop)", GUILayout.Height(28), GUILayout.Width(140)))
                {
                    _runner?.RequestCancel();
                }

                GUI.enabled = true;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("로그파일 폴더 열기", GUILayout.Height(28), GUILayout.Width(160)))
                {
                    OpenGameDataFolder();
                }
            }
        }

        /// <summary>
        /// 완료 페이지에서 스텝 성공/실패/스킵 집계를 표시합니다.
        /// </summary>
        private void DrawStepResultSummary()
        {
            using (new EditorGUILayout.VerticalScope(_styleBox))
            {
                EditorGUILayout.LabelField("스텝 요약", _styleTitle);
                EditorGUILayout.Space(4);

                int ok = _uiSteps.Count(s => s.State == StepUiState.Succeeded);
                int fail = _uiSteps.Count(s => s.State == StepUiState.Failed);
                int skipped = _uiSteps.Count(s => s.State == StepUiState.Skipped);

                EditorGUILayout.LabelField($"성공: {ok} / 실패: {fail} / 스킵: {skipped} / 총: {_uiSteps.Length}");
            }
        }

        #endregion

        #region Pipeline Build

        /// <summary>
        /// 현재 옵션 상태를 기반으로 SetupStep 파이프라인을 구성합니다.
        /// </summary>
        /// <remarks>
        /// 스텝의 추가 순서는 일부 기능 의존(예: Localization → 프리팹/옵션 UI) 때문에 중요합니다.
        /// </remarks>
        protected virtual void BuildStepPipeline()
        {
            bool needKoreanFontStep = _setKoreanFont || _setAllSampleData;
            bool needSampleResources = _setAllSampleData;

            _setupSteps.Clear();
            AssetDatabase.StartAssetEditing();

            // 1) 공통 필수 스텝
            _setupSteps.Add(new StepAddLayers());
            _setupSteps.Add(new StepAddSortingLayers());
            _setupSteps.Add(new StepAddTags());

            _setupSteps.Add(new StepCreateDefaultScenes());
            _setupSteps.Add(new StepCreateSettingScriptableObject());

            // 순서 중요: Localization은 옵션 윈도우 프리팹이 복사될 때 사용된다.
            _setupSteps.Add(new StepCopyEmptyDataTable());
            _setupSteps.Add(new StepCopyDefaultLocalization());
            
            // 순서 중요: StepSetSceneRequireObject 에서 Popup Default 프리팹을 사용한다.
            _setupSteps.Add(new StepCopyPackageResources());

            // 순서 중요: 필수 UI 윈도우 복사하기. 옵션 윈도우 프리팹을 Intro 씬에서 사용한다.
            _setupSteps.Add(new StepCopyDefaultUIWindowPrefab());

            // DataAddressable 폴더에서 디폴트로 복사해야하는 리소스
            _setupSteps.Add(new StepCopyDefaultDataAddressable());
            
            // 한글 폰트는 옵션에 따라 추가
            if (needKoreanFontStep)
            {
                _setupSteps.Add(new StepCopyKoreanFonts());
            }

            // 2) 샘플 RPG 리소스/데이터
            if (needSampleResources)
            {
                _setupSteps.Add(new StepCopyAllSampleData());
                _setupSteps.Add(new StepInstantiateUIWindowsFromTable());
                _setupSteps.Add(new StepSetSettingScriptableObject());
                _setupSteps.Add(new StepSetCamera());
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            
            _setupSteps.Add(new StepSetSceneRequireObject(needSampleResources));

            // Addressables는 마지막에 등록
            _setupSteps.Add(new StepSetDefaultAddressableData());
            if (needSampleResources)
            {
                _setupSteps.Add(new StepSetAddressableData());
            }
        }

        #endregion

        #region Run (Installer Runner)

        /// <summary>
        /// 현재 옵션에 따라 스텝 파이프라인을 구성하고 Runner 실행을 시작합니다.
        /// </summary>
        /// <param name="validateOnly">true면 Validate 단계만 수행하고 Execute는 수행하지 않습니다.</param>
        /// <exception cref="Exception">
        /// Runner 생성/시작 과정에서 예외가 발생할 수 있습니다.
        /// (대부분은 내부 구현에서 처리되지만, 에디터 환경/파일 IO 이슈 등으로 발생 가능)
        /// </exception>
        private void StartRun(bool validateOnly)
        {
            if (_runner != null && _runner.IsRunning)
            {
                EditorUtility.DisplayDialog(Title, "이미 실행 중입니다.", "OK");
                return;
            }

            BuildStepPipeline();

            var steps = _setupSteps.Where(s => s != null && s.enabledStep).ToArray();
            if (steps.Length == 0)
            {
                EditorUtility.DisplayDialog(Title, "활성화된 스텝이 없습니다.", "OK");
                return;
            }

            _uiSteps = steps.Select(s => new StepUiItem { Step = s, State = StepUiState.Pending }).ToArray();
            _logLines.Clear();

            StopRunner();

            _runner = new SetupRunner(steps, validateOnly);
            _runner.OnStepStateChanged += HandleStepStateChanged;
            _runner.OnLogLine += AppendLogLine;
            _runner.OnCompleted += HandleCompleted;

            _page = WizardPage.Installing;
            _runner.Start();

            // 즉시 한 번 그려주기
            Repaint();
        }

        /// <summary>
        /// Runner의 이벤트 구독을 해제하고 리소스를 정리합니다.
        /// </summary>
        private void StopRunner()
        {
            if (_runner == null) return;

            _runner.OnStepStateChanged -= HandleStepStateChanged;
            _runner.OnLogLine -= AppendLogLine;
            _runner.OnCompleted -= HandleCompleted;

            _runner.Dispose();
            _runner = null;
        }

        /// <summary>
        /// Runner에서 스텝 상태가 바뀔 때 UI 상태를 갱신합니다.
        /// </summary>
        /// <param name="index">변경된 스텝의 인덱스입니다.</param>
        /// <param name="phase">현재 스텝 단계(Validate/Execute)입니다.</param>
        /// <param name="result">단계 실행 결과(실행 중/성공/실패/스킵)입니다.</param>
        /// <param name="message">상태 보조 메시지(에러/스킵 사유 등)입니다.</param>
        private void HandleStepStateChanged(int index, SetupRunner.StepPhase phase, SetupRunner.StepResult result, string message)
        {
            if (index < 0 || index >= _uiSteps.Length) return;

            var item = _uiSteps[index];

            switch (phase)
            {
                case SetupRunner.StepPhase.Validate:
                    item.State = result == SetupRunner.StepResult.Running ? StepUiState.Validating :
                                 result == SetupRunner.StepResult.Skipped ? StepUiState.Skipped :
                                 result == SetupRunner.StepResult.Succeeded ? StepUiState.Succeeded :
                                 StepUiState.Failed;
                    break;

                case SetupRunner.StepPhase.Execute:
                    item.State = result == SetupRunner.StepResult.Running ? StepUiState.Executing :
                                 result == SetupRunner.StepResult.Skipped ? StepUiState.Skipped :
                                 result == SetupRunner.StepResult.Succeeded ? StepUiState.Succeeded :
                                 StepUiState.Failed;
                    break;
            }

            item.Message = message;
            Repaint();
        }

        /// <summary>
        /// Runner가 완료(성공/실패/취소)되었을 때 결과 페이지로 전환합니다.
        /// </summary>
        /// <param name="runner">완료된 Runner 인스턴스입니다.</param>
        private void HandleCompleted(SetupRunner runner)
        {
            // Runner는 내부적으로 Progress.Remove까지 처리합니다.
            _page = WizardPage.Finish;
            Repaint();
        }

        /// <summary>
        /// Runner에서 전달되는 로그 한 줄을 UI 버퍼에 추가하고, 최대 라인 수를 유지합니다.
        /// </summary>
        /// <param name="line">추가할 로그 문자열입니다.</param>
        private void AppendLogLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;

            _logLines.Add(line);
            if (_logLines.Count > MaxLogLines)
                _logLines.RemoveRange(0, _logLines.Count - MaxLogLines);

            // 로그가 추가되면 자동 스크롤(하단 고정) 느낌을 주기 위해 마지막으로 이동
            _scrollLogs.y = float.MaxValue;
            Repaint();
        }

        #endregion

        #region Utils

        /// <summary>
        /// 프로젝트 셋업 로그가 저장되는 폴더를 OS 파일 탐색기로 엽니다.
        /// </summary>
        /// <remarks>
        /// 플랫폼별로 탐색기 실행 방식이 다르므로 분기 처리합니다.
        /// </remarks>
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

        #endregion
    }
}
#endif
