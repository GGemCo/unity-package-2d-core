using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo.Scripts;
using UnityEditor.AddressableAssets;

namespace GGemCo.Editor
{
    public class AddressableLoaderTool : EditorWindow
    {
        private string keyInput = "";
        private string labelInput = "";

        private UnityEngine.Object loadedByKey;
        private List<UnityEngine.Object> loadedByLabel = new List<UnityEngine.Object>();
        private bool isLoading = false;

        [MenuItem("GGemCo/툴/Addressable 로더 툴")]
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
            keyInput = EditorGUILayout.TextField("Key 로드", keyInput);
            if (GUILayout.Button("Key 로드하기"))
            {
                _ = LoadByKeyAsync(keyInput);
            }

            if (loadedByKey != null)
            {
                EditorGUILayout.ObjectField("로드된 리소스", loadedByKey, typeof(UnityEngine.Object), false);
                if (GUILayout.Button("Key 리소스 해제"))
                {
                    AddressableLoaderManager.Release(loadedByKey);
                    loadedByKey = null;
                }
            }

            GUILayout.Space(20);
            labelInput = EditorGUILayout.TextField("Label 로드", labelInput);
            if (GUILayout.Button("Label로 리소스 로드하기"))
            {
                _ = LoadByLabelAsync(labelInput);
            }
            if (isLoading)
            {
                GUILayout.Label("로드 중입니다...", EditorStyles.helpBox);
            }

            if (loadedByLabel.Count > 0)
            {
                GUILayout.Label($"로드된 개수: {loadedByLabel.Count}", EditorStyles.miniLabel);
                foreach (var obj in loadedByLabel)
                {
                    EditorGUILayout.ObjectField(obj.name, obj, typeof(UnityEngine.Object), false);
                }

                if (GUILayout.Button("모든 Label 리소스 해제"))
                {
                    ReleaseAllLabelResources();
                }
            }
        }

        private async Task LoadByKeyAsync(string key)
        {
            isLoading = true;
            loadedByKey = await AddressableLoaderManager.LoadByKeyAsync<UnityEngine.Object>(key);
            isLoading = false;
            if (loadedByKey == null)
                Debug.LogError("[AddressableLoaderTool] Key 로드 실패");
        }

        private async Task LoadByLabelAsync(string label)
        {
            isLoading = true;
            var results = await AddressableLoaderManager.LoadByLabelAsync<UnityEngine.Object>(label);
            isLoading = false;
            if (results != null)
                loadedByLabel = results;
            else
                Debug.LogError("[AddressableLoaderTool] Label 로드 실패");
        }
        private void ReleaseAllLabelResources()
        {
            foreach (var obj in loadedByLabel)
            {
                AddressableLoaderManager.Release(obj);
            }

            loadedByLabel.Clear();
        }
    }
}
