using System;
using System.Collections;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public enum PopupBubbleThumbnailType
    {
        Player,
        Merchant,
        Witch,
        RogGoblin,
        FighterGoblin,
        MageGoblin,
        Guardian,
        BossGoblin,
    }
    public class PopupMetadataBubble : PopupMetadata
    {
        public PopupBubbleThumbnailType ThumbnailType;
        public float Duration;
        public Vector3 Position;
    }

    /// <summary>
    /// 디폴트 팝업창
    /// </summary>
    public class PopupBubble : DefaultPopup
    {
        [Serializable]
        private class EntityThumbnailInfo
        {
            [Tooltip("썸네일 이미지 타입")]
            public PopupBubbleThumbnailType thumbnailType;
            [Tooltip("썸네일 이미지")]
            public Sprite thumbnailSprite;
            [Tooltip("썸네일 표시 기준 위치")]
            public ConfigCommon.ThumbnailPositionType thumbnailPositionType;
        }
        
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("캐릭터 썸네일 이미지")]
        [SerializeField] private Image imageThumbnailCharacter;
        [Tooltip("캐릭터 썸네일 이미지 위치. 오른쪽 기준")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacter;
        [Tooltip("캐릭터 썸네일 이미지 위치. 왼쪽 기준")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacterLeft;
        
        [SerializeField] private List<EntityThumbnailInfo> entityThumbnailInfos;

        private RectTransform _bubbleRectTransform;
        private EntityThumbnailInfo _currentEntityPlayerInfo;
        private float _duration;
        private Vector3 _position;
        private Coroutine _coroutineFadeOut;
        
        protected override void Awake()
        {
            base.Awake();
            _bubbleRectTransform = GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            if (_coroutineFadeOut != null)
                StopCoroutine(_coroutineFadeOut);
        }

        protected override void OnInitialize(PopupMetadata popupMetadata)
        {
            PopupMetadataBubble popupMetadataBubble = popupMetadata as PopupMetadataBubble;
            if (popupMetadataBubble == null) return;
            
            _duration = popupMetadataBubble.Duration;
            _position = popupMetadataBubble.Position;
            _currentEntityPlayerInfo = GetEntityInfo(popupMetadataBubble.ThumbnailType);

            SetThumbnail();
            SetPosition();
            SetDuration();
        }

        private void SetPosition()
        {
            transform.position = _position;
        }

        private void SetDuration()
        {
            if (_duration <= 0) return;
            if (_coroutineFadeOut != null) StopCoroutine(_coroutineFadeOut);
            
            _coroutineFadeOut = StartCoroutine(CoroutineFadeOut());
        }

        private IEnumerator CoroutineFadeOut()
        {
            yield return new WaitForSeconds(_duration);
            ClosePopup();
        }

        private void SetThumbnail()
        {
            if (!imageThumbnailCharacter) return;
            if (_currentEntityPlayerInfo == null) return;
            imageThumbnailCharacter.sprite = _currentEntityPlayerInfo.thumbnailSprite;
            
            if (imageThumbnailCharacter && imageThumbnailCharacter.gameObject.TryGetComponent<RectTransform>(out var thumbnailRectTransform))
            {
                var panelHalfWidth = panelContent.rect.width * 0.5f;
                var thumbnailHalfWidth = thumbnailRectTransform.rect.width * 0.5f;
                var side = _currentEntityPlayerInfo.thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left ? -1f : 1f;

                var x = side * (panelHalfWidth + thumbnailHalfWidth) + offsetImageThumbnailCharacter.x;
                if (_currentEntityPlayerInfo.thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left)
                    x = side * (panelHalfWidth + thumbnailHalfWidth) + offsetImageThumbnailCharacterLeft.x;
                
                var y = offsetImageThumbnailCharacter.y;
                imageThumbnailCharacter.transform.localPosition = new Vector3(x, y, 0);
            }

        }

        private EntityThumbnailInfo GetEntityInfo(PopupBubbleThumbnailType thumbnailType)
        {
            foreach (var info in entityThumbnailInfos)
            {
                if (info.thumbnailType == thumbnailType) return info;
            }

            return null;
        }
    }
}
