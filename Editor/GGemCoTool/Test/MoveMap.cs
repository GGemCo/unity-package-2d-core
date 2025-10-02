using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class MoveMap : DefaultEditorWindow
    {
        private const string Title = "맵 이동툴";
        private TableMap _tableMap;
        private int _selectedIndex;
        
        private readonly List<string> _names = new List<string>();
        private readonly List<int> _uids = new List<int>();
        private Dictionary<int, Dictionary<string, string>> _tableDictionary;
        
        [MenuItem(ConfigEditor.NameToolMoveMap, false, (int)ConfigEditor.ToolOrdering.MoveMap)]
        public static void ShowWindow()
        {
            GetWindow<MoveMap>(Title);
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedIndex = 0;
            _tableMap = TableLoaderManager.LoadMapTable();
            _tableDictionary = _tableMap.GetDatas();
            LoadTableInfoData();
        }

        private void OnGUI()
        {
            if (_selectedIndex >= _names.Count)
            {
                _selectedIndex = 0;
            }

            _selectedIndex = EditorGUILayout.Popup("맵 선택", _selectedIndex, _names.ToArray());
            if (GUILayout.Button("맵 이동"))
            {
                ChangeMap();
            }
        }

        private void ChangeMap()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }
            SceneGame.Instance.mapManager.LoadMap(_uids[_selectedIndex]);
        }

        private void LoadTableInfoData()
        {
            _names.Clear();
            _uids.Clear();

            foreach (var kvp in _tableDictionary)
            {
                var info = _tableMap.GetDataByUid(kvp.Key);
                if (info.Uid <= 0) continue;

                _names.Add($"{info.Uid} - {info.Name}");
                _uids.Add(info.Uid);
            }
            _selectedIndex = 0; // 추가
        }
    }
}