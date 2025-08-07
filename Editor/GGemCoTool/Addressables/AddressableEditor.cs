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
        private SettingAffect _settingAffect;
        private SettingSound _settingSound;

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
        public TableAffect TableAffect;
        public TableSound TableSound;
        
        public float buttonWidth;
        public float buttonHeight;
        
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
            
            buttonHeight = 40f;
            
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
            _settingAffect = new SettingAffect(this);
            _settingSound = new SettingSound(this);
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
            TableAffect = TableLoaderManager.LoadAffectTable();
            TableSound = TableLoaderManager.LoadSoundTable();
        }

        private void OnGUI()
        {
            buttonWidth = position.width / 2f - 10f;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.HelpBox("캐릭터 추가 후 맵을 추가해야 맵별 배치되어있는 캐릭터 정보가 반영됩니다.", MessageType.Error);
            
            EditorGUILayout.BeginHorizontal();
            _settingScriptableObject.OnGUI();
            _settingTable.OnGUI();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            _settingCharacters.OnGUI();
            _settingMap.OnGUI();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            _settingEffect.OnGUI();
            _settingItem.OnGUI();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            _settingSkill.OnGUI();
            _settingDialogue.OnGUI();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            _settingQuest.OnGUI();
            _settingCutscene.OnGUI();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            _settingAffect.OnGUI();
            _settingSound.OnGUI();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }
    }
}