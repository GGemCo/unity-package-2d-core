using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 효과 TimelineAsset을 검증하고 RuntimeSequence로 베이크하는 EditorWindow입니다.
    /// </summary>
    public sealed class UIEffectTimelineEditorWindow : EditorWindow
    {
        private TimelineAsset _timelineAsset;
        private UIEffectRuntimeSequence _runtimeSequence;
        private string _outputPath = "Assets/Editor/UIEffectTimeline/UIEffectRuntimeSequence.asset";
        private Vector2 _scrollPosition;
        private readonly List<string> _messages = new List<string>();

        /// <summary>
        /// UI 효과 타임라인 편집툴을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolUIEffectTimeline, false, (int)ConfigEditor.ToolOrdering.UIEffectTimeline)]
        public static void Open()
        {
            var window = GetWindow<UIEffectTimelineEditorWindow>();
            window.titleContent = new GUIContent("UI Effect Timeline");
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawSelectionPanel();
            EditorGUILayout.Space(8f);
            DrawBakePanel();
            EditorGUILayout.Space(8f);
            DrawPreviewPanel();
            EditorGUILayout.Space(8f);
            DrawMessagePanel();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 원본 Timeline과 출력 RuntimeSequence 경로 선택 UI를 그립니다.
        /// </summary>
        private void DrawSelectionPanel()
        {
            EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);
            _timelineAsset = (TimelineAsset)EditorGUILayout.ObjectField("Timeline Asset", _timelineAsset, typeof(TimelineAsset), false);
            _runtimeSequence = (UIEffectRuntimeSequence)EditorGUILayout.ObjectField("Runtime Sequence", _runtimeSequence, typeof(UIEffectRuntimeSequence), false);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택된 Sequence 경로 사용"))
                {
                    ApplySelectedSequencePath();
                }

                if (GUILayout.Button("출력 경로 선택"))
                {
                    SelectOutputPath();
                }
            }
        }

        /// <summary>
        /// 검증과 베이크 버튼 UI를 그립니다.
        /// </summary>
        private void DrawBakePanel()
        {
            EditorGUILayout.LabelField("Bake", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                {
                    Validate();
                }

                if (GUILayout.Button("Bake"))
                {
                    Bake();
                }
            }
        }

        /// <summary>
        /// Play Mode에서 RuntimeSequence를 실제로 재생하는 미리보기 UI를 그립니다.
        /// </summary>
        private void DrawPreviewPanel()
        {
            EditorGUILayout.LabelField("Play Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("씬에 UIEffectTimelineTargetRegistry를 배치하고 targetKey를 등록하면 Play Mode에서 실제 UI에 재생할 수 있습니다.", MessageType.Info);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || _runtimeSequence == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Preview Play"))
                    {
                        PreviewPlay();
                    }

                    if (GUILayout.Button("Preview Stop"))
                    {
                        PreviewStop();
                    }
                }
            }
        }

        /// <summary>
        /// 검증/베이크 결과 메시지를 그립니다.
        /// </summary>
        private void DrawMessagePanel()
        {
            if (_messages.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField("Messages", EditorStyles.boldLabel);
            foreach (string message in _messages)
            {
                EditorGUILayout.HelpBox(message, MessageType.None);
            }
        }

        /// <summary>
        /// 선택된 RuntimeSequence 에셋의 경로를 출력 경로로 사용합니다.
        /// </summary>
        private void ApplySelectedSequencePath()
        {
            if (_runtimeSequence == null)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(_runtimeSequence);
            if (!string.IsNullOrEmpty(path))
            {
                _outputPath = path;
            }
        }

        /// <summary>
        /// 저장 패널을 열어 RuntimeSequence 출력 경로를 선택합니다.
        /// </summary>
        private void SelectOutputPath()
        {
            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "UI Effect Runtime Sequence 저장",
                _timelineAsset != null ? _timelineAsset.name + "_RuntimeSequence" : "UIEffectRuntimeSequence",
                "asset",
                "RuntimeSequence 에셋 저장 경로를 선택하세요.");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                _outputPath = selectedPath;
            }
        }

        /// <summary>
        /// 현재 TimelineAsset의 UIEffectClip 설정을 검증합니다.
        /// </summary>
        private void Validate()
        {
            _messages.Clear();
            bool isValid = UIEffectTimelineValidationUtility.Validate(_timelineAsset, out List<string> messages);
            _messages.AddRange(messages);
            if (isValid)
            {
                _messages.Add("검증이 완료되었습니다. 오류가 없습니다.");
            }
        }

        /// <summary>
        /// 현재 TimelineAsset을 RuntimeSequence로 베이크합니다.
        /// </summary>
        private void Bake()
        {
            _messages.Clear();
            if (!UIEffectTimelineValidationUtility.Validate(_timelineAsset, out List<string> messages))
            {
                _messages.AddRange(messages);
                return;
            }

            _runtimeSequence = UIEffectTimelineBaker.Bake(_timelineAsset, _outputPath);
            if (_runtimeSequence != null)
            {
                EditorGUIUtility.PingObject(_runtimeSequence);
                _messages.Add($"베이크 완료: {_runtimeSequence.events.Length}개 이벤트, {_runtimeSequence.payloads.Length}개 Payload");
            }
            else
            {
                _messages.Add("베이크에 실패했습니다.");
            }
        }

        /// <summary>
        /// Play Mode에서 선택된 RuntimeSequence를 재생합니다.
        /// </summary>
        private void PreviewPlay()
        {
            UIEffectTimelinePlayer player = Object.FindObjectOfType<UIEffectTimelinePlayer>();
            if (player == null)
            {
                var playerObject = new GameObject("UIEffectTimelinePlayer_Preview");
                player = playerObject.AddComponent<UIEffectTimelinePlayer>();
            }

            player.Play(_runtimeSequence);
        }

        /// <summary>
        /// Play Mode에서 실행 중인 Preview 재생을 중지합니다.
        /// </summary>
        private void PreviewStop()
        {
            UIEffectTimelinePlayer player = Object.FindObjectOfType<UIEffectTimelinePlayer>();
            if (player != null)
            {
                player.Stop();
            }
        }
    }
}
