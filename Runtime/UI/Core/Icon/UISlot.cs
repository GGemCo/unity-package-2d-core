using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이콘 슬롯
    /// </summary>
    public class UISlot : MonoBehaviour
    {
        [Header(UIWindowConstants.TitleHeaderCommon)]
        [Tooltip("필터링 된 슬롯인지 여부")]
        public bool isFiltering;
        [Tooltip("Canvas Group 컴포넌트 사용 여부")]
        public bool useCanvasGroup;
        
        [Header("색상")]
        [Tooltip("일반 상태 색상")]
        [SerializeField] private Color colorNormal = Color.white;
        [Tooltip("선택되었을 때 색상")]
        [SerializeField] private Color colorSelected = Color.blue;
        
        [Header("장착")]
        [Tooltip("장착되었을 때 색상")]
        [SerializeField] private Color colorEquip = Color.yellow;
        [Tooltip("현재 선택 문맥에서 이 슬롯의 아이템이 장착 중임을 표시할 이미지")]
        [SerializeField] private Image imageEquipped;
        [Tooltip("현재 선택 문맥에서 이 슬롯의 아이템이 장착 중임을 표시할 텍스트")]
        [SerializeField] private TextMeshProUGUI textEquipped;
        [Tooltip("장착 되었을 때, 슬롯 배경 이미지 숨기 여부")]
        [SerializeField] private bool isDisableBackgroundImageOnEquip = false;

        [Header("비활성")]
        [Tooltip("비활성 상태일 때 슬롯 배경에 적용할 색상입니다. 알파 값으로 투명도를 함께 지정합니다.")]
        [SerializeField] private Color colorInactive = new Color(1f, 1f, 1f, 0.35f);
        [Tooltip("비활성 상태일 때 표시할 슬롯 오버레이 이미지입니다.")]
        [SerializeField] private Image imageInactive;

        private UIWindow _window;
        private UIWindowConstants.WindowUid _windowUid;
        public int Index { get; private set; }

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Image _imageSlot;
        private bool _isSelected;
        private bool _isEquipped;
        private bool _isInactive;

        /// <summary>
        /// 현재 슬롯이 비활성 상태인지 반환합니다.
        /// 비활성 슬롯은 아이콘을 받을 수 없고 선택/장착 표시보다 비활성 표시를 우선합니다.
        /// </summary>
        public bool IsInactive => _isInactive;

        /// <summary>
        /// prefab 생성 후 호출되는 함수
        /// </summary>
        /// <param name="window"></param>
        /// <param name="windowUid"></param>
        /// <param name="slotIndex"></param>
        /// <param name="slotSize"></param>
        public void Initialize(UIWindow window, UIWindowConstants.WindowUid windowUid, int slotIndex, Vector2 slotSize)
        {
            _window = window;
            _windowUid = windowUid;
            Index = slotIndex;
            _isSelected = false;
            _isEquipped = false;
            _isInactive = false;
            
            if (_imageSlot == null)
                _imageSlot = GetComponent<Image>();
            
            _rectTransform = GetComponent<RectTransform>();
            if (useCanvasGroup)
            {
                // 커스텀 슬롯 프리팹에 CanvasGroup이 이미 있으면 재사용하여 중복 컴포넌트 생성을 방지합니다.
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            ChangeSlotImageSize(slotSize);
            SetEquippedState(false);
        }
        /// <summary>
        /// 슬롯 이미지 사이즈 변경하기
        /// </summary>
        /// <param name="size"></param>
        private void ChangeSlotImageSize(Vector2 size)
        {
            _rectTransform.sizeDelta = size;
        }

        public void SetPosition(Vector3 position)
        {
            transform.localPosition = position;
        }

        /// <summary>
        /// 슬롯의 CanvasGroup에 투명도를 적용합니다.
        /// <see cref="useCanvasGroup"/>이 활성화되어 초기화된 슬롯에서만 적용됩니다.
        /// </summary>
        /// <param name="alpha">슬롯과 자식 UI에 적용할 투명도입니다.</param>
        public void SetAlpha(float alpha)
        {
            if (!useCanvasGroup || _canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        public void SetColor(Color color)
        {
            if (!_imageSlot) return;
            _imageSlot.color = color;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected && !_isInactive;
            RefreshSlotVisualState();
        }

        /// <summary>
        /// 슬롯의 비활성 상태를 설정하고 시각 상태를 갱신합니다.
        /// 비활성 상태에서는 선택/장착 표현을 해제하고 비활성 오버레이를 표시합니다.
        /// </summary>
        /// <param name="inactive">비활성 여부입니다.</param>
        public void SetInactiveState(bool inactive)
        {
            _isInactive = inactive;
            if (_isInactive)
            {
                _isSelected = false;
                _isEquipped = false;
            }

            if (imageInactive != null)
            {
                imageInactive.gameObject.SetActive(_isInactive);
            }

            RefreshSlotVisualState();
        }

        /// <summary>
        /// 이 슬롯에 있는 아이템이 현재 선택 문맥에서 이미 사용 중인지 표시합니다.
        /// 아이콘 잠금과 달리 슬롯의 입력 가능 상태는 바꾸지 않고 표시 오브젝트만 토글합니다.
        /// </summary>
        public void SetEquippedState(bool equipped)
        {
            _isEquipped = equipped && !_isInactive;

            if (imageEquipped != null)
            {
                imageEquipped.gameObject.SetActive(_isEquipped);
                imageEquipped.color = _isEquipped ? colorEquip : colorNormal;
            }

            RefreshSlotVisualState();

            if (textEquipped != null)
            {
                textEquipped.gameObject.SetActive(_isEquipped);
            }
        }

        /// <summary>
        /// 슬롯 배경 표시를 현재 상태 우선 순위에 맞게 갱신합니다.
        /// 우선 순위: 장착 > 선택 > 일반
        /// </summary>
        private void RefreshSlotVisualState()
        {
            if (!_imageSlot) return;

            // 장착 상태에서 배경 이미지를 숨기는 옵션은 색상 계산과 별도로 처리합니다.
            _imageSlot.enabled = !(isDisableBackgroundImageOnEquip && _isEquipped);

            if (_isInactive)
            {
                _imageSlot.enabled = true;
                SetColor(colorInactive);
                return;
            }

            if (_isEquipped)
            {
                SetColor(colorEquip);
                return;
            }

            SetColor(_isSelected ? colorSelected : colorNormal);
        }
    }
}
