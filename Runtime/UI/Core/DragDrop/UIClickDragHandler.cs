using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 클릭으로 드래그 시작/종료를 처리하는 핸들러
    /// - 같은 GameObject에 UIIcon 이 있어야 한다.
    /// - 첫 클릭: 드래그 시작 (아이콘이 마우스를 따라다님)
    /// - 다시 클릭: 드랍 처리 (UIDragHandler.OnEndDrag 와 동일한 로직)
    /// </summary>
    [RequireComponent(typeof(UIIcon))]
    public class UIClickDragHandler : MonoBehaviour
    {
        private bool _isClickDragging = false;
        private Vector3 _clickDragOriginalPosition;

        private UIIcon _uiIcon;
        private GameObject _canvas;

        // 외부에서 켜고 끌 수 있도록
        private bool _enableClickDrag = true;
        private readonly List<Graphic> _graphics = new List<Graphic>();

        private void Awake()
        {
            _uiIcon = GetComponent<UIIcon>();
            // 하위 오브젝트 Graphic 컴포넌트 모두 수집
            GetComponentsInChildren(true, _graphics);
        }

        private void Start()
        {
            if (SceneGame.Instance && SceneGame.Instance.canvasUI)
                _canvas = SceneGame.Instance.canvasUI.gameObject;
        }

        private void Update()
        {
            if (!_isClickDragging) return;

#if GGEMCO_USE_OLD_INPUT
            transform.position = Input.mousePosition;
#elif GGEMCO_USE_NEW_INPUT
            transform.position = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
            transform.position = Input.mousePosition;
#endif
        }

        private void OnDisable()
        {
            // 비활성화 시 드래그 중이면 원상 복귀
            if (_isClickDragging)
            {
                _isClickDragging = false;
                ReturnToOriginalSlot();
                // UIDragHandler 다시 활성화
                _uiIcon.SetDrag(true);
            }
        }

        /// <summary>
        /// 외부에서 호출: 클릭 1번 → 시작, 다시 클릭 → 종료/드랍
        /// </summary>
        public void ToggleClickDrag()
        {
            if (!_enableClickDrag) return;

            if (_isClickDragging)
                EndClickDrag();
            else
                BeginClickDrag();
        }

        public void SetEnableClickDrag(bool enable)
        {
            _enableClickDrag = enable;
            if (!enable && _isClickDragging)
            {
                // 끄는 순간 드래그 중이면 즉시 정리
                EndClickDrag();
            }
        }

        private void SetRaycastTargets(bool enable)
        {
            foreach (var g in _graphics)
            {
                g.raycastTarget = enable;
            }
            var image = GetComponent<Image>();
            // 다시 클릭할 수 있게 최상위 Image 오브젝트의 interaction은 켜준다
            if (image != null) image.raycastTarget = true;
        }
        private void BeginClickDrag()
        {
            if (_canvas == null || _uiIcon == null) return;

            _isClickDragging = true;

            // 시작 위치 저장
            _clickDragOriginalPosition = transform.position;

            // 최상위 Canvas 밑으로 이동해서 위에 보이게
            transform.SetParent(_canvas.transform);
            
            SetRaycastTargets(false);

            // 기존 드래그(UIDragHandler)와 충돌 방지
            _uiIcon.SetDrag(false);
            // 클릭 드래그 종료 후 위치 정리를 위해 추가. GoBackToSlot 함수에서 사용 됨 
            _uiIcon.SetOriginalPosition(transform.position);
        }

        private void EndClickDrag()
        {
            _isClickDragging = false;

            // UIDragHandler 다시 켜기
            _uiIcon.SetDrag(true);

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                ReturnToOriginalSlot();
                return;
            }

            // 마우스 위치로 Raycast
            PointerEventData pointerData = new PointerEventData(eventSystem);
#if GGEMCO_USE_OLD_INPUT
            pointerData.position = Input.mousePosition;
#elif GGEMCO_USE_NEW_INPUT
            pointerData.position = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
            pointerData.position = Input.mousePosition;
#endif

            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);

            GameObject targetIcon = null;
            if (raycastResults.Count > 0)
            {
                // 첫번째는 클릭한 아이콘이므로, 다음으로 interaction 된 오브젝트를 선택한다.
                targetIcon = raycastResults[1].gameObject;
            }

            // 순서 중요
            SetRaycastTargets(true);

            GameObject droppedIcon = gameObject;
            UIIcon droppedUiIcon = _uiIcon;

            if (droppedIcon != null && droppedUiIcon != null)
            {
                // UIDragHandler.OnEndDrag 와 동일한 흐름

                // 1) 윈도우 밖으로 드랍
                if (targetIcon == null)
                {
                    droppedUiIcon.window.OnEndDragOutWindow(
                        pointerData,
                        droppedIcon,
                        targetIcon,
                        _clickDragOriginalPosition);
                    return;
                }

                // 2) 다른 아이콘 위로 드랍
                UIIcon targetUiIcon = targetIcon.GetComponentInParent<UIIcon>();
                if (targetUiIcon != null && targetUiIcon.window != null)
                {
                    targetUiIcon.window.OnEndDragInIcon(droppedIcon, targetIcon);
                    return;
                }

                // 3) 특정 UIWindow 영역에 드랍
                var targetWindow = targetIcon.GetComponentInParent<UIWindow>();
                if (targetWindow != null && droppedUiIcon.windowUid != targetWindow.uid)
                {
                    targetWindow.OnEndDragInWindow(droppedIcon);
                    return;
                }

                // 4) 아무 처리도 안되면 원래 자리로 복귀
                ReturnToOriginalSlot();
            }
            else
            {
                ReturnToOriginalSlot();
            }
        }

        private void ReturnToOriginalSlot()
        {
            if (_uiIcon == null || _uiIcon.window == null) return;

            GameObject targetSlot = _uiIcon.window.slots[_uiIcon.slotIndex];
            transform.SetParent(targetSlot.transform);
            transform.position = _clickDragOriginalPosition;
        }
    }
}