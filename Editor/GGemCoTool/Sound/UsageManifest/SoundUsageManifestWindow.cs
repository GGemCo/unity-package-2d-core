using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Core 기본 사용처와 설치된 패키지 확장 사용처를 분석하고 매니페스트를 생성하는 에디터 창입니다.
    /// </summary>
    public sealed class SoundUsageManifestWindow : EditorWindow
    {
        private const string WindowTitle = "사운드 사용 매니페스트";

        private bool _rebuildRuntimeTablePack = true;
        private Vector2 _scrollPosition;
        private SoundUsageManifestBuildResult _lastResult;
        private SoundUsageManifestValidationResult _lastValidationResult;

        /// <summary>
        /// 사운드 사용 매니페스트 생성 도구를 엽니다.
        /// </summary>
        [MenuItem(
            ConfigEditor.NameToolSoundUsageManifest,
            false,
            (int)ConfigEditor.ToolOrdering.SoundUsageManifest)]
        public static void ShowWindow()
        {
            SoundUsageManifestWindow window = GetWindow<SoundUsageManifestWindow>(WindowTitle);
            window.minSize = new Vector2(620f, 420f);
            window.Show();
        }

        /// <summary>
        /// 분석 옵션, 실행 버튼 및 마지막 생성 결과를 IMGUI로 표시합니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "map의 regen_monster/regen_npc JSON, 캐릭터 애니메이션 이벤트, window 테이블의 UI 프리팹을 분석해 " +
                "sound_usage_manifest.txt를 생성합니다. 설치된 상위 패키지 분석기도 자동으로 실행되며, " +
                "Skill 패키지가 있으면 맵 몬스터의 스킬 사운드도 함께 수집합니다.",
                MessageType.Info);

            _rebuildRuntimeTablePack = EditorGUILayout.ToggleLeft(
                "생성 후 Core 런타임 테이블 팩 다시 만들기",
                _rebuildRuntimeTablePack);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전체 분석 및 매니페스트 생성", GUILayout.Height(34f)))
                    BuildManifest();

                if (GUILayout.Button("현재 매니페스트 검증", GUILayout.Width(150f), GUILayout.Height(34f)))
                    ValidateManifest();

                using (new EditorGUI.DisabledScope(
                           string.IsNullOrWhiteSpace(ConfigAddressableTable.TableSoundUsageManifest.Path)))
                {
                    if (GUILayout.Button("생성 파일 선택", GUILayout.Width(120f), GUILayout.Height(34f)))
                        PingGeneratedFile();
                }
            }

            EditorGUILayout.Space(10f);
            DrawResult();
            DrawValidationResult();
        }

        /// <summary>
        /// 자동 분석 빌더를 실행하고 콘솔 및 현재 창에 결과를 표시합니다.
        /// </summary>
        private void BuildManifest()
        {
            _lastResult = SoundUsageManifestBuilder.Build(_rebuildRuntimeTablePack);
            if (_lastResult.Succeeded)
            {
                _lastValidationResult = SoundUsageManifestValidator.Validate(checkStaleness: true);
                Debug.Log(
                    $"[SoundUsageManifest] 생성 완료. path={_lastResult.OutputPath}, records={_lastResult.RecordCount}, contributors={_lastResult.ContributorCount}, warnings={_lastResult.WarningCount}, validationErrors={_lastValidationResult.ErrorCount}");
                ShowNotification(new GUIContent(
                    _lastValidationResult.IsValid
                        ? "사운드 사용 매니페스트 생성 및 검증 완료"
                        : "생성 완료, 검증 오류 확인 필요"));
            }
            else
            {
                Debug.LogError("[SoundUsageManifest] 생성에 실패했습니다. 도구 창의 진단 메시지를 확인해주세요.");
                ShowNotification(new GUIContent("사운드 사용 매니페스트 생성 실패"));
            }

            Repaint();
        }


        /// <summary>
        /// 현재 매니페스트, 실제 AudioClip 및 Addressables 연결을 검사합니다.
        /// </summary>
        private void ValidateManifest()
        {
            _lastValidationResult = SoundUsageManifestValidator.Validate(checkStaleness: true);
            if (_lastValidationResult.IsValid)
            {
                Debug.Log(
                    $"[SoundUsageManifest] 검증 통과. rows={_lastValidationResult.ManifestRowCount}, resources={_lastValidationResult.ResourceCount}, warnings={_lastValidationResult.WarningCount}");
                ShowNotification(new GUIContent("사운드 매니페스트 검증 통과"));
            }
            else
            {
                Debug.LogError(
                    $"[SoundUsageManifest] 검증 실패. errors={_lastValidationResult.ErrorCount}, warnings={_lastValidationResult.WarningCount}");
                ShowNotification(new GUIContent("사운드 매니페스트 검증 실패"));
            }

            Repaint();
        }

        /// <summary>
        /// Project 창에서 생성된 sound_usage_manifest.txt 에셋을 선택하고 강조합니다.
        /// </summary>
        private static void PingGeneratedFile()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                ConfigAddressableTable.TableSoundUsageManifest.Path);
            if (asset == null)
            {
                EditorUtility.DisplayDialog(
                    WindowTitle,
                    "아직 생성된 sound_usage_manifest.txt가 없습니다.",
                    "확인");
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// 마지막 생성 결과의 요약과 경고 메시지를 스크롤 영역에 표시합니다.
        /// </summary>
        private void DrawResult()
        {
            if (_lastResult == null)
            {
                EditorGUILayout.HelpBox("아직 생성 작업을 실행하지 않았습니다.", MessageType.None);
                return;
            }

            MessageType resultType = _lastResult.Succeeded ? MessageType.Info : MessageType.Error;
            EditorGUILayout.HelpBox(
                $"성공: {_lastResult.Succeeded}\n" +
                $"출력: {_lastResult.OutputPath}\n" +
                $"행 수: {_lastResult.RecordCount}\n" +
                $"맵 범위 수: {_lastResult.MapScopeCount}\n" +
                $"UI 윈도우 범위 수: {_lastResult.UiWindowScopeCount}\n" +
                $"외부 분석기 수: {_lastResult.ContributorCount}\n" +
                $"경고 수: {_lastResult.WarningCount}\n" +
                $"런타임 테이블 팩 재생성: {_lastResult.RuntimeTablePackRebuilt}",
                resultType);

            EditorGUILayout.LabelField("진단 메시지", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            IReadOnlyList<string> messages = _lastResult.Messages;
            for (int i = 0; i < messages.Count; i++)
                EditorGUILayout.SelectableLabel(messages[i], GUILayout.MinHeight(18f));
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 마지막 매니페스트 검증 결과를 심각도와 함께 표시합니다.
        /// </summary>
        private void DrawValidationResult()
        {
            if (_lastValidationResult == null)
                return;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("매니페스트 검증 결과", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"통과: {_lastValidationResult.IsValid}\n" +
                $"행 수: {_lastValidationResult.ManifestRowCount}, 리소스 수: {_lastValidationResult.ResourceCount}\n" +
                $"오류: {_lastValidationResult.ErrorCount}, 경고: {_lastValidationResult.WarningCount}",
                _lastValidationResult.IsValid ? MessageType.Info : MessageType.Error);

            IReadOnlyList<SoundUsageValidationMessage> messages = _lastValidationResult.Messages;
            for (int i = 0; i < messages.Count; i++)
            {
                SoundUsageValidationMessage message = messages[i];
                MessageType type = message.Severity switch
                {
                    SoundUsageValidationSeverity.Error => MessageType.Error,
                    SoundUsageValidationSeverity.Warning => MessageType.Warning,
                    _ => MessageType.None,
                };
                EditorGUILayout.HelpBox(message.Message, type);
            }
        }
    }
}
