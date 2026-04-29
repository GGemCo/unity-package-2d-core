using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 노드 하나를 표시하고 선택 입력을 처리하는 아이콘입니다.
    /// </summary>
    public class UIIconWorldMap : UIIcon, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("맵 이름")]
        [SerializeField] private TextMeshProUGUI textName;

        [Tooltip("일반 상태 아이콘 색상")]
        [SerializeField] private Color colorNormal = Color.white;

        [Tooltip("선택 상태 아이콘 색상")]
        [SerializeField] private Color colorSelected = Color.blue;
        
        private TableMap _tableMap;
        private StruckTableMap _struckTableMap;
        private WorldMapNodeDefinition _nodeDefinition;

        /// <summary>현재 아이콘이 표시하는 월드맵 노드 ID입니다.</summary>
        public string NodeId => _nodeDefinition != null ? _nodeDefinition.NodeId : string.Empty;

        /// <summary>현재 아이콘이 표시하는 월드맵 노드 정의입니다.</summary>
        public WorldMapNodeDefinition NodeDefinition => _nodeDefinition;
        
        /// <summary>
        /// 아이콘 초기화 후 월드맵 전용 의존성을 연결합니다.
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            IconType = IconConstants.Type.WorldMap;
            _tableMap ??= TableLoaderManager.Instance != null ? TableLoaderManager.Instance.TableMap : null;
            DisableIconDragHandler();
        }

        /// <summary>
        /// 월드맵 노드 정의를 아이콘에 연결하고 TableMap 표시 정보를 갱신합니다.
        /// </summary>
        /// <param name="nodeDefinition">표시할 월드맵 노드 정의입니다.</param>
        /// <param name="mapData">노드가 참조하는 TableMap 데이터입니다.</param>
        public void SetWorldMapNode(WorldMapNodeDefinition nodeDefinition, StruckTableMap mapData)
        {
            _nodeDefinition = nodeDefinition;
            if (_nodeDefinition == null)
            {
                ClearIconInfos();
                return;
            }

            _struckTableMap = mapData ?? _tableMap?.GetDataByUid(_nodeDefinition.MapUid);
            ChangeInfoByUid(_nodeDefinition.MapUid, 1, 1);
            ApplyNodeDisplayName();
        }
        
        /// <summary>
        /// mapUid를 기준으로 TableMap 정보를 연결하고 월드맵 아이콘 표시를 갱신합니다.
        /// </summary>
        /// <param name="iconUid">TableMap UID입니다.</param>
        /// <param name="iconCount">아이콘 개수입니다.</param>
        /// <param name="iconLevel">아이콘 레벨입니다.</param>
        /// <param name="iconIsLearn">학습 여부입니다.</param>
        /// <param name="remainCoolTime">남은 쿨타임입니다.</param>
        /// <param name="iconInstanceId">아이콘 인스턴스 ID입니다.</param>
        /// <param name="iconType">아이콘 타입입니다.</param>
        /// <returns>정보 변경에 성공하면 true입니다.</returns>
        public override bool ChangeInfoByUid(int iconUid, int iconCount = 0, int iconLevel = 0,
            bool iconIsLearn = false, int remainCoolTime = 0, long iconInstanceId = 0,
            IconConstants.Type iconType = IconConstants.Type.None)
        {
            if (!base.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, remainCoolTime, iconInstanceId,
                    iconType)) return false;

            _tableMap ??= TableLoaderManager.Instance != null ? TableLoaderManager.Instance.TableMap : null;
            var info = _tableMap != null ? _tableMap.GetDataByUid(iconUid) : null;
            if (info == null)
            {
                GcLogger.LogError("월드맵 아이콘에 연결할 TableMap 데이터가 없습니다.");
                return false;
            }

            _struckTableMap = info;
            ApplyNodeDisplayName();
            UpdateInfo();
            return true;
        }

        /// <summary>
        /// 포인터 진입 시 월드맵 정보 팝업을 표시할 수 있는 확장 지점입니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 포인터 이탈 시 월드맵 정보 팝업을 닫을 수 있는 확장 지점입니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 월드맵 아이콘은 기본 선택 처리와 함께 색상 강조를 추가로 반영합니다.
        /// </summary>
        /// <param name="value">선택 여부입니다.</param>
        public override void SetSelected(bool value)
        {
            base.SetSelected(value);

            if (ImageIcon != null)
            {
                ImageIcon.color = value ? colorSelected : colorNormal;
            }
        }

        /// <summary>
        /// 클릭한 월드맵 노드를 부모 윈도우의 선택 아이콘으로 지정합니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (GcLogger.IsNull(_struckTableMap, "map 데이터가 없습니다.")) return;
            if (GcLogger.IsNull(window, "아이콘에 연결된 윈도우가 없습니다.")) return;
            if (GcLogger.IsZero(_struckTableMap.Uid, "map uid 값이 없습니다.")) return;

            window.SetSelectedIcon(index);
        }
        
        /// <summary>
        /// 월드맵 노드가 지정한 iconAddress를 아이콘 이미지 경로로 사용합니다.
        /// </summary>
        /// <returns>아이콘 이미지 경로입니다.</returns>
        protected override string GetIconImagePath()
        {
            return _nodeDefinition != null ? _nodeDefinition.IconAddress : string.Empty;
        }

        /// <summary>
        /// 월드맵 노드에 iconAddress가 있을 때만 아이콘 이미지를 교체합니다.
        /// iconAddress가 비어 있으면 프리팹의 기본 이미지를 유지합니다.
        /// </summary>
        protected override void UpdateIconImage()
        {
            if (_nodeDefinition == null || string.IsNullOrWhiteSpace(_nodeDefinition.IconAddress))
            {
                return;
            }

            base.UpdateIconImage();
        }

        /// <summary>
        /// 월드맵 노드 override 제목 또는 TableMap 이름을 텍스트에 반영합니다.
        /// </summary>
        private void ApplyNodeDisplayName()
        {
            if (textName == null)
            {
                return;
            }

            if (_nodeDefinition != null && !string.IsNullOrWhiteSpace(_nodeDefinition.TitleOverride))
            {
                textName.text = _nodeDefinition.TitleOverride;
                return;
            }

            textName.text = _struckTableMap != null ? _struckTableMap.Name : string.Empty;
        }

        /// <summary>
        /// 월드맵 아이콘은 아이템 드래그 대상이 아니므로 공용 아이콘 드래그 핸들러를 비활성화합니다.
        /// 부모 월드맵 컨테이너가 드래그 이벤트를 받을 수 있도록 하기 위한 처리입니다.
        /// </summary>
        private void DisableIconDragHandler()
        {
            UIDragHandler dragHandler = GetComponent<UIDragHandler>();
            if (dragHandler != null)
            {
                dragHandler.enabled = false;
            }
        }
    }
}
