using UnityEditor;

namespace GGemCo.Editor
{
    public class DefaultEditorWindow : EditorWindow
    {
        public bool IsLoading = true;
        public TableLoaderManager TableLoaderManager;

        protected virtual void OnEnable()
        {
            IsLoading = true;
            TableLoaderManager = new TableLoaderManager();
        }
    }
}