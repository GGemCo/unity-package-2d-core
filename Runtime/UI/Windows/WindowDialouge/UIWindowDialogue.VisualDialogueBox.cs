using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowDialogue"/>의 대화 박스 및 공통 썸네일 배치를 담당합니다.
    /// </summary>
    public partial class UIWindowDialogue
    {
        /// <summary>
        /// 현재 시각 모드에 맞춰 썸네일 위치를 갱신합니다.
        /// </summary>
        private void RefreshThumbnailPosition()
        {
            if (dialogueVisualMode == DialogueVisualMode.SpeechBubble)
            {
                RefreshSpeechBubbleLayout();
            }
        }

        /// <summary>
        /// 대화 박스 모드의 썸네일 위치를 계산합니다.
        /// </summary>
        private void RefreshDialogueBoxThumbnailPosition()
        {
            if (panelMessage == null ||
                imageThumbnail == null ||
                !imageThumbnail.gameObject.TryGetComponent(out RectTransform thumbnailRectTransform))
            {
                return;
            }

            ConfigCommon.ThumbnailPositionType thumbnailPositionType = ResolveEffectiveThumbnailPositionType();
            if (thumbnailPositionType == ConfigCommon.ThumbnailPositionType.None)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelMessage);

            float panelHalfWidth = panelMessage.rect.width * 0.5f;
            float thumbnailHalfWidth = thumbnailRectTransform.rect.width * 0.5f;
            Vector3 offset = ResolveThumbnailOffset(thumbnailPositionType);
            float side = ResolveThumbnailSideSign(thumbnailPositionType);
            SetThumbnailLocalPositionRelativeToPanel(
                thumbnailRectTransform,
                new Vector2(side * (panelHalfWidth + thumbnailHalfWidth) + offset.x, offset.y));
        }

        /// <summary>
        /// 현재 노드와 시각 모드에 적용할 유효 썸네일 배치 방향을 계산합니다.
        /// </summary>
        /// <returns>유효한 썸네일 배치 방향입니다.</returns>
        private ConfigCommon.ThumbnailPositionType ResolveEffectiveThumbnailPositionType()
        {
            if (_currentDialogue == null)
            {
                return ConfigCommon.ThumbnailPositionType.Right;
            }

            if (_currentDialogue.thumbnailPositionType != ConfigCommon.ThumbnailPositionType.None)
            {
                return _currentDialogue.thumbnailPositionType;
            }

            if (useLegacyThumbnailFallbackForNone || dialogueVisualMode == DialogueVisualMode.DialogueBox)
            {
                return ConfigCommon.ThumbnailPositionType.Right;
            }

            return ConfigCommon.ThumbnailPositionType.None;
        }

        /// <summary>
        /// 썸네일 배치 방향을 좌(-1) 또는 우(+1) 부호로 변환합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        /// <returns>왼쪽이면 -1, 오른쪽이면 +1입니다.</returns>
        private static float ResolveThumbnailSideSign(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            return thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left ? -1f : 1f;
        }

        /// <summary>
        /// 썸네일 배치 방향에 맞는 오프셋을 반환합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        /// <returns>좌우 방향별 썸네일 오프셋입니다.</returns>
        private Vector3 ResolveThumbnailOffset(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            return thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left
                ? offsetImageThumbnailCharacterLeft
                : offsetImageThumbnailCharacter;
        }

        /// <summary>
        /// 텍스트 패널 기준 좌표를 썸네일 부모 좌표계로 변환하여 적용합니다.
        /// </summary>
        /// <param name="thumbnailRectTransform">위치를 적용할 RectTransform입니다.</param>
        /// <param name="panelLocalPosition">텍스트 패널 기준 로컬 좌표입니다.</param>
        private void SetThumbnailLocalPositionRelativeToPanel(
            RectTransform thumbnailRectTransform,
            Vector2 panelLocalPosition)
        {
            if (thumbnailRectTransform == null)
            {
                return;
            }

            RectTransform thumbnailParentRectTransform = thumbnailRectTransform.parent as RectTransform;
            if (panelMessage == null || thumbnailParentRectTransform == null)
            {
                thumbnailRectTransform.localPosition = new Vector3(
                    panelLocalPosition.x,
                    panelLocalPosition.y,
                    thumbnailRectTransform.localPosition.z);
                return;
            }

            Vector3 worldPoint = panelMessage.TransformPoint(
                new Vector3(panelLocalPosition.x, panelLocalPosition.y, 0f));
            Vector3 parentLocalPoint = thumbnailParentRectTransform.InverseTransformPoint(worldPoint);
            thumbnailRectTransform.localPosition = new Vector3(
                parentLocalPoint.x,
                parentLocalPoint.y,
                thumbnailRectTransform.localPosition.z);
        }
    }
}
