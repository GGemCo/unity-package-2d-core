using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class UseEffect : DefaultEditorWindow
    {
        private const string Title = "이펙트 사용툴";
        private TableEffect _tableEffect;
        private int _selectedIndex;
        private float _scale;
        private float _duration;
        
        private readonly List<string> _names = new List<string>();
        private readonly List<int> _uids = new List<int>();
        private Dictionary<int, StruckTableEffect> _tableDictionary;
        
        [MenuItem(ConfigEditor.NameToolUseEffect, false, (int)ConfigEditor.ToolOrdering.UseEffect)]
        public static void ShowWindow()
        {
            GetWindow<UseEffect>(Title);
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedIndex = 0;
            _tableEffect = TableLoaderManager.LoadEffectTable();
            _tableDictionary = _tableEffect.GetDatas();
            LoadTableInfoData();
        }

        private void OnGUI()
        {
            if (_selectedIndex >= _names.Count)
            {
                _selectedIndex = 0;
            }

            _selectedIndex = EditorGUILayout.Popup("이펙트 선택", _selectedIndex, _names.ToArray());

            _scale = EditorGUILayout.FloatField("Scale", _scale);
            _duration = EditorGUILayout.FloatField("Duration", _duration);
            
            if (GUILayout.Button("이펙트 사용"))
            {
                CreateEffect();
            }
        }

        private void CreateEffect()
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
            DefaultEffect defaultEffect = SceneGame.Instance.EffectManager.CreateEffect(struckAnimationEventEffect);
            if (!defaultEffect) return;
            // 카테고리가 UI 일 경우는 Canvas 하위로 이동 한다.
            var info = _tableDictionary.GetValueOrDefault(struckAnimationEventEffect.Uid);
            if (info is { Category: EffectConstants.Category.UI })
            {
                defaultEffect.gameObject.transform.SetParent(SceneGame.Instance.canvasUI.transform);   
                defaultEffect.gameObject.transform.localPosition = Vector3.zero;             
            }
            else
            {
                defaultEffect.gameObject.transform.position = SceneGame.Instance.cameraManager.GetPositionCenter();
            }
            // 임시로 제일 위로 나오게 처리
            defaultEffect.SetSortingLayer(ConfigSortingLayer.Keys.UI);
        }

        private void LoadTableInfoData()
        {
            _names.Clear();
            _uids.Clear();

            foreach (var kvp in _tableDictionary)
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