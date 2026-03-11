using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
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
        public void SetPopupTypePrefabs(GameObject[] prefabs) => popupTypePrefabs = prefabs;
        
        [SerializeField] private Transform canvasPopup; // 팝업이 들어갈 canvas
        public void SetCanvasPopup(Transform value) => canvasPopup = value;

        private readonly Queue<PopupMetadata> _popupQueue = new Queue<PopupMetadata>();
        private DefaultPopup _currentDefaultPopup;
        
        /// <summary>
        /// 공통 팝업 생성 메서드
        /// </summary>
        /// <param name="popupMetadata"></param>
        private void ShowPopupWithMetadata(PopupMetadata popupMetadata)
        {
            if (popupMetadata == null)
            {
                GcLogger.LogError($"팝업 prefab이 없습니다. type: {popupMetadata.PopupType}");
                return;
            }

            if (popupMetadata.ForceShow)
            {
                var popup = CreatePopup(popupMetadata);
                popup?.ShowPopup();
                return;
            }

            _popupQueue.Enqueue(popupMetadata);
            ShowNextPopup();
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
                Title = "System_Info_Title" //시스템 안내 
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
                Title = "System_Info_Title" //시스템 안내 
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
            if (_currentDefaultPopup != null)
            {
                return;
            }

            if (_popupQueue.Count == 0)
            {
                return;
            }

            PopupMetadata nextMetadata = _popupQueue.Dequeue();
            _currentDefaultPopup = CreatePopup(nextMetadata);
            _currentDefaultPopup?.ShowPopup();
        }

        private DefaultPopup CreatePopup(PopupMetadata popupMetadata)
        {
            GameObject prefab = GetPopupPrefab(popupMetadata.PopupType);
            if (prefab == null)
            {
                GcLogger.LogError($"팝업 prefab이 없습니다. type: {popupMetadata.PopupType}");
                return null;
            }

            DefaultPopup newPopup = Instantiate(prefab, canvasPopup).GetComponent<DefaultPopup>();
            if (newPopup == null)
            {
                GcLogger.LogError("팝업을 생성할 수 없습니다.");
                return null;
            }

            newPopup.Initialize(popupMetadata);
            newPopup.Closed += OnPopupClosed;
            return newPopup;
        }
        /// <summary>
        /// 팝업이 닫힐 때 호출
        /// </summary>
        private void OnPopupClosed(DefaultPopup popup)
        {
            if (popup != null)
            {
                popup.Closed -= OnPopupClosed;
            }

            if (_currentDefaultPopup == popup)
            {
                _currentDefaultPopup = null;
            }

            ShowNextPopup();
        }

        private GameObject GetPopupPrefab(Type popupType)
        {
            if (popupTypePrefabs == null || popupTypePrefabs.Length == 0)
            {
                GcLogger.LogError("Popup prefab 배열이 비어 있습니다.");
                return null;
            }

            if ((int)popupType < 0 || (int)popupType >= popupTypePrefabs.Length)
            {
                GcLogger.LogError($"잘못된 PopupType: {popupType}");
                return null;
            }
            return popupTypePrefabs[(int)popupType];
        }

        public void Cancel()
        {
            if (!_currentDefaultPopup) return;
            _currentDefaultPopup.ClosePopup();
        }
    }
}
