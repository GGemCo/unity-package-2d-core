using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트(버프/디버프) 아이콘.
    /// - 테이블(StruckTableAffect) 직접 의존을 제거하고,
    /// - Presenter가 전달하는 <see cref="AffectUiItem"/>만으로 표시를 갱신한다.
    /// </summary>
    public class UIIconBuff : UIIcon
    {
        [Header("Affect Decorator")]
        [Tooltip("버프/디버프 타입 보조 아이콘. 비어 있으면 런타임에 자동 생성합니다.")]
        [SerializeField] private Image imageDecorator;

        private string _iconKey;
        private float _totalDuration;
        private float _remainingTime;
        private AffectUiDecoratorData _currentDecorator;

        // --- Caches (avoid repeating static UI work on every snapshot refresh) ---
        private int _cachedAffectUid;
        private string _cachedIconKey;
        private int _cachedStacks;
        private float _cachedTotalDuration;
        private bool _coolTimeHandlerStarted;

        protected override void Awake()
        {
            base.Awake();
            windowUid = UIWindowConstants.WindowUid.PlayerBuffInfo;
            IconType = IconConstants.Type.Buff;
            EnsureDecoratorImage();
            ClearDecorator();
        }

        /// <summary>
        /// 스냅샷 아이템을 바인딩한다.
        /// </summary>
        public void Bind(in AffectUiItem item)
        {
            if (item.AffectUid <= 0) return;

            // --------------------
            // 1) Static (set once)
            // --------------------
            // - Icon sprite/path
            // - Handler start/reset (only when duration policy changes)
            // --------------------
            int newUid = item.AffectUid;
            string newIconKey = item.IconKey;
            float newTotal = Mathf.Max(0f, item.TotalDuration);
            float newRemain = Mathf.Max(0f, item.RemainingTime);
            int newStacks = Mathf.Max(1, item.Stacks);

            bool uidChanged = _cachedAffectUid != newUid;
            bool iconChanged = uidChanged || !string.Equals(_cachedIconKey, newIconKey);
            bool totalChanged = !_coolTimeHandlerStarted || !Mathf.Approximately(_cachedTotalDuration, newTotal);

            uid = newUid;
            _iconKey = newIconKey;
            _totalDuration = newTotal;
            _remainingTime = newRemain;

            if (iconChanged)
            {
                // UpdateInfo() => UpdateIconImage() which loads sprite.
                // This is a relatively heavy operation, so only do it when uid/icon changes.
                UpdateInfo();
                _cachedAffectUid = newUid;
                _cachedIconKey = newIconKey;
            }

            // --------------------
            // 2) Dynamic (update only when changed)
            // --------------------
            if (_cachedStacks != newStacks)
            {
                SetCount(newStacks);
                _cachedStacks = newStacks;
            }

            ApplyDecorator(item.Decorator);

            // 쿨타임 게이지(남은 시간) 동기화
            var mgr = SceneGame.Instance != null ? SceneGame.Instance.uIIconCoolTimeManager : null;
            if (mgr == null) return;

            if (_totalDuration > 0f)
            {
                // StartHandler는 매번 호출할 필요가 없다.
                if (totalChanged)
                {
                    mgr.StartHandler(windowUid, this, _totalDuration);
                    _cachedTotalDuration = _totalDuration;
                    _coolTimeHandlerStarted = true;
                }

                // 남은 시간은 주기적으로 갱신되어야 한다.
                mgr.SetRemainCoolTime(windowUid, uid, _remainingTime);
            }
            else
            {
                // 지속 시간이 0이면 게이지를 끈다. (이전 상태가 켜져있던 경우에만)
                if (_coolTimeHandlerStarted)
                {
                    mgr.ResetCoolTime(windowUid, uid);
                    _cachedTotalDuration = 0f;
                    _coolTimeHandlerStarted = false;
                }
            }
        }

        /// <summary>
        /// 풀링 회수 시 호출.
        /// </summary>
        public void ClearCoolTime()
        {
            var mgr = SceneGame.Instance != null ? SceneGame.Instance.uIIconCoolTimeManager : null;
            if (mgr != null)
                mgr.ResetCoolTime(windowUid, uid);

            _coolTimeHandlerStarted = false;
            _cachedTotalDuration = 0f;
        }

        public void ClearDecorator()
        {
            _currentDecorator = AffectUiDecoratorData.Hidden;
            if (imageDecorator == null)
                return;

            imageDecorator.sprite = null;
            imageDecorator.enabled = false;
            imageDecorator.gameObject.SetActive(false);
        }

        /// <summary>
        /// 풀링 재사용을 고려해, 바인딩 캐시를 초기화한다.
        /// (아이콘 sprite/카운트/쿨타임 핸들러 상태 등)
        /// </summary>
        public void ResetBindingCache()
        {
            _cachedAffectUid = 0;
            _cachedIconKey = null;
            _cachedStacks = 0;
            _cachedTotalDuration = 0f;
            _coolTimeHandlerStarted = false;
            ClearDecorator();
        }

        private void ApplyDecorator(in AffectUiDecoratorData decorator)
        {
            EnsureDecoratorImage();
            if (imageDecorator == null)
                return;

            if (!decorator.Visible || decorator.Sprite == null)
            {
                if (_currentDecorator.Visible)
                    ClearDecorator();
                return;
            }

            imageDecorator.sprite = decorator.Sprite;
            imageDecorator.enabled = true;
            imageDecorator.gameObject.SetActive(true);

            var rect = imageDecorator.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = decorator.Size;
            rect.anchoredPosition = ResolveDecoratorPosition(decorator.Anchor, decorator.Offset);

            _currentDecorator = decorator;
        }

        private Vector2 ResolveDecoratorPosition(AffectUiDecoratorAnchor anchor, Vector2 offset)
        {
            var iconRect = GetComponent<RectTransform>();
            float width = 0f;
            float height = 0f;
            if (iconRect != null)
            {
                width = Mathf.Abs(iconRect.rect.width);
                height = Mathf.Abs(iconRect.rect.height);
            }

            float halfX = width * 0.25f;
            float halfY = height * 0.25f;
            Vector2 basePosition = anchor switch
            {
                AffectUiDecoratorAnchor.LeftBottom => new Vector2(-halfX, -halfY),
                AffectUiDecoratorAnchor.RightBottom => new Vector2(halfX, -halfY),
                AffectUiDecoratorAnchor.LeftTop => new Vector2(-halfX, halfY),
                AffectUiDecoratorAnchor.RightTop => new Vector2(halfX, halfY),
                _ => new Vector2(halfX, -halfY)
            };

            return basePosition + offset;
        }

        private void EnsureDecoratorImage()
        {
            if (imageDecorator != null)
                return;

            var child = transform.Find("AffectTypeDecorator");
            if (child != null)
            {
                imageDecorator = child.GetComponent<Image>();
                if (imageDecorator != null)
                    return;
            }

            var go = new GameObject("AffectTypeDecorator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);

            imageDecorator = go.GetComponent<Image>();
            imageDecorator.raycastTarget = false;
            imageDecorator.enabled = false;
            go.SetActive(false);
        }

        /// <summary>
        /// 아이콘 이미지 경로 가져오기.
        /// </summary>
        protected override string GetIconImagePath()
        {
            return FileHelper.GetFileName(_iconKey);
        }

        /// <summary>
        /// 아이콘 이미지 업데이트.
        /// </summary>
        protected override void UpdateIconImage()
        {
            if (ImageIcon == null) return;

            string key = GetIconImagePath();
            if (string.IsNullOrEmpty(key))
            {
                ImageIcon.sprite = spriteBlank;
                CacheNormalIconSprite(spriteBlank);
                return;
            }

            // Affect 패키지가 설치되어 있으면 아이콘을 로드한다. (Core는 Affect를 직접 참조하지 않는다.)
            Sprite sprite = AffectRuntimeBridge.TryLoadIconSprite(key);
            ImageIcon.sprite = sprite;
            CacheNormalIconSprite(sprite);
        }
    }
}
