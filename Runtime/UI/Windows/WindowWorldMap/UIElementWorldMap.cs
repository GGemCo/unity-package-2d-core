using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 윈도우 - 맵 리스트 element
    /// </summary>
    public class UIElementWorldMap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler 
    {
        public Vector3 iconPosition;
        public TextMeshProUGUI textName;
        public TextMeshProUGUI textLevel;
        public TextMeshProUGUI textNeedLevel;
        
        private UIWindowWorldMap _uiWindowWorldMap;
        private UIWindowWorldMapInfo _uiWindowWorldMapInfo;
        private StruckTableMap _struckTableMap;
        private SaveDataIcon _saveDataIcon;
        private TableMap _tableWorldMap;
        private int _slotIndex;

        private LocalizationManager _localizationManager;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="uiWindowWorldMap"></param>
        /// <param name="slotIndex"></param>
        /// <param name="struckTableMap"></param>
        public void Initialize(UIWindowWorldMap uiWindowWorldMap, int slotIndex, StruckTableMap struckTableMap)
        {
            _slotIndex = slotIndex;
            _struckTableMap = struckTableMap;

            _uiWindowWorldMap = uiWindowWorldMap;
            _tableWorldMap = TableLoaderManager.Instance.TableMap;
            _localizationManager = LocalizationManager.Instance;
        }
        
        private void Start()
        {
            _uiWindowWorldMapInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowWorldMapInfo>(
                    UIWindowConstants.WindowUid.WorldMapInfo);
        }

        /// <summary>
        /// slotIndex 로 아이템 정보를 가져온다.
        /// SaveDataIcon 정보에 따라 버튼 visible 업데이트
        /// </summary>
        public void UpdateInfos(SaveDataIcon saveDataIcon)
        {
            if (saveDataIcon == null)
            {
                GcLogger.LogError($"저장된 정보가 없습니다.");
                return;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // todo. 정리 필요
            // _uiWindowWorldMapInfo.SetWorldMapUid(_struckTableMap.Uid, _struckTableMap.Level, new Vector2(1f, 1f), new Vector3(transform.position.x - _uiWindowWorldMap.containerIcon.cellSize.x / 2f,
            //     transform.position.y + _uiWindowWorldMap.containerIcon.cellSize.y / 2f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _uiWindowWorldMapInfo.Show(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }
        public Vector3 GetIconPosition() => iconPosition;
    }
}