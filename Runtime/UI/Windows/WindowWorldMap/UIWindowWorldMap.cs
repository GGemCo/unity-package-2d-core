using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 정의 데이터를 UI 노드와 연결선으로 표시하는 월드맵 윈도우입니다.
    /// </summary>
    public partial class UIWindowWorldMap : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("월드맵 노드와 연결선이 들어갈 최상위 오브젝트")]
        public GameObject containerWorldMap;

        [Tooltip("연결선이 들어갈 레이어입니다. 비어 있으면 런타임에 자동 생성합니다.")]
        [SerializeField] private RectTransform containerLineLayer;

        [Tooltip("노드 슬롯과 아이콘이 들어갈 레이어입니다. 비어 있으면 런타임에 자동 생성합니다.")]
        [SerializeField] private RectTransform containerNodeLayer;

        [Tooltip("월드맵 배경을 표시할 Image입니다. 비어 있으면 containerWorldMap의 Image를 사용합니다.")]
        [SerializeField] private Image imageBackground;

        [Tooltip("월드맵을 표시하는 viewport입니다. 비어 있으면 containerWorldMap의 부모 RectTransform을 사용합니다.")]
        [SerializeField] private RectTransform viewportWorldMap;

        [Header("월드맵 표시 정책")]
        [Tooltip("월드맵 노드, 연결선, 선택, 이동 가능 여부를 결정하는 표시 정책입니다.")]
        [SerializeField] private WorldMapWindowPresentationOptions presentationOptions =
            WorldMapWindowPresentationOptions.CreateDefault();
        
        [Tooltip("이동하기 버튼")]
        [SerializeField] private Button buttonWarp;

        [Tooltip("일반 연결선 색상")]
        [SerializeField] private Color edgeColorNormal = new Color(0.72f, 0.72f, 0.72f, 1f);

        [Tooltip("잠긴 연결선 색상")]
        [SerializeField] private Color edgeColorLocked = new Color(0.95f, 0.62f, 0.24f, 1f);

        [Tooltip("비밀 연결선 색상")]
        [SerializeField] private Color edgeColorSecret = new Color(0.62f, 0.46f, 0.92f, 1f);

        [Tooltip("선택된 노드와 연결된 연결선 강조 색상")]
        [SerializeField] private Color edgeColorHighlighted = new Color(0.3f, 0.75f, 1f, 1f);

        [Tooltip("연결선 두께")]
        [SerializeField] private float edgeThickness = 6f;

        [Header("연결선 이미지")]
        [Tooltip("일반 연결선에 사용할 기본 스프라이트입니다. 비어 있으면 기존 색상 라인으로 표시합니다.")]
        [SerializeField] private Sprite edgeSpriteNormal;

        [Tooltip("잠김 연결선에 사용할 기본 스프라이트입니다. 비어 있으면 일반 연결선 스프라이트를 사용합니다.")]
        [SerializeField] private Sprite edgeSpriteLocked;

        [Tooltip("비밀 연결선에 사용할 기본 스프라이트입니다. 비어 있으면 일반 연결선 스프라이트를 사용합니다.")]
        [SerializeField] private Sprite edgeSpriteSecret;

        [Tooltip("선택된 노드와 연결된 선에 사용할 하이라이트 스프라이트입니다. 비어 있으면 일반 스프라이트에 하이라이트 색상을 적용합니다.")]
        [SerializeField] private Sprite edgeSpriteHighlighted;

        [Tooltip("연결선 스프라이트를 그리는 방식입니다.")]
        [SerializeField] private WorldMapEdgeSpriteDrawMode edgeSpriteDrawMode = WorldMapEdgeSpriteDrawMode.Sliced;

        [Header("포인트 상태별 이미지")]
        [Tooltip("현재 플레이어가 있는 맵일 때 보여줄 이미지")]
        [SerializeField] private Sprite spritePointCurrentMap;
        [Tooltip("플레이어가 이동 가능한 맵일 때")]
        [SerializeField] private Sprite spritePointMovePossible;
        [Tooltip("플레이어가 이동 불가능한 맵일 때")]
        [SerializeField] private Sprite spritePointMoveImPossible;

        [Header("선택 노드 중앙 이동")]
        [Tooltip("선택한 월드맵 아이콘을 viewport 중앙으로 이동시키는 옵션입니다.")]
        [SerializeField] private WorldMapNodeCenteringOptions selectedNodeCenteringOptions = new WorldMapNodeCenteringOptions();

        [Header("월드맵 선택 이미지")]
        [Tooltip("현재 플레이어가 있는 맵을 선택했을 때 사용할 선택 이미지 Prefab입니다.")]
        [SerializeField] private GameObject prefabSelectedCurrentMap;

        [Tooltip("현재 플레이어가 있는 맵을 선택했을 때 사용할 선택 이미지 Sprite입니다.")]
        [SerializeField] private Sprite spriteSelectedCurrentMap;

        [Tooltip("현재 플레이어가 있는 맵을 선택했을 때 선택 아이콘 이미지 애니메이션을 별도로 지정할지 여부입니다.")]
        [SerializeField] private bool overrideSelectedCurrentMapAnimation;

        [Tooltip("현재 플레이어가 있는 맵을 선택했을 때 사용할 선택 아이콘 이미지 애니메이션 설정입니다.")]
        [SerializeField] private UISelectedIconAnimationSettings selectedCurrentMapAnimation =
            new UISelectedIconAnimationSettings();

        private readonly Dictionary<string, UIIconWorldMap> _nodeIconById = new Dictionary<string, UIIconWorldMap>();
        private readonly Dictionary<string, RectTransform> _nodeRectById = new Dictionary<string, RectTransform>();
        private readonly List<WorldMapLineRenderer> _edgeLines = new List<WorldMapLineRenderer>();

        private UIIconWorldMap _selectedUIIconWorldMap;
        private MapManager _mapManager;
        private TableMap _tableMap;
        private WorldMapDefinition _worldMapDefinition;
        private string _requestedBackgroundAddress;
        private WorldMapDragController _dragController;
        private int _selectionCenteringRequestId;

        /// <summary>현재 윈도우가 표시 중인 월드맵 정의입니다.</summary>
        public WorldMapDefinition WorldMapDefinition => _worldMapDefinition;
    }
}
