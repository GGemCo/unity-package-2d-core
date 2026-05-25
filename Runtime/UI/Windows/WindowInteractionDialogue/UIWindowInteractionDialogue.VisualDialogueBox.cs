using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public partial class UIWindowInteractionDialogue
    {
        /// <summary>
        /// 썸네일 위치를 대화 내용과 시각 모드에 맞게 갱신합니다.
        /// </summary>
        private void RefreshThumbnailPosition()
        {
            if (dialogueVisualMode == DialogueVisualMode.SpeechBubble)
            {
                RefreshSpeechBubbleLayout();
                return;
            }

            RefreshDialogueBoxThumbnailPosition();
        }

        /// <summary>
        /// 대화 박스 모드의 썸네일 위치 계산을 수행합니다.
        /// </summary>
        private void RefreshDialogueBoxThumbnailPosition()
        {
            if (panelMessage == null)
            {
                return;
            }

            if (imageThumbnail == null || !imageThumbnail.gameObject.TryGetComponent(out RectTransform thumbnailRectTransform))
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

            float x = side * (panelHalfWidth + thumbnailHalfWidth) + offset.x;
            float y = offset.y;
            SetThumbnailLocalPositionRelativeToPanel(thumbnailRectTransform, new Vector2(x, y));
        }

        /// <summary>
        /// 현재 UI에 적용할 유효 썸네일 배치 방향을 계산합니다.
        /// </summary>
        /// <remarks>
        /// 노드 값이 <see cref="ConfigCommon.ThumbnailPositionType.None"/> 이면 모드/옵션에 따라 오른쪽 폴백 또는 숨김 처리합니다.
        /// </remarks>
        /// <returns>유효한 썸네일 배치 방향입니다.</returns>
        private ConfigCommon.ThumbnailPositionType ResolveEffectiveThumbnailPositionType()
        {
            if (_currentDialogueNode == null)
            {
                return ConfigCommon.ThumbnailPositionType.Right;
            }

            if (_currentDialogueNode.thumbnailPositionType != ConfigCommon.ThumbnailPositionType.None)
            {
                return _currentDialogueNode.thumbnailPositionType;
            }

            if (useLegacyThumbnailFallbackForNone || dialogueVisualMode == DialogueVisualMode.DialogueBox)
            {
                return ConfigCommon.ThumbnailPositionType.Right;
            }

            return ConfigCommon.ThumbnailPositionType.None;
        }

        /// <summary>
        /// 썸네일 배치 방향을 좌(-1) / 우(+1) 부호로 변환합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        /// <returns>왼쪽이면 -1, 오른쪽이면 +1을 반환합니다.</returns>
        private static float ResolveThumbnailSideSign(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            return thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left ? -1f : 1f;
        }

        /// <summary>
        /// 썸네일 배치 방향에 맞는 오프셋 값을 반환합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        /// <returns>좌우 방향별 오프셋입니다.</returns>
        private Vector3 ResolveThumbnailOffset(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            return thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left
                ? offsetImageThumbnailCharacterLeft
                : offsetImageThumbnailCharacter;
        }

        /// <summary>
        /// 텍스트 패널 기준 좌표를 썸네일 부모 좌표계로 변환해 썸네일 위치에 적용합니다.
        /// 썸네일이 텍스트 패널의 자식이 아닌 프리팹에서도 좌/우 오프셋이 동일한 기준으로 적용되게 합니다.
        /// </summary>
        /// <param name="thumbnailRectTransform">위치를 적용할 썸네일 RectTransform입니다.</param>
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

            Vector3 worldPoint = panelMessage.TransformPoint(new Vector3(panelLocalPosition.x, panelLocalPosition.y, 0f));
            Vector3 parentLocalPoint = thumbnailParentRectTransform.InverseTransformPoint(worldPoint);
            thumbnailRectTransform.localPosition = new Vector3(
                parentLocalPoint.x,
                parentLocalPoint.y,
                thumbnailRectTransform.localPosition.z);
        }
    }
}
