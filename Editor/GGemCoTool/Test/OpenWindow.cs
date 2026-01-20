using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class OpenWindow : DefaultEditorWindow
    {
        private const string Title = "아이템 생성툴";
        private TableWindow _tableWindow;
        private int _selectedIndex;
        private readonly List<string> _names = new List<string>();
        private readonly List<int> _uids = new List<int>();
        private Dictionary<int, StruckTableWindow> _dictionary;
        
        [MenuItem(ConfigEditor.NameToolOpenWindow, false, (int)ConfigEditor.ToolOrdering.OpenWindow)]
        public static void ShowWindow()
        {
            GetWindow<OpenWindow>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedIndex = 0;
            _tableWindow = TableLoaderManager.LoadWindowTable();
            _dictionary = _tableWindow.GetDatas();
            LoadItemInfoData();
        }
        private void OnGUI()
        {
            if (_selectedIndex >= _names.Count)
            {
                _selectedIndex = 0;
            }
            _selectedIndex = EditorGUILayout.Popup("윈도우 선택", _selectedIndex, _names.ToArray());
            if (GUILayout.Button("윈도우 열기")) OpenUIWindow();
        }

        private void OpenUIWindow()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }
            int uid = _uids[_selectedIndex];
            if (uid <= 0)
            {
                EditorUtility.DisplayDialog(Title, "오픈할 윈도우를 선택해주세요.", "OK");
                return;
            }

            SceneGame.Instance.uIWindowManager.ShowWindow((UIWindowConstants.WindowUid)uid, true);
        }

        private void LoadItemInfoData()
        {
            _names.Clear();
            _uids.Clear();

            foreach (var kvp in _dictionary)
            {
                var info = kvp.Value;
                if (info.Uid <= 0) continue;

                _names.Add($"{info.Uid} - {info.Name}");
                _uids.Add(info.Uid);
            }
            _selectedIndex = 0; // 추가
        }
    }
}