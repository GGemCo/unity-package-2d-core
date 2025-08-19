using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIPanelOptionBase : MonoBehaviour
    {
        [Header(UIWindowConstants.TitleHeaderCommon)]
        [Tooltip("변경한 내용 적용 버튼")]
        [SerializeField] protected Button buttonConfirm;
        [Tooltip("변경한 내용 취소 버튼")]
        [SerializeField] protected Button buttonCancel;
        [Tooltip("디폴트 값으로 초기화 버튼")]
        [SerializeField] protected Button buttonReset;
        // 변경한 값이 있는지 체크
        protected bool isChanged;
        public UIWindowOption uiWindowOption;
        protected SoundManager soundManager;
        protected PopupManager popupManager;

        protected virtual void Awake()
        {
            isChanged = false;
            buttonConfirm?.onClick.AddListener(OnClickConfirm);
            buttonCancel?.onClick.AddListener(OnClickCancel);
            buttonReset?.onClick.AddListener(OnClickReset);
            
            SetButtonInteractable(false);
        }
        protected virtual void OnDestroy()
        {
            buttonConfirm?.onClick.RemoveAllListeners();
            buttonCancel?.onClick.RemoveAllListeners();
            buttonReset?.onClick.RemoveAllListeners();
        }

        protected virtual void OnClickReset()
        {
        }

        protected virtual void OnClickCancel()
        {
        }

        protected virtual void OnClickConfirm()
        {
        }

        protected void SetButtonInteractable(bool isInteractable)
        {
            if (buttonConfirm)
            {
                buttonConfirm.interactable = isInteractable;
            }

            if (buttonCancel)
            {
                buttonCancel.interactable = isInteractable;
            }
        }
        /// <summary>
        /// 변경한 값이 있을 경우 
        /// </summary>
        /// <param name="value"></param>
        public void SetIsChange(bool value)
        {
            isChanged = value;
            if (value)
            {
                if (buttonConfirm != null)
                {
                    buttonConfirm.interactable = true;
                }
                if (buttonCancel != null)
                {
                    buttonCancel.interactable = true;
                }
            }
            else
            {
                SetButtonInteractable(false);
            }
        }

        public virtual bool Show(bool show)
        {
            gameObject.SetActive(show);
            return true;
        }

        public bool IsChange()
        {
            return isChanged;
        }

        public void SetUIWindowOption(UIWindowOption puiWindowOption)
        {
            uiWindowOption = puiWindowOption;
            popupManager = uiWindowOption.popupManager;
            soundManager = uiWindowOption.soundManager;
        }
    }
}