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
    }
}