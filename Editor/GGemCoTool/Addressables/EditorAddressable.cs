using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public class EditorAddressable : DefaultEditorWindow
    {
        private const string Title = "Addressable 셋팅하기";
        
        private SettingScriptableObject _settingScriptableObject;
        private SettingTable _settingTable;
        private SettingCharacters _settingCharacters;
        private SettingMap _settingMap;
        private SettingEffect _settingEffect;
        private SettingItem _settingItem;
        private SettingDialogue _settingDialogue;
        private SettingQuest _settingQuest;

        public TableMap TableMap;
        public TableNpc TableNpc;
        public TableMonster TableMonster;
        public TableAnimation TableAnimation;
        public TableEffect TableEffect;
        public TableItem TableItem;
        public TableDialogue TableDialogue;
        public TableQuest TableQuest;

        [MenuItem(ConfigEditor.NameToolSettingAddressable, false, (int)ConfigEditor.ToolOrdering.SettingAddressable)]
        public static void ShowWindow()
        {
            GetWindow<EditorAddressable>(Title);
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            // _settingMap 에서 테이블을 사용하기 때문에 테이블 먼저 로드해야 함
            LoadTables();
            
            _settingScriptableObject = new SettingScriptableObject(this);
            _settingTable = new SettingTable(this);
            _settingMap = new SettingMap(this);
            _settingCharacters = new SettingCharacters(this);
            _settingEffect = new SettingEffect(this);
            _settingItem = new SettingItem(this);
            _settingDialogue = new SettingDialogue(this);
            _settingQuest = new SettingQuest(this);
        }

        public void LoadTables()
        {
            TableMap = TableLoaderManager.LoadMapTable();
            TableNpc = TableLoaderManager.LoadNpcTable();
            TableMonster = TableLoaderManager.LoadMonsterTable();
            TableAnimation = TableLoaderManager.LoadSpineTable();
            TableEffect = TableLoaderManager.LoadEffectTable();
            TableItem = TableLoaderManager.LoadItemTable();
            TableDialogue = TableLoaderManager.LoadDialogueTable();
            TableQuest = TableLoaderManager.LoadQuestTable();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("캐릭터 추가후 맵을 추가해야 Regen 정보가 반영됩니다.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            _settingScriptableObject.OnGUI();
            EditorGUILayout.Space(10);
            _settingTable.OnGUI();
            
            if (TableMonster == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("monster 테이블이 없습니다.", MessageType.Info);
            }
            else {
                EditorGUILayout.Space(10);
                _settingCharacters.OnGUI();
            }
            
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
            if (TableEffect == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("effect 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingEffect.OnGUI();
            }
            if (TableItem == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("item 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingItem.OnGUI();
            }
            if (TableDialogue == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("dialogue 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingDialogue.OnGUI();
            }
            if (TableQuest == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("quest 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingQuest.OnGUI();
            }
        }
    }
}