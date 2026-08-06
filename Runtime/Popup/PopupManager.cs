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
            Default, // 메시지, 확인, 취소 버튼 있는 타입
            Bubble,
        }

        [SerializeField] private GameObject[] popupTypePrefabs;
        public void SetPopupTypePrefabs(GameObject[] prefabs) => popupTypePrefabs = prefabs;
        
        [SerializeField] private Transform canvasPopup; // 팝업이 들어갈 canvas
        public void SetCanvasPopup(Transform value) => canvasPopup = value;

        private readonly Queue<PopupMetadata> _popupQueue = new Queue<PopupMetadata>();
        private readonly HashSet<string> _reservedRequestKeys = new HashSet<string>();
        private readonly Dictionary<DefaultPopup, string> _requestKeyByPopup = new Dictionary<DefaultPopup, string>();
        private DefaultPopup _currentDefaultPopup;
        
        /// <summary>
        /// 공통 팝업 생성 메서드
        /// </summary>
        /// <param name="popupMetadata"></param>
        private void ShowPopupWithMetadata(PopupMetadata popupMetadata)
        {
            if (popupMetadata == null)
            {
                GcLogger.LogError("팝업 메타데이터가 없습니다.");
                return;
            }

            if (!TryReserveRequestKey(popupMetadata.RequestKey))
            {
                return;
            }

            if (popupMetadata.ForceShow)
            {
                var popup = CreatePopup(popupMetadata);
                if (popup == null)
                {
                    ReleaseRequestKey(popupMetadata.RequestKey);
                    return;
                }

                TrackPopupRequest(popup, popupMetadata.RequestKey);
                popup.ShowPopup();
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

            // 프리팹 누락 등으로 생성에 실패한 요청은 해제하고 다음 요청을 계속 처리합니다.
            while (_popupQueue.Count > 0)
            {
                PopupMetadata nextMetadata = _popupQueue.Dequeue();
                _currentDefaultPopup = CreatePopup(nextMetadata);
                if (_currentDefaultPopup == null)
                {
                    ReleaseRequestKey(nextMetadata.RequestKey);
                    continue;
                }

                TrackPopupRequest(_currentDefaultPopup, nextMetadata.RequestKey);
                _currentDefaultPopup.ShowPopup();
                return;
            }
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
                if (_requestKeyByPopup.TryGetValue(popup, out string requestKey))
                {
                    _requestKeyByPopup.Remove(popup);
                    ReleaseRequestKey(requestKey);
                }
            }

            if (_currentDefaultPopup == popup)
            {
                _currentDefaultPopup = null;
            }

            ShowNextPopup();
        }

        /// <summary>
        /// 요청 키가 비어 있지 않을 때 동일한 팝업이 표시 중이거나 대기 중인지 확인하고 예약합니다.
        /// </summary>
        /// <param name="requestKey">중복 여부를 확인할 요청 식별자입니다.</param>
        /// <returns>새 요청을 등록할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryReserveRequestKey(string requestKey)
        {
            return string.IsNullOrEmpty(requestKey) || _reservedRequestKeys.Add(requestKey);
        }

        /// <summary>
        /// 생성된 팝업과 요청 키의 수명주기를 연결합니다.
        /// </summary>
        /// <param name="popup">생성된 팝업입니다.</param>
        /// <param name="requestKey">팝업 요청 식별자입니다.</param>
        private void TrackPopupRequest(DefaultPopup popup, string requestKey)
        {
            if (popup == null || string.IsNullOrEmpty(requestKey))
            {
                return;
            }

            _requestKeyByPopup[popup] = requestKey;
        }

        /// <summary>
        /// 더 이상 표시하거나 대기하지 않는 요청 키의 예약을 해제합니다.
        /// </summary>
        /// <param name="requestKey">해제할 요청 식별자입니다.</param>
        private void ReleaseRequestKey(string requestKey)
        {
            if (!string.IsNullOrEmpty(requestKey))
            {
                _reservedRequestKeys.Remove(requestKey);
            }
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

        /// <summary>
        /// 표시 중이거나 대기 중인 팝업 가운데 요청 키가 일치하는 항목만 취소합니다.
        /// </summary>
        /// <param name="requestKey">취소할 팝업 요청 식별자입니다.</param>
        /// <returns>취소할 팝업을 찾았으면 <see langword="true"/>를 반환합니다.</returns>
        public bool Cancel(string requestKey)
        {
            if (string.IsNullOrEmpty(requestKey))
            {
                return false;
            }

            DefaultPopup popupToClose = null;
            foreach (KeyValuePair<DefaultPopup, string> pair in _requestKeyByPopup)
            {
                if (pair.Value == requestKey)
                {
                    popupToClose = pair.Key;
                    break;
                }
            }

            if (popupToClose != null)
            {
                popupToClose.ClosePopup();
                return true;
            }

            bool removed = false;
            int queuedCount = _popupQueue.Count;
            for (int i = 0; i < queuedCount; i++)
            {
                PopupMetadata metadata = _popupQueue.Dequeue();
                if (!removed && metadata != null && metadata.RequestKey == requestKey)
                {
                    removed = true;
                    continue;
                }

                _popupQueue.Enqueue(metadata);
            }

            if (removed)
            {
                ReleaseRequestKey(requestKey);
            }

            return removed;
        }
    }
}
