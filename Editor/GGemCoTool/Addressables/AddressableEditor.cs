using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class AddressableEditor : DefaultEditorWindow
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
        private SettingCutscene _settingCutscene;
        private SettingSkill _settingSkill;

        public TableMap TableMap;
        public TableNpc TableNpc;
        public TableMonster TableMonster;
        public TableAnimation TableAnimation;
        public TableEffect TableEffect;
        public TableItem TableItem;
        public TableDialogue TableDialogue;
        public TableQuest TableQuest;
        public TableCutscene TableCutscene;
        public TableSkill TableSkill;
        private Vector2 _scrollPosition;

        [MenuItem(ConfigEditor.NameToolSettingAddressable, false, (int)ConfigEditor.ToolOrdering.SettingAddressable)]
        public static void ShowWindow()
        {
            GetWindow<AddressableEditor>(Title);
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
            _settingCutscene = new SettingCutscene(this);
            _settingSkill = new SettingSkill(this);
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
            TableCutscene = TableLoaderManager.LoadCutsceneTable();
            TableSkill = TableLoaderManager.LoadSkillTable();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.HelpBox("캐릭터 추가후 맵을 추가해야 Regen 정보가 반영됩니다.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            _settingScriptableObject.OnGUI();
            EditorGUILayout.Space(10);
            _settingTable.OnGUI();
            
            if (TableMonster == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Monster} 테이블이 없습니다.", MessageType.Info);
            }
            else {
                EditorGUILayout.Space(10);
                _settingCharacters.OnGUI();
            }
            
            if (TableMap == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Map} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingMap.OnGUI();
            }
            if (TableEffect == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Effect} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingEffect.OnGUI();
            }
            if (TableItem == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Item} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingItem.OnGUI();
            }
            if (TableSkill == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Skill} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingSkill.OnGUI();
            }
            if (TableDialogue == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Dialogue} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingDialogue.OnGUI();
            }
            if (TableQuest == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Quest} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingQuest.OnGUI();
            }
            if (TableCutscene == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Cutscene} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(10);
                _settingCutscene.OnGUI();
            }
            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }
    }
}