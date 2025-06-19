using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 팝업창 매니저
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        // 인트로 씬의 ErrorManager 에서 NormalButtons 타입을 사용하고 있다 
        public enum Type
        {
            None,
            Default // 메시지, 확인, 취소 버튼 있는 타입
        }
        [SerializeField] private GameObject[] popupTypePrefabs;
        [SerializeField] private Transform canvasPopup; // 팝업이 들어갈 canvas
        [SerializeField] private GameObject elementRewardItem;
    
        private readonly Queue<DefaultPopup> popupQueue = new Queue<DefaultPopup>();
        private DefaultPopup currentDefaultPopup;
        
        /// <summary>
        /// 공통 팝업 생성 메서드
        /// </summary>
        /// <param name="popupMetadata"></param>
        private void ShowPopupWithMetadata(PopupMetadata popupMetadata)
        {
            GameObject prefab = GetPopupPrefab(popupMetadata.PopupType);
            if (prefab == null)
            {
                GcLogger.LogError($"팝업 prefab이 없습니다. type: {popupMetadata.PopupType}");
                return;
            }

            DefaultPopup newPopup = Instantiate(prefab, canvasPopup).GetComponent<DefaultPopup>();
            if (newPopup == null)
            {
                GcLogger.LogError("팝업을 생성할 수 없습니다.");
                return;
            }

            newPopup.Initialize(popupMetadata);

            if (popupMetadata.ForceShow)
            {
                newPopup.ShowPopup();
            }
            else
            {
                popupQueue.Enqueue(newPopup);
                ShowNextPopup();
            }
        }
        /// <summary>
        /// 단순 팝업 메시지
        /// </summary>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        public void ShowPopupOnlyMessage(string message, params object[] parameters)
        {
            ShowPopupWithMetadata(new PopupMetadata
            {
                Message = string.Format(message, parameters),
                ShowConfirmButton = true,
                ShowCancelButton = false,
                PopupType = Type.Default
            });
        }
        /// <summary>
        /// 경고 팝업
        /// </summary>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        public void ShowPopupWarning(string message, params object[] parameters)
        {
            ShowPopupWithMetadata(new PopupMetadata
            {
                Message = string.Format(message, parameters),
                ShowConfirmButton = true,
                ShowCancelButton = false,
                PopupType = Type.Default,
                MessageColor = Color.yellow,
                Title = "시스템 안내"
            });
        }
        /// <summary>
        /// 에러 팝업
        /// </summary>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        public void ShowPopupError(string message, params object[] parameters)
        {
            ShowPopupWithMetadata(new PopupMetadata
            {
                Message = string.Format(message, parameters),
                ShowConfirmButton = true,
                ShowCancelButton = false,
                PopupType = Type.Default,
                MessageColor = Color.red,
                Title = "시스템 안내"
            });
        }
        /// <summary>
        /// 일반적인 팝업 생성
        /// </summary>
        /// <param name="popupMetadata"></param>
        public void ShowPopup(PopupMetadata popupMetadata)
        {
            ShowPopupWithMetadata(popupMetadata);
        }
        /// <summary>
        /// 다음 팝업 표시 로직 개선
        /// </summary>
        private void ShowNextPopup()
        {
            if (currentDefaultPopup != null)
            {
                currentDefaultPopup.ClosePopup();
                currentDefaultPopup = null;
            }

            if (popupQueue.Count == 0) return;

            currentDefaultPopup = popupQueue.Dequeue();
            currentDefaultPopup.ShowPopup();

            if (currentDefaultPopup.buttonConfirm != null)
            {
                currentDefaultPopup.buttonConfirm.onClick.RemoveAllListeners();
                currentDefaultPopup.buttonConfirm.onClick.AddListener(OnPopupClosed);
            }
            if (currentDefaultPopup.buttonCancel != null)
            {
                currentDefaultPopup.buttonCancel.onClick.RemoveAllListeners();
                currentDefaultPopup.buttonCancel.onClick.AddListener(OnPopupClosed);
            }
        }
        /// <summary>
        /// 팝업이 닫힐 때 호출
        /// </summary>
        private void OnPopupClosed()
        {
            if (currentDefaultPopup != null)
            {
                RemovePopupListeners(currentDefaultPopup);
                currentDefaultPopup = null;
            }
            ShowNextPopup();
        }
        /// <summary>
        /// 버튼 리스너 제거
        /// </summary>
        /// <param name="popup"></param>
        private void RemovePopupListeners(DefaultPopup popup)
        {
            if (popup.buttonConfirm != null)
            {
                popup.buttonConfirm.onClick.RemoveListener(OnPopupClosed);
            }
            if (popup.buttonCancel != null)
            {
                popup.buttonCancel.onClick.RemoveListener(OnPopupClosed);
            }
        }
        /// <summary>
        /// 예외 처리 추가한 팝업 프리팹 검색
        /// </summary>
        /// <param name="popupType"></param>
        /// <returns></returns>
        private GameObject GetPopupPrefab(Type popupType)
        {
            if ((int)popupType < 0 || (int)popupType >= popupTypePrefabs.Length)
            {
                GcLogger.LogError($"잘못된 PopupType: {popupType}");
                return null;
            }
            return popupTypePrefabs[(int)popupType];
        }
        private void OnDestroy()
        {
            
        }
    }
}