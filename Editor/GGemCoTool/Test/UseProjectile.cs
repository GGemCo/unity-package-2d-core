using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class UseProjectile : DefaultEditorWindow
    {
        private const string Title = "프로젝타일 사용툴";
        private TableProjectile _tableProjectile;
        private int _selectedIndex;
        private float _scale;
        private float _duration;
        
        private readonly List<string> _names = new List<string>();
        private readonly List<int> _uids = new List<int>();
        private Dictionary<int, Dictionary<string, string>> _tableDictionary;
        
        [MenuItem(ConfigEditor.NameToolUseProjectile, false, (int)ConfigEditor.ToolOrdering.UseProjectile)]
        public static void ShowWindow()
        {
            GetWindow<UseProjectile>(Title);
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedIndex = 0;
            _tableProjectile = TableLoaderManager.LoadProjectileTable();
            _tableDictionary = _tableProjectile.GetDatas();
            LoadTableInfoData();
        }

        private void OnGUI()
        {
            if (_selectedIndex >= _names.Count)
            {
                _selectedIndex = 0;
            }

            _selectedIndex = EditorGUILayout.Popup("프로젝타일 선택", _selectedIndex, _names.ToArray());

            _scale = EditorGUILayout.FloatField("Scale", _scale);
            _duration = EditorGUILayout.FloatField("Duration", _duration);
            
            if (GUILayout.Button("프로젝타일 사용"))
            {
                Create();
            }
        }

        private void Create()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }
            StruckAnimationEventEffect struckAnimationEventEffect = new StruckAnimationEventEffect
            {
                Uid = _uids[_selectedIndex],
                Scale = _scale,
                Duration = _duration
            };
            SceneGame.Instance.ProjectileManager.CreateProjectile(_uids[_selectedIndex]);
        }

        private void LoadTableInfoData()
        {
            _names.Clear();
            _uids.Clear();

            foreach (var kvp in _tableDictionary)
            {
                var info = _tableProjectile.GetDataByUid(kvp.Key);
                if (info.Uid <= 0) continue;

                _names.Add($"{info.Uid} - {info.Name}");
                _uids.Add(info.Uid);
            }
            _selectedIndex = 0; // 추가
        }
    }
}