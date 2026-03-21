using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 프로젝트의 Addressables 관련 데이터(설정/테이블/캐릭터/맵/아이템 등)를 에디터에서 구성하기 위한 전용 윈도우입니다.
    /// 내부적으로 Setting* 모듈들을 구성하여, 각 모듈이 자신의 UI와 셋업 로직을 OnGUI에서 렌더링/수행하도록 합니다.
    /// </summary>
    /// <remarks>
    /// - 테이블 데이터는 일부 Setting 모듈(예: SettingMap)에서 참조하므로 OnEnable에서 선행 로드합니다.
    /// - UI는 2열 레이아웃(좌/우)에 모듈을 배치하는 형태로 구성되어 있습니다.
    /// </remarks>
    public class AddressableEditor : DefaultEditorWindow
    {
        private const string Title = "Addressable 셋팅하기";

        // 각 기능별 GUI/로직을 분리한 모듈(Setting*)들
        private SettingScriptableObject _settingScriptableObject;
        private SettingTable _settingTable;
        private SettingCharacters _settingCharacters;
        private SettingMap _settingMap;
        private SettingVfx _settingVfx;
        private SettingItem _settingItem;
        private SettingDialogue _settingDialogue;
        private SettingQuest _settingQuest;
        private SettingCutscene _settingCutscene;
        private SettingSound _settingSound;

        /// <summary>2열 레이아웃에서 각 모듈 버튼 영역의 폭입니다.</summary>
        public float buttonWidth;

        /// <summary>모듈 UI에서 사용하는 기본 버튼 높이입니다.</summary>
        public float buttonHeight;

        /// <summary>스크롤 위치(에디터 윈도우 리페인트 시 유지).</summary>
        private Vector2 _scrollPosition;

        /// <summary>
        /// Addressables 설정 윈도우를 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolSettingAddressable, false, (int)ConfigEditor.ToolOrdering.SettingAddressable)]
        public static void ShowWindow()
        {
            GetWindow<AddressableEditor>(Title);
        }

        /// <summary>
        /// 에디터 윈도우가 활성화될 때 모듈과 테이블을 초기화합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            buttonHeight = 40f;

            // 각 Setting* 모듈은 AddressableEditor(본 윈도우)를 통해 공용 상태/테이블/유틸에 접근합니다.
            _settingScriptableObject = new SettingScriptableObject(this);
            _settingTable = new SettingTable(this);
            _settingMap = new SettingMap(this);
            _settingCharacters = new SettingCharacters(this);
            _settingVfx = new SettingVfx(this);
            _settingItem = new SettingItem(this);
            _settingDialogue = new SettingDialogue(this);
            _settingQuest = new SettingQuest(this);
            _settingCutscene = new SettingCutscene(this);
            _settingSound = new SettingSound(this);
        }

        /// <summary>
        /// Addressables 설정 UI를 그립니다.
        /// 각 Setting* 모듈의 OnGUI를 2열 레이아웃으로 배치합니다.
        /// </summary>
        private void OnGUI()
        {
            // 2열 레이아웃 기준 버튼 폭 계산(좌/우 컬럼)
            buttonWidth = position.width / 2f - 10f;

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;

                // 구성 순서 의존성을 사용자에게 안내
                EditorGUILayout.HelpBox("캐릭터 추가 후 맵을 추가해야 맵별 배치되어있는 캐릭터 정보가 반영됩니다.", MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _settingScriptableObject?.OnGUI();
                    _settingTable?.OnGUI();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _settingCharacters?.OnGUI();
                    _settingMap?.OnGUI();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _settingVfx?.OnGUI();
                    _settingItem?.OnGUI();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _settingDialogue?.OnGUI();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _settingQuest?.OnGUI();
                    _settingCutscene?.OnGUI();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _settingSound?.OnGUI();
                }

                EditorGUILayout.Space(20);
            }
        }
    }
}
