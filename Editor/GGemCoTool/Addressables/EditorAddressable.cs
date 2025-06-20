using System.Threading.Tasks;
using GGemCo.Scripts;
using UnityEditor;

namespace GGemCo.Editor
{
    public class EditorAddressable : DefaultEditorWindow
    {
        private const string Title = "Addressable 셋팅하기";
        
        private readonly SettingScriptableObject _settingScriptableObject = new SettingScriptableObject();
        private SettingTable _settingTable;
        private SettingMonster _settingMonster;
        private SettingMap _settingMap;

        public TableMap TableMap;
        public TableNpc TableNpc;
        public TableMonster TableMonster;

        [MenuItem(ConfigEditor.NameToolAddressableSetting, false, (int)ConfigEditor.ToolOrdering.SettingAddressable)]
        public static void ShowWindow()
        {
            GetWindow<EditorAddressable>(Title);
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            _settingTable = new SettingTable(this);
            _settingMonster = new SettingMonster();
            _settingMap = new SettingMap(this);
            _ = LoadAsync();
        }
        public async Task LoadAsync()
        {
            try
            {
                var loadMap = TableLoaderManager.LoadMapTableAsync();
                var loadNpc = TableLoaderManager.LoadNpcTableAsync();
                var loadMonster = TableLoaderManager.LoadMonsterTableAsync();
                
                await Task.WhenAll(loadMap, loadNpc, loadMonster);

                TableMap = loadMap.Result;
                TableNpc = loadNpc.Result;
                TableMonster = loadMonster.Result;

                isLoading = false;
                Repaint();
            }
            catch (System.Exception ex)
            {
                ShowLoadTableException(Title, ex);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            _settingScriptableObject.OnGUI();
            EditorGUILayout.Space(10);
            _settingTable.OnGUI();

            if (TableMap == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("map 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingMap.OnGUI();
            }

            if (TableMonster == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("monster 테이블이 없습니다.", MessageType.Info);
            }
            else {
                EditorGUILayout.Space(10);
                _settingMonster.OnGUI();
            }
        }
    }
}