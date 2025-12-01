using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// Text 입력을 받아서 처리하는 팝업창
    /// </summary>
    public class UIWindowInputField : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("타이틀")]
        public TMP_Text textTitle;
        [Tooltip("입력받는 InputField")]
        public TMP_InputField inputField;

        [Tooltip("나누기 버튼")]
        public Button buttonConfirm;
        [Tooltip("취소 버트")]
        public Button buttonCancel;
        
        private Action<string> _onConfirm;
        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.ItemSplit;
            base.Awake();
            buttonConfirm?.onClick.AddListener(OnClickConfirm);
            buttonCancel?.onClick.AddListener(OnClickCancel);
            _onConfirm = null;
        }

        private void OnDestroy()
        {
            buttonConfirm?.onClick.RemoveAllListeners();
            buttonCancel?.onClick.RemoveAllListeners();
        }

        public void UpdateInfo(string titleName, Action<string> onConfirm)
        {
            // 순서 중요, 활성화를 먼저 해야 textTitle을 수정할 수 있다.
            Show(true);
            if (textTitle != null)
            {
                textTitle.text = titleName;
            }

            inputField.text = string.Empty;
            _onConfirm = onConfirm;
        }
        /// <summary>
        /// 아이템 나누기
        /// </summary>
        private void OnClickConfirm()
        {
            if (string.IsNullOrEmpty(inputField.text)) return;
            
            _onConfirm?.Invoke(inputField.text);
            Show(false);
            _onConfirm = null;
        }

        private void OnClickCancel()
        {
            Show(false);
        }
    }
}