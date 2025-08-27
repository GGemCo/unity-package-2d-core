using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 옵션 패널 Base 
    /// </summary>
    public abstract class UIPanelOptionBase : MonoBehaviour
    {
        [Header(UIWindowConstants.TitleHeaderCommon)]
        [Tooltip("변경한 내용 적용 버튼")]
        [SerializeField] protected Button buttonConfirm;
        [Tooltip("변경한 내용 취소 버튼")]
        [SerializeField] protected Button buttonCancel;
        [Tooltip("디폴트 값으로 초기화 버튼")]
        [SerializeField] protected Button buttonReset;
        [Tooltip("타이틀로 사용할 Localization Key. GGemCoUIWindowOption String Table에 등록해주세요.")]
        [SerializeField] private string title;
        public string Title
        {
            get => title;
            set => title = value;
        }

        public int PanelIndex { get; set; }

        // 변경한 값이 있는지 체크
        public bool IsDirty { get; protected set; }
        
        protected UIWindowOption uiWindowOption;
        protected SoundManager soundManager;
        protected PopupManager popupManager;

        protected virtual void Awake()
        {
            IsDirty = false;
            buttonConfirm?.onClick.AddListener(OnClickConfirm);
            buttonCancel?.onClick.AddListener(OnClickCancel);
            buttonReset?.onClick.AddListener(OnClickReset);
            
            SetButtonsInteractable(false);
        }
        protected virtual void OnDestroy()
        {
            buttonConfirm?.onClick.RemoveAllListeners();
            buttonCancel?.onClick.RemoveAllListeners();
            buttonReset?.onClick.RemoveAllListeners();
        }

        private void OnClickConfirm()
        {
            if (TryApply()) MarkDirty(false);
        }
        private void OnClickCancel()
        {
            Revert();
            MarkDirty(false);
        }

        private void OnClickReset()
        {
            ResetToDefault();
            MarkDirty(true);
        }

        private void SetButtonsInteractable(bool enable)
        {
            if (buttonConfirm) buttonConfirm.interactable = enable;
            if (buttonCancel)  buttonCancel.interactable  = enable;
        }
        /// <summary>
        /// 변경한 값이 있을 경우 
        /// </summary>
        /// <param name="value"></param>
        public void MarkDirty(bool value)
        {
            IsDirty = value;
            SetButtonsInteractable(value);
        }

        public virtual bool Show(bool show)
        {
            gameObject.SetActive(show);
            if (show)
            {
                RefreshFromModel();
                MarkDirty(false);
            }
            return true;
        }

        public virtual void SetWindowOption(UIWindowOption puiWindowOption)
        {
            uiWindowOption = puiWindowOption;
            popupManager = uiWindowOption.popupManager;
            soundManager = uiWindowOption.soundManager;
        }

        public abstract bool TryApply();
        public abstract void Revert();
        protected abstract void ResetToDefault();
        protected abstract void RefreshFromModel();
    }
}