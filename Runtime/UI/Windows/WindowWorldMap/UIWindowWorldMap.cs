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

        public void SetSelectedMap(UIIconWorldMap uiElementWorldMap)
        {
            if (_selectedUIIconWorldMap != null)
            {
                _selectedUIIconWorldMap.SetSelected(false);
            }
            _selectedUIIconWorldMap = uiElementWorldMap;
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