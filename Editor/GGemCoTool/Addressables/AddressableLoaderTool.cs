using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo2DCore;

namespace GGemCo.Editor
{
    public class AddressableLoaderTool : EditorWindow
    {
        private string _keyInput = "";
        private string _labelInput = "";

        private Object _loadedByKey;
        private Dictionary<string, Object> _loadedByLabel = new Dictionary<string, Object>();
        private bool _isLoading;

        [MenuItem(ConfigEditor.NameToolLoadAddressable, false, (int)ConfigEditor.ToolOrdering.LoadAddressable)]
        public static void ShowWindow()
        {
            var window = GetWindow<AddressableLoaderTool>();
            window.titleContent = new GUIContent("Addressable 로더 툴");
            window.minSize = new Vector2(400, 250);
        }

        private void OnGUI()
        {
            GUILayout.Label("Addressable 리소스 로더", EditorStyles.boldLabel);

            GUILayout.Space(10);
            _keyInput = EditorGUILayout.TextField("Key 로드", _keyInput);
            if (GUILayout.Button("Key 로드하기"))
            {
                _ = LoadByKeyAsync(_keyInput);
            }

            if (_loadedByKey)
            {
                EditorGUILayout.ObjectField("로드된 리소스", _loadedByKey, typeof(Object), false);
                if (GUILayout.Button("Key 리소스 해제"))
                {
                    AddressableLoaderController.Release(_loadedByKey);
                    _loadedByKey = null;
                }
            }

            GUILayout.Space(20);
            _labelInput = EditorGUILayout.TextField("Label 로드", _labelInput);
            if (GUILayout.Button("Label로 리소스 로드하기"))
            {
                _ = LoadByLabelAsync(_labelInput);
            }
            if (_isLoading)
            {
                GUILayout.Label("로드 중입니다...", EditorStyles.helpBox);
            }

            if (_loadedByLabel.Count <= 0) return;
            GUILayout.Label($"로드된 개수: {_loadedByLabel.Count}", EditorStyles.miniLabel);
            foreach (var obj in _loadedByLabel)
            {
                EditorGUILayout.ObjectField(obj.Value.name, obj.Value, typeof(Object), false);
            }

            if (GUILayout.Button("모든 Label 리소스 해제"))
            {
                ReleaseAllLabelResources();
            }
        }

        private async Task LoadByKeyAsync(string key)
        {
            _isLoading = true;
            _loadedByKey = await AddressableLoaderController.LoadByKeyAsync<Object>(key);
            _isLoading = false;
            if (!_loadedByKey)
                Debug.LogError("[AddressableLoaderTool] Key 로드 실패");
        }

        private async Task LoadByLabelAsync(string label)
        {
            _isLoading = true;
            var results = await AddressableLoaderController.LoadByLabelAsync<Object>(label);
            _isLoading = false;
            if (results != null)
                _loadedByLabel = results;
            else
                Debug.LogError("[AddressableLoaderTool] Label 로드 실패");
        }
        private void ReleaseAllLabelResources()
        {
            foreach (var obj in _loadedByLabel)
            {
                AddressableLoaderController.Release(obj.Value);
            }

            _loadedByLabel.Clear();
        }
    }
}
