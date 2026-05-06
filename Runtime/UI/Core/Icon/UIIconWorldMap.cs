using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 노드 아이콘에 적용할 시각 상태입니다.
    /// 실제 이동 가능 여부는 월드맵 이동 판정에서 별도로 처리합니다.
    /// </summary>
    public enum WorldMapNodeVisualState
    {
        /// <summary>노드를 월드맵에서 숨기는 상태입니다.</summary>
        Hidden,

        /// <summary>일반 활성 상태입니다.</summary>
        Normal,

        /// <summary>활성화되지 않은 노드를 표시하는 비활성 상태입니다.</summary>
        Inactive,

        /// <summary>표시는 되지만 아직 클리어한 적이 없는 노드 상태입니다.</summary>
        NoClear,
    }

    /// <summary>
    /// 월드맵 노드 하나를 표시하고 선택 입력을 처리하는 아이콘입니다.
    /// </summary>
    public class UIIconWorldMap : UIIcon, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("맵 이름")]
        [SerializeField] private TextMeshProUGUI textName;

        [Tooltip("노드 타입별 데코레이션 스프라이트를 표시할 Image 오브젝트")]
        [SerializeField] private Image imageIconDeco;

        [Tooltip("노드 포인트 상태 Image 오브젝트")]
        [SerializeField] private Image imageIconPoint;

        [Header("클리어 상태")]
        [Tooltip("클리어한 적이 없는 월드맵 노드일 때 아이콘 이미지에 적용할 색상입니다.")]
        [SerializeField] private Color colorNoClear = new Color(1f, 1f, 1f, 0.35f);
        [Tooltip("클리어한 적이 없는 월드맵 노드일 때 표시할 아이콘 이미지입니다.")]
        [SerializeField] private Sprite spriteNoClear;
        [Tooltip("NoInvite Sprite를 표시할 Image입니다. 비어 있으면 기존처럼 ImageIcon에 직접 적용합니다.")]
        [SerializeField] private Image imageNoClear;

        private TableMap _tableMap;
        private StruckTableMap _struckTableMap;
        private WorldMapNodeDefinition _nodeDefinition;
        private WorldMapNodeDefinition _displayNodeDefinition;
        private int _displayMapUid;
        private Sprite _iconSprite;
        private WorldMapNodeDecorationRuntimeData _decorationData = WorldMapNodeDecorationRuntimeData.Empty;
        private Animator _decorationAnimator;
        private bool _isDecorationAnimationPlaying;

        /// <summary>현재 아이콘이 표시하는 월드맵 노드 ID입니다.</summary>
        public string NodeId => _nodeDefinition != null ? _nodeDefinition.NodeId : string.Empty;

        /// <summary>현재 아이콘이 표시하는 월드맵 노드 정의입니다.</summary>
        public WorldMapNodeDefinition NodeDefinition => _nodeDefinition;

        /// <summary>현재 아이콘에 표시 중인 월드맵 노드 정의입니다. 입장 규칙으로 대체되지 않으면 NodeDefinition과 같습니다.</summary>
        public WorldMapNodeDefinition DisplayNodeDefinition => _displayNodeDefinition;

        /// <summary>현재 아이콘에 표시 중인 TableMap UID입니다. 이동 요청 UID와 다를 수 있습니다.</summary>
        public int DisplayMapUid => _displayMapUid > 0 ? _displayMapUid : uid;
        
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
        /// 반복하지 않는 데코레이션 애니메이션이 한 번 재생된 뒤 마지막 상태에서 멈추도록 갱신합니다.
        /// </summary>
        private void Update()
        {
            UpdateDecorationAnimationPlayback();
        }

        /// <summary>
        /// 월드맵 노드 정의를 아이콘에 연결하고 TableMap 표시 정보를 갱신합니다.
        /// </summary>
        /// <param name="nodeDefinition">표시할 월드맵 노드 정의입니다.</param>
        /// <param name="mapData">노드가 참조하는 TableMap 데이터입니다.</param>
        /// <param name="iconSprite">AddressableLoaderWorldMap에서 로드한 노드 아이콘 Sprite입니다.</param>
        /// <param name="inactiveSprite">노드 비활성 상태에서 사용할 override Sprite입니다.</param>
        public void SetWorldMapNode(WorldMapNodeDefinition nodeDefinition, StruckTableMap mapData, Sprite iconSprite = null, Sprite inactiveSprite = null)
        {
            SetWorldMapNode(nodeDefinition, nodeDefinition, mapData, iconSprite, inactiveSprite, WorldMapNodeDecorationRuntimeData.Empty);
        }

        /// <summary>
        /// 월드맵 입장 노드와 실제 표시할 노드 정보를 분리해서 아이콘에 연결합니다.
        /// 입장 요청 UID는 원본 노드를 유지하고, 이름과 이미지는 표시 노드 기준으로 갱신합니다.
        /// </summary>
        /// <param name="nodeDefinition">이동 요청과 그래프 판정에 사용할 원본 월드맵 노드입니다.</param>
        /// <param name="displayNodeDefinition">화면에 표시할 월드맵 노드입니다. null이면 원본 노드를 사용합니다.</param>
        /// <param name="mapData">표시 노드가 참조하는 TableMap 데이터입니다.</param>
        /// <param name="iconSprite">표시 노드에 사용할 Sprite입니다.</param>
        /// <param name="inactiveSprite">비활성 상태에서 사용할 override Sprite입니다.</param>
        public void SetWorldMapNode(
            WorldMapNodeDefinition nodeDefinition,
            WorldMapNodeDefinition displayNodeDefinition,
            StruckTableMap mapData,
            Sprite iconSprite = null,
            Sprite inactiveSprite = null)
        {
            SetWorldMapNode(
                nodeDefinition,
                displayNodeDefinition,
                mapData,
                iconSprite,
                inactiveSprite,
                WorldMapNodeDecorationRuntimeData.Empty);
        }

        /// <summary>
        /// 월드맵 노드와 표시용 노드, 아이콘/비활성/데코레이션 override 정보를 아이콘에 연결합니다.
        /// </summary>
        /// <param name="nodeDefinition">이동 요청과 그래프 판정에 사용할 원본 월드맵 노드입니다.</param>
        /// <param name="displayNodeDefinition">화면에 표시할 월드맵 노드입니다.</param>
        /// <param name="mapData">표시 노드가 참조하는 TableMap 데이터입니다.</param>
        /// <param name="iconSprite">표시 노드에 사용할 Sprite입니다.</param>
        /// <param name="inactiveSprite">비활성 상태에서 사용할 override Sprite입니다.</param>
        /// <param name="decorationData">데코레이션 override 런타임 데이터입니다.</param>
        public void SetWorldMapNode(
            WorldMapNodeDefinition nodeDefinition,
            WorldMapNodeDefinition displayNodeDefinition,
            StruckTableMap mapData,
            Sprite iconSprite,
            Sprite inactiveSprite,
            WorldMapNodeDecorationRuntimeData decorationData)
        {
            _nodeDefinition = nodeDefinition;
            _displayNodeDefinition = displayNodeDefinition ?? nodeDefinition;
            _displayMapUid = _displayNodeDefinition != null && _displayNodeDefinition.MapUid > 0
                ? _displayNodeDefinition.MapUid
                : _nodeDefinition != null
                    ? _nodeDefinition.MapUid
                    : 0;
            _iconSprite = iconSprite;
            _decorationData = decorationData;
            if (_nodeDefinition == null)
            {
                _decorationData = WorldMapNodeDecorationRuntimeData.Empty;
                SetInactiveSpriteOverride(null);
                _displayNodeDefinition = null;
                _displayMapUid = 0;
                ClearIconInfos();
                ApplyNodeDecoration();
                SetPointSprite(null);
                SetWorldMapNodeVisualState(WorldMapNodeVisualState.Normal);
                return;
            }

            _struckTableMap = mapData ?? _tableMap?.GetDataByUid(DisplayMapUid);
            SetInactiveSpriteOverride(inactiveSprite);
            SetInactiveVisualState(false, false);
            ChangeInfoByUid(_nodeDefinition.MapUid, 1, 1);
            ApplyNodeDisplayName();
            ApplyNodeDecoration();
            SetWorldMapNodeVisualState(WorldMapNodeVisualState.Normal);
        }

        /// <summary>
        /// 월드맵 노드의 시각 상태에 맞춰 비활성 또는 미클리어 비주얼을 적용합니다.
        /// 이동 가능 여부는 변경하지 않고 아이콘 표현만 갱신합니다.
        /// </summary>
        /// <param name="visualState">아이콘에 적용할 월드맵 노드 시각 상태입니다.</param>
        public void SetWorldMapNodeVisualState(WorldMapNodeVisualState visualState)
        {
            if (visualState == WorldMapNodeVisualState.Inactive)
            {
                ClearNoInviteVisual();
                SetInactiveVisualState(true, false);
                return;
            }

            SetInactiveVisualState(false, false);
            ApplyNoInviteVisual(visualState == WorldMapNodeVisualState.NoClear);
        }

        /// <summary>
        /// 미클리어 노드 비주얼을 해제하고 기본 아이콘 표현으로 되돌립니다.
        /// 이후 비활성 비주얼을 적용할 때 NoInvite 오브젝트가 함께 남지 않도록 정리합니다.
        /// </summary>
        private void ClearNoInviteVisual()
        {
            SetInactiveVisualState(false, false);
            ApplyNoInviteVisual(false);
        }

        /// <summary>
        /// 미클리어 노드 상태에 맞춰 색상과 전용 스프라이트 표시를 갱신합니다.
        /// 전용 Image가 없으면 기본 아이콘 Image의 스프라이트를 임시로 교체합니다.
        /// </summary>
        /// <param name="show">미클리어 비주얼을 표시하면 true입니다.</param>
        private void ApplyNoInviteVisual(bool show)
        {
            ApplyNoInviteSprite(show);
            if (ImageIcon == null)
            {
                return;
            }

            if (!show)
            {
                return;
            }

            ImageIcon.color = colorNoClear;
            OnSetColorImageIcon(colorNoClear);
        }

        /// <summary>
        /// NoInvite Sprite를 전용 Image 또는 기본 아이콘 Image에 적용합니다.
        /// 숨길 때는 전용 Image를 비활성화하고, 기본 아이콘 복원은 공용 비활성 해제 로직에 맡깁니다.
        /// </summary>
        /// <param name="show">NoInvite Sprite를 표시하면 true입니다.</param>
        private void ApplyNoInviteSprite(bool show)
        {
            if (imageNoClear != null)
            {
                imageNoClear.sprite = show ? spriteNoClear : null;
                imageNoClear.gameObject.SetActive(show && spriteNoClear != null);
                return;
            }

            if (show && spriteNoClear != null && ImageIcon != null)
            {
                ImageIcon.sprite = spriteNoClear;
            }
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
            int displayMapUid = DisplayMapUid > 0 ? DisplayMapUid : iconUid;
            var info = _tableMap != null ? _tableMap.GetDataByUid(displayMapUid) : null;
            if (info == null)
            {
                GcLogger.LogError("월드맵 아이콘에 연결할 TableMap 데이터가 없습니다.");
                return false;
            }

            _struckTableMap = info;
            ApplyNodeDisplayName();
            ApplyNodeDecoration();
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
        /// 월드맵 노드 포인트 상태 이미지를 설정합니다.
        /// </summary>
        /// <param name="sprite">포인트에 표시할 Sprite입니다. null이면 포인트를 숨깁니다.</param>
        public void SetPointSprite(Sprite sprite)
        {
            if (imageIconPoint == null)
            {
                return;
            }

            imageIconPoint.sprite = sprite;
            imageIconPoint.enabled = sprite != null;
            imageIconPoint.gameObject.SetActive(sprite != null);
        }
        
        /// <summary>
        /// 월드맵 노드가 지정한 iconAddress를 아이콘 이미지 경로로 사용합니다.
        /// </summary>
        /// <returns>아이콘 이미지 경로입니다.</returns>
        protected override string GetIconImagePath()
        {
            WorldMapNodeDefinition displayNode = _displayNodeDefinition ?? _nodeDefinition;
            return displayNode != null ? displayNode.IconAddress : string.Empty;
        }

        /// <summary>
        /// AddressableLoaderWorldMap에서 전달받은 Sprite가 있을 때만 아이콘 이미지를 교체합니다.
        /// Sprite가 없으면 프리팹의 기본 이미지를 유지합니다.
        /// </summary>
        protected override void UpdateIconImage()
        {
            if (_iconSprite != null)
            {
                ChangeIconImage(_iconSprite);
            }
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

            WorldMapNodeDefinition displayNode = _displayNodeDefinition ?? _nodeDefinition;
            if (displayNode != null && !string.IsNullOrWhiteSpace(displayNode.TitleOverride))
            {
                textName.text = displayNode.TitleOverride;
                return;
            }

            textName.text = _struckTableMap != null ? _struckTableMap.Name : string.Empty;
        }

        /// <summary>
        /// 월드맵 설정에서 노드 타입별 데코레이션 스프라이트를 찾아 IconDeco에 적용합니다.
        /// </summary>
        private void ApplyNodeDecoration()
        {
            if (imageIconDeco == null)
            {
                return;
            }

            ApplyNodeDecorationOffset(_decorationData.Offset);
            ApplyNodeDecorationSize(_decorationData.Size);
            ApplyNodeDecorationScale(_decorationData.Scale);
            if (!string.IsNullOrEmpty(_decorationData.AnimationName) && _decorationData.AnimatorController != null)
            {
                ApplyAnimatedNodeDecoration(_decorationData);
                return;
            }

            Sprite decoSprite = _decorationData.Sprite != null
                ? _decorationData.Sprite
                : ResolveNodeDecorationSprite();
            ApplyStaticNodeDecoration(decoSprite);
        }

        /// <summary>
        /// 정적 Sprite 기반 노드 데코레이션을 Image Icon Deco에 적용합니다.
        /// </summary>
        /// <param name="decoSprite">표시할 데코레이션 Sprite입니다. null이면 데코레이션을 숨깁니다.</param>
        private void ApplyStaticNodeDecoration(Sprite decoSprite)
        {
            ClearNodeDecorationAnimator();
            imageIconDeco.sprite = decoSprite;
            imageIconDeco.enabled = decoSprite != null;
            imageIconDeco.gameObject.SetActive(decoSprite != null);
        }

        /// <summary>
        /// AnimatorController 기반 노드 데코레이션을 Image Icon Deco에 적용하고 지정한 상태를 재생합니다.
        /// </summary>
        /// <param name="decorationData">적용할 데코레이션 런타임 데이터입니다.</param>
        private void ApplyAnimatedNodeDecoration(WorldMapNodeDecorationRuntimeData decorationData)
        {
            if (!window.gameObject.activeSelf) return;
            Animator animator = GetOrAddNodeDecorationAnimator();
            imageIconDeco.sprite = decorationData.Sprite;
            imageIconDeco.enabled = true;
            imageIconDeco.gameObject.SetActive(true);

            animator.enabled = true;
            animator.speed = 1f;
            animator.runtimeAnimatorController = decorationData.AnimatorController;
            animator.Rebind();
            animator.Update(0f);

            if (!string.IsNullOrWhiteSpace(decorationData.AnimationName))
            {
                animator.Play(decorationData.AnimationName, 0, 0f);
                animator.Update(0f);
            }

            _isDecorationAnimationPlaying = true;
        }

        /// <summary>
        /// 데코레이션 Image 오브젝트에 Animator가 없으면 추가하고, 있으면 재사용합니다.
        /// </summary>
        /// <returns>Image Icon Deco에 연결된 Animator입니다.</returns>
        private Animator GetOrAddNodeDecorationAnimator()
        {
            if (_decorationAnimator != null)
            {
                return _decorationAnimator;
            }

            _decorationAnimator = imageIconDeco.GetComponent<Animator>();
            if (_decorationAnimator == null)
            {
                _decorationAnimator = imageIconDeco.gameObject.AddComponent<Animator>();
            }

            return _decorationAnimator;
        }

        /// <summary>
        /// 정적 데코레이션을 표시할 때 이전 노드에서 사용하던 Animator 상태를 해제합니다.
        /// </summary>
        private void ClearNodeDecorationAnimator()
        {
            if (_decorationAnimator == null)
            {
                return;
            }

            _decorationAnimator.runtimeAnimatorController = null;
            _decorationAnimator.speed = 1f;
            _decorationAnimator.enabled = false;
            _isDecorationAnimationPlaying = false;
        }

        /// <summary>
        /// 데코레이션 위치를 월드맵 아이콘 중앙 기준 오프셋으로 적용합니다.
        /// </summary>
        /// <param name="offset">아이콘 중앙 기준 위치 오프셋입니다.</param>
        private void ApplyNodeDecorationOffset(Vector2 offset)
        {
            RectTransform rectTransform = imageIconDeco.rectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = offset;
        }
        
        private void ApplyNodeDecorationSize(Vector2 size)
        {
            RectTransform rectTransform = imageIconDeco.rectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.sizeDelta = size;
        }
        
        private void ApplyNodeDecorationScale(Vector2 scale)
        {
            RectTransform rectTransform = imageIconDeco.rectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.localScale = scale;
        }

        /// <summary>
        /// Loop가 꺼진 데코레이션 애니메이션을 첫 재생이 끝난 시점에 정지합니다.
        /// </summary>
        private void UpdateDecorationAnimationPlayback()
        {
            if (!_isDecorationAnimationPlaying ||
                _decorationData.Loop ||
                _decorationAnimator == null ||
                !_decorationAnimator.enabled ||
                _decorationAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            if (_decorationAnimator.IsInTransition(0))
            {
                return;
            }

            AnimatorStateInfo stateInfo = _decorationAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime < 1f)
            {
                return;
            }

            _decorationAnimator.speed = 0f;
            _isDecorationAnimationPlaying = false;
        }

        /// <summary>
        /// 현재 노드 타입에 연결된 데코레이션 스프라이트를 설정 로더에서 조회합니다.
        /// </summary>
        /// <returns>노드 타입에 맞는 데코레이션 스프라이트입니다. 설정 또는 스프라이트가 없으면 null을 반환합니다.</returns>
        private Sprite ResolveNodeDecorationSprite()
        {
            WorldMapNodeDefinition displayNode = _displayNodeDefinition ?? _nodeDefinition;
            if (displayNode == null)
            {
                return null;
            }

            GGemCoWorldMapSettings worldMapSettings = null;
            if (AddressableLoaderSettings.Instance != null)
            {
                worldMapSettings = AddressableLoaderSettings.Instance.worldMapSettings;
            }

            if (worldMapSettings == null && AddressableLoaderSettingsRegist.Instance != null)
            {
                worldMapSettings = AddressableLoaderSettingsRegist.Instance.worldMapSettings;
            }

            return worldMapSettings != null
                ? worldMapSettings.GetDecorationSprite(displayNode.NodeType)
                : null;
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
        
        protected override void OnSetColorImageIcon(Color color)
        {
            if (!imageIconDeco) return;
            imageIconDeco.color = color;
        }
    }
}
