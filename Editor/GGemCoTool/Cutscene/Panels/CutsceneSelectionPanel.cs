using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 연출 선택/정보/기본 액션 섹션만 담당하는 패널입니다.
    /// 실제 처리 로직은 외부 콜백으로 위임합니다.
    /// </summary>
    internal sealed class CutsceneSelectionPanel
    {
        private readonly string _title;

        public CutsceneSelectionPanel(string title)
        {
            _title = title;
        }

        public void Draw(
            CutsceneEditorState state,
            IReadOnlyList<SearchableDropdownUtility.Option<StruckTableCutscene>> dropDownOptions,
            Func<string> getSelectedCutsceneJsonPath,
            Action<StruckTableCutscene> onCutsceneSelected,
            Action playSelectedCutscene,
            Action pingSelectedCutsceneJson,
            Action importSelectedCutsceneJsonToTempTimeline,
            Action repaint)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("연출 선택", EditorStyles.boldLabel);

                if (dropDownOptions == null || dropDownOptions.Count == 0)
                {
                    EditorGUILayout.HelpBox("등록된 연출이 없습니다. cutscene 테이블을 확인해주세요.", MessageType.Warning);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Cutscene");

                    var currentText = state.SelectedCutscene != null
                        ? $"{state.SelectedCutscene.Uid} - {state.SelectedCutscene.Memo}"
                        : "선택...";

                    var selectedIndex = state.SelectedCutscene != null ? state.SelectedCutscene.Uid : 0;

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: dropDownOptions,
                        selectedIndex: selectedIndex,
                        onSelected: (idx, opt) =>
                        {
                            onCutsceneSelected?.Invoke(opt.Data);
                            repaint?.Invoke();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                if (state.SelectedCutscene == null)
                {
                    return;
                }

                EditorGUILayout.LabelField("UID", state.SelectedCutscene.Uid.ToString());
                EditorGUILayout.LabelField("Memo", state.SelectedCutscene.Memo);
                EditorGUILayout.LabelField("FileName", state.SelectedCutscene.FileName);
                EditorGUILayout.LabelField("Json Path", getSelectedCutsceneJsonPath?.Invoke() ?? string.Empty);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!Application.isPlaying))
                    {
                        if (GUILayout.Button("연출 플레이", GUILayout.Height(24)))
                        {
                            playSelectedCutscene?.Invoke();
                        }
                    }

                    if (GUILayout.Button("Json 에셋 선택", GUILayout.Height(24)))
                    {
                        pingSelectedCutsceneJson?.Invoke();
                    }

                    if (GUILayout.Button("Json -> Temp Timeline", GUILayout.Height(24)))
                    {
                        importSelectedCutsceneJsonToTempTimeline?.Invoke();
                    }
                }
            }
        }
    }
}
