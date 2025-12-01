using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // 드래그 하기전 위치
        private Vector3 _originalPosition;
        // 드래그 가능 여부
        private bool _isPossibleDrag = true;
        private GameObject _canvas;
        
        private List<Graphic> _graphics = new List<Graphic>();

        private void Awake()
        {
            // 하위 오브젝트 Graphic 컴포넌트 모두 수집
            GetComponentsInChildren(true, _graphics);
        }
        protected virtual void Start()
        {
            if (SceneGame.Instance && SceneGame.Instance.canvasUI)
                _canvas = SceneGame.Instance.canvasUI.gameObject;
        }


        public void SetIsPossibleDrag(bool set) => _isPossibleDrag = set;
        public bool GetIsPossibleDrag() => _isPossibleDrag;
        /// <summary>
        /// 드래그 시작
        /// </summary>
        /// <param name="eventData"></param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isPossibleDrag) return;

            transform.SetParent(_canvas.transform);
            SetRaycastTargets(false);

            _originalPosition = transform.position;
        }
        /// <summary>
        /// 드래그 중일때 
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isPossibleDrag) return;
            
#if GGEMCO_USE_OLD_INPUT
            transform.position = Input.mousePosition;
#elif GGEMCO_USE_NEW_INPUT
            transform.position= Mouse.current.position.ReadValue();
#endif
        }
        /// <summary>
        /// 드래그 끝났을때
        /// </summary>
        /// <param name="eventData"></param>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isPossibleDrag) return;
            SetRaycastTargets(true);

            GameObject droppedIcon = eventData.pointerDrag;
            UIIcon droppedUiIcon = droppedIcon.GetComponent<UIIcon>();
            GameObject targetIcon = eventData.pointerEnter;
            
            if (droppedIcon != null)
            {
                // 윈도우 밖에 드래그 앤 드랍했을때  
                if (targetIcon == null)
                {
                    droppedUiIcon.window.OnEndDragOutWindow(eventData, droppedIcon, targetIcon, _originalPosition);
                    return;
                }
                UIIcon targetUiIcon = targetIcon.GetComponentInParent<UIIcon>();
                if (targetUiIcon != null && targetUiIcon.window != null)
                {
                    targetUiIcon.window.OnEndDragInIcon(droppedIcon, targetIcon);
                    return;
                }
                // 아이콘이 아니고, 윈도우에 드랍했을때 처리
                var targetWindow = targetIcon.GetComponentInParent<UIWindow>();
                if (targetWindow != null)
                {
                    targetWindow.OnEndDragInWindow(droppedIcon);
                    return;
                }
            }
            GameObject targetSlot = droppedUiIcon.window.slots[droppedUiIcon.slotIndex];
            droppedIcon.transform.SetParent(targetSlot.transform);
            droppedIcon.transform.position = _originalPosition;
        }
        public Vector3 GetOriginalPosition() => _originalPosition;

        private void SetRaycastTargets(bool enable)
        {
            var image = GetComponent<Image>();
            if (image != null) image.raycastTarget = enable;
            
            foreach (var g in _graphics)
            {
                g.raycastTarget = enable;
            }
        }
    }
}