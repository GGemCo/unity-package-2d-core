using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIWindowWorldMap : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("맵 아이콘이 들어갈 오브젝트")]
        public GameObject containerWorldMap;
        
        [Tooltip("이동하기 버튼")]
        [SerializeField] private Button buttonWarp;

        private UIIconWorldMap _selectedUIIconWorldMap;
        private MapManager _mapManager;
        private TableMap _tableMap;

        protected override void Awake()
        {
            _selectedUIIconWorldMap = null;
            uid = UIWindowConstants.WindowUid.WorldMap;
            if (TableLoaderManager.Instance == null) return;
            _tableMap = TableLoaderManager.Instance.TableMap;
            maxCountIcon = _tableMap.GetDatas().Count;

            // 순서 중요: IconPoolManager에서 사용 (슬롯 빌드 전략 등록 후 base.Awake 호출)
            SlotIconBuildStrategyRegistry.Register(uid, window => new SlotIconBuildStrategyWorldMap(_tableMap));

            base.Awake();
            
            buttonWarp?.onClick.AddListener(OnClickWarp);
        }

        protected override void Start()
        {
            base.Start();
            _mapManager = SceneGame.mapManager;
        }

        private void OnDestroy()
        {
            buttonWarp?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 슬롯 위치 정해주기
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="index"></param>
        public void SetPositionUiSlot(UISlot slot, int index)
        {
        }

        /// <summary>
        /// 월드맵 전용 선택 참조를 기본 selectedIcon 흐름과 동기화합니다.
        /// 버튼 액션은 이 참조를 사용하므로 선택 변경 시 함께 갱신합니다.
        /// </summary>
        /// <param name="icon">선택된 아이콘</param>
        protected override void OnSelectedIcon(UIIcon icon)
        {
            base.OnSelectedIcon(icon);
            _selectedUIIconWorldMap = icon as UIIconWorldMap;
        }

        protected override void OnClearedSelectedIcon()
        {
            base.OnClearedSelectedIcon();
            _selectedUIIconWorldMap = null;
        }

        private void OnClickWarp()
        {
            if (GcLogger.IsNull(_mapManager, nameof(MapManager))) return;
            if (GcLogger.IsNull(_selectedUIIconWorldMap, "선택된 맵이 없습니다.")) return;
            if (_selectedUIIconWorldMap.uid == _mapManager.GetCurrentMapUid()) return;
            _mapManager.LoadMap(_selectedUIIconWorldMap.uid);
        }
    }
}
