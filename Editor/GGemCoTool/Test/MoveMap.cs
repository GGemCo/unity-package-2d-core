using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class MoveMap : DefaultEditorWindow
    {
        private const string Title = "맵 이동툴";
        
        // Tables
        private TableMap _tableAffect;
        private Dictionary<int, StruckTableMap> _dictionary;
        
        // Dropdown data
        private readonly List<SearchableDropdownUtility.Option<StruckTableMap>> _dropDownOptions = new();
        private StruckTableMap _selectedData;
        
        private Vector2 _scroll;
        private string _lastReloadMessage = string.Empty;
        
        [MenuItem(ConfigEditor.NameToolMoveMap, false, (int)ConfigEditor.ToolOrdering.MoveMap)]
        public static void ShowWindow()
        {
            GetWindow<MoveMap>(Title);
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            _selectedData = null;
            selectedCharacterIndex = 0;
            selectedCharacter = null;

            ReloadAllTables();
            RefreshSceneCharacters();
        }

        protected override void OnSelectedCharacterChanged(CharacterBase character)
        {
            Repaint();
        }
        
        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                EditorGUILayout.Space(6);

                DrawPlayModeGate();
                EditorGUILayout.Space(6);

                DrawSection();
                EditorGUILayout.Space(8);
                
                DrawApplySection();
                EditorGUILayout.Space(8);

                DrawReloadSection();
                EditorGUILayout.Space(20);
            }
        }

        #region GUI
        
        private void DrawSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Map");

                    if (_dropDownOptions.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Map 테이블이 비어있습니다. 테이블 로딩/Addressables 설정을 확인해주세요.", MessageType.Warning);
                        return;
                    }

                    string currentText = _selectedData != null ? _selectedData.Name : "선택...";
                    int selectIndex = _selectedData?.Uid ?? 0;

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _dropDownOptions,
                        selectedIndex: selectIndex,
                        onSelected: (idx, opt) =>
                        {
                            _selectedData = opt.Data;
                            Repaint();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                if (_selectedData != null)
                {
                    EditorGUILayout.LabelField("UID", _selectedData.Uid.ToString());
                    EditorGUILayout.LabelField("Name", _selectedData.Name);
                }
            }
        }
        
        private void DrawApplySection()
        {
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("맵 이동", GUILayout.Height(26)))
                    ApplySelected();
            }
        }
        private void DrawReloadSection()
        {
            DrawTableReloadSection(
                _lastReloadMessage,
                "map 재로딩",
                ReloadAllTables);
        }
        #endregion
        
        private void ReloadAllTables()
        {
            try
            {
                _tableAffect = TableLoaderManager.LoadMapTable();

                _dictionary = _tableAffect?.GetDatas();
                RebuildDropdown();

                _lastReloadMessage = $"테이블 재로딩 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _lastReloadMessage = $"테이블 재로딩 실패: {e.GetType().Name} - {e.Message}";
            }

            Repaint();
        }
        
        private void RebuildDropdown()
        {
            RebuildDropdownOptions(
                source: _dictionary?.Values,
                targetOptions: _dropDownOptions,
                isValidRow: row => row.Uid > 0,
                keySelector: row => row.Uid.ToString(),
                valueSelector: row => row.Name,
                assignSelected: row => _selectedData = row);
        }
        
        private void ApplySelected()
        {
            if (!Application.isPlaying || !SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }
            SceneGame.Instance.mapManager.LoadMap(_selectedData.Uid);
        }
    }
}