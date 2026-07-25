using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowDialogue"/> 말풍선의 첫 프레임 레이아웃 노출을 제어합니다.
    /// </summary>
    public partial class UIWindowDialogue
    {
        /// <summary>
        /// 초기 노출 제어에 사용할 말풍선 루트 CanvasGroup 참조를 캐시합니다.
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
        /// 첫 메시지 레이아웃 계산이 끝날 때까지 패널 노출을 지연합니다.
        /// </summary>
        /// <param name="requestVersion">현재 노드 요청 버전입니다.</param>
        private void BeginDeferredInitialReveal(int requestVersion)
        {
            if (dialogueVisualMode != DialogueVisualMode.SpeechBubble)
            {
                return;
            }

            _isInitialRevealPending = true;
            _initialRevealRequestVersion = requestVersion;
            SetPanelDialogueVisibilityForInitialReveal(false);
        }

        /// <summary>
        /// 지연 중인 초기 노출 상태를 취소하고 패널 가시성을 복원합니다.
        /// </summary>
        private void CancelDeferredInitialReveal()
        {
            _isInitialRevealPending = false;
            _initialRevealRequestVersion = -1;
            SetPanelDialogueVisibilityForInitialReveal(true);
        }

        /// <summary>
        /// 요청 버전이 유효하면 레이아웃을 즉시 계산한 뒤 말풍선을 노출합니다.
        /// </summary>
        /// <param name="requestVersion">완료할 노드 요청 버전입니다.</param>
        private void TryCompleteDeferredInitialReveal(int requestVersion)
        {
            if (!_isInitialRevealPending ||
                requestVersion != _dialogueLoadVersion ||
                requestVersion != _initialRevealRequestVersion)
            {
                return;
            }

            ApplyInitialRevealLayoutPasses();
            SetPanelDialogueVisibilityForInitialReveal(true);
            _isInitialRevealPending = false;
            _initialRevealRequestVersion = -1;
        }

        /// <summary>
        /// 첫 프레임 위치 점프를 줄이기 위해 텍스트 메시와 레이아웃을 두 차례 계산합니다.
        /// </summary>
        private void ApplyInitialRevealLayoutPasses()
        {
            textMessage?.ForceMeshUpdate();
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
        /// 초기 노출 제어용 CanvasGroup의 가시성과 입력 가능 상태를 설정합니다.
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
