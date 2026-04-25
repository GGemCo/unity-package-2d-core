using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GGemCo2DCore
{
    public class UIIconWorldMap : UIIcon, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("맵 이름")]
        [SerializeField] private TextMeshProUGUI textName;
        
        private TableMap _tableMap;
        private StruckTableMap _struckTableMap;
        private MapManager _mapManager;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            IconType = IconConstants.Type.WorldMap;
            _tableMap ??= TableLoaderManager.Instance.TableMap;
            _mapManager ??= SceneGame.Instance.mapManager;
        }
        
        /// <summary>
        /// 다른 uid 로 변경하기
        /// </summary>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        /// <param name="iconLevel"></param>
        /// <param name="iconIsLearn"></param>
        /// <param name="remainCoolTime"></param>
        /// <param name="iconInstanceId"></param>
        /// <param name="iconType"></param>
        public override bool ChangeInfoByUid(int iconUid, int iconCount = 0, int iconLevel = 0,
            bool iconIsLearn = false, int remainCoolTime = 0, long iconInstanceId = 0,
            IconConstants.Type iconType = IconConstants.Type.None)
        {
            if (!base.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, remainCoolTime, iconInstanceId,
                    iconType)) return false;
            var info = _tableMap.GetDataByUid(iconUid);
            if (info == null)
            {
                GcLogger.LogError("아이콘 테이블에 없는 아이템 입니다.");
                return false;
            }

            _struckTableMap = info;
            
            if (textName != null) textName.text = _struckTableMap.Name;

            UpdateInfo();
            return true;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 월드맵 아이콘은 기본 선택 처리와 함께 색상 강조를 추가로 반영합니다.
        /// </summary>
        /// <param name="value">선택 여부</param>
        public override void SetSelected(bool value)
        {
            base.SetSelected(value);

            if (ImageIcon != null)
            {
                ImageIcon.color = value ? Color.blue : Color.white;
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (GcLogger.IsNull(_struckTableMap, "map 데이터가 없습니다.")) return;
            if (GcLogger.IsNull(window, "아이콘에 연결된 윈도우가 없습니다.")) return;
            if (GcLogger.IsZero(_struckTableMap.Uid, "map uid 값이 없습니다.")) return;

            // 월드맵도 다른 UIIcon과 동일하게 부모 윈도우를 통해 단일 선택 상태를 유지합니다.
            window.SetSelectedIcon(index);
        }
        
        /// <summary>
        /// 아이콘 이미지 경로 가져오기 
        /// </summary>
        /// <returns></returns>
        protected override string GetIconImagePath()
        {
            // return _struckTableMap?.FileName;
            return "";
        }
    }
}
