using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 컷신 선택, 선택 정보 표시, 기본 실행 액션을 담당하는 에디터 패널입니다.
    /// 실제 데이터 변경 및 실행 로직은 외부 콜백으로 위임합니다.
    /// </summary>
    internal sealed class CutsceneSelectionPanel
    {
        /// <summary>
        /// 패널 식별 또는 표시 용도로 보관하는 제목입니다.
        /// </summary>
        private readonly string _title;

        /// <summary>
        /// 컷신 선택 패널을 초기화합니다.
        /// </summary>
        /// <param name="title">패널의 제목 또는 구분용 문자열입니다.</param>
        public CutsceneSelectionPanel(string title)
        {
            _title = title;
        }

        /// <summary>
        /// 컷신 선택 UI와 선택된 컷신의 상세 정보 및 관련 액션 버튼을 그립니다.
        /// </summary>
        /// <param name="state">현재 선택 상태와 에디터 UI 상태를 보관하는 객체입니다.</param>
        /// <param name="dropDownOptions">드롭다운에 표시할 컷신 선택 항목 목록입니다.</param>
        /// <param name="getSelectedCutsceneJsonPath">현재 선택된 컷신의 JSON 경로를 반환하는 콜백입니다.</param>
        /// <param name="onCutsceneSelected">드롭다운에서 컷신 선택이 변경되었을 때 호출되는 콜백입니다.</param>
        /// <param name="playSelectedCutscene">선택된 컷신 재생을 요청하는 콜백입니다.</param>
        /// <param name="pingSelectedCutsceneJson">선택된 컷신의 JSON 에셋을 프로젝트 창에서 표시하는 콜백입니다.</param>
        /// <param name="importSelectedCutsceneJsonToTempTimeline">선택된 컷신의 JSON을 Temp Timeline으로 가져오는 콜백입니다.</param>
        /// <param name="repaint">선택 변경 후 UI를 다시 그리기 위한 콜백입니다.</param>
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
                            // 선택된 컷신을 외부 상태에 반영합니다.
                            onCutsceneSelected?.Invoke(opt.Data);

                            // 선택 변경 내용을 즉시 UI에 반영합니다.
                            repaint?.Invoke();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                if (state.SelectedCutscene == null)
                {
                    return;
                }

                // 현재 선택된 컷신의 기본 메타데이터를 표시합니다.
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