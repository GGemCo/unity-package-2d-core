using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowInteractionDialogue"/>의 초기 노출 타이밍 제어를 담당하는 partial 스크립트입니다.
    /// </summary>
    public partial class UIWindowInteractionDialogue
    {
        /// <summary>
        /// 말풍선 루트 패널의 <see cref="CanvasGroup"/> 참조를 캐시합니다.
        /// </summary>
        private void CacheInitialRevealCanvasGroupReference()
        {
            if (panelDialogue == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            GameObject target = panelDialogue != null
                ? panelDialogue
                : _panelDialogueRectTransform != null
                    ? _panelDialogueRectTransform.gameObject
                    : null;

            if (target == null)
            {
                _panelDialogueCanvasGroup = null;
                return;
            }

            UiFadeUtility.TryGetCanvasGroup(target, true, out _panelDialogueCanvasGroup);
        }

        /// <summary>
        /// 초기 메시지/썸네일 레이아웃 계산이 끝나기 전까지 패널 노출을 지연하도록 설정합니다.
        /// </summary>
        /// <param name="requestVersion">현재 인터랙션 요청 버전입니다.</param>
        private void BeginDeferredInitialReveal(int requestVersion)
        {
            _isInitialRevealPending = true;
            _initialRevealRequestVersion = requestVersion;
            SetPanelDialogueVisibilityForInitialReveal(false);
        }

        /// <summary>
        /// 노출 지연 상태를 해제하고 기본 가시 상태를 복원합니다.
        /// 숨김 처리 중 세션이 종료될 때 잔여 상태가 남지 않도록 정리합니다.
        /// </summary>
        private void CancelDeferredInitialReveal()
        {
            _isInitialRevealPending = false;
            _initialRevealRequestVersion = -1;
            SetPanelDialogueVisibilityForInitialReveal(true);
        }

        /// <summary>
        /// 요청 버전이 유효하면 말풍선 레이아웃을 강제 계산한 뒤 패널을 노출합니다.
        /// </summary>
        /// <param name="requestVersion">노출 완료를 시도할 요청 버전입니다.</param>
        private void TryCompleteDeferredInitialReveal(int requestVersion)
        {
            if (!_isInitialRevealPending)
            {
                return;
            }

            if (requestVersion != _dialogueLoadVersion || requestVersion != _initialRevealRequestVersion)
            {
                return;
            }

            ApplyInitialRevealLayoutPasses();
            SetPanelDialogueVisibilityForInitialReveal(true);
            _isInitialRevealPending = false;
            _initialRevealRequestVersion = -1;
        }

        /// <summary>
        /// 초기 노출 전에 텍스트 메쉬/레이아웃/썸네일 위치를 강제 갱신합니다.
        /// 프레임 경계에서 발생하는 첫 프레임 점프를 줄이기 위해 2회 패스를 수행합니다.
        /// </summary>
        private void ApplyInitialRevealLayoutPasses()
        {
            if (textMessage != null)
            {
                textMessage.ForceMeshUpdate();
            }

            for (int i = 0; i < 2; i++)
            {
                Canvas.ForceUpdateCanvases();
                if (panelMessage != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(panelMessage);
                }

                RefreshThumbnailPosition();
                RefreshPosition();
            }
        }

        /// <summary>
        /// 초기 노출 제어용으로 말풍선 루트 패널의 가시성과 입력 가능 상태를 설정합니다.
        /// </summary>
        /// <param name="visible">표시 여부입니다.</param>
        private void SetPanelDialogueVisibilityForInitialReveal(bool visible)
        {
            CacheInitialRevealCanvasGroupReference();
            if (_panelDialogueCanvasGroup == null)
            {
                return;
            }

            _panelDialogueCanvasGroup.alpha = visible ? 1f : 0f;
            _panelDialogueCanvasGroup.interactable = visible;
            _panelDialogueCanvasGroup.blocksRaycasts = visible;
        }
    }
}

