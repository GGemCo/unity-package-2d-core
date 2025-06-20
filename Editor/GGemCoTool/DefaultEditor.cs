using System;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class DefaultEditor : UnityEditor.Editor
    {
        public bool IsLoading = true;
        public TableLoaderManager TableLoaderManager;
        
        protected virtual void OnEnable()
        {
            IsLoading = true;
            TableLoaderManager = new TableLoaderManager();
        }
        protected void ShowLoadTableException(string ptitle, Exception ex)
        {
            Debug.LogError($"{ptitle} LoadAsync 예외 발생: {ex.Message}");
            EditorUtility.DisplayDialog(ptitle, "테이블 로딩 중 오류가 발생했습니다.", "OK");
            IsLoading = false;
        }
    }
}