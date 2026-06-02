using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// Unity UI Image에 Addressables 아이콘 Sprite를 안전하게 바인딩하는 공용 컴포넌트입니다.
    /// </summary>
    public sealed class UIAddressableIconBinder : MonoBehaviour
    {
        [Tooltip("아이콘 Sprite를 적용할 Image입니다. 비어 있으면 현재 GameObject의 Image를 사용합니다.")]
        [SerializeField] private Image targetImage;

        [Tooltip("로드 중이거나 실패했을 때 사용할 대체 Sprite입니다.")]
        [SerializeField] private Sprite fallbackSprite;

        [Tooltip("로드 중에 대체 Sprite를 표시할지 여부입니다.")]
        [SerializeField] private bool showFallbackWhileLoading;

        private int _requestVersion;

        /// <summary>
        /// 아이콘 Sprite 적용 결과가 변경될 때 호출됩니다.
        /// </summary>
        public event Action<Sprite> SpriteApplied;

        /// <summary>
        /// 현재 GameObject에 연결된 Image를 캐시합니다.
        /// </summary>
        private void Awake()
        {
            ResolveTargetImage();
        }

        /// <summary>
        /// 비활성화 시 지연 로드 완료 콜백이 이전 요청을 덮어쓰지 못하도록 무효화합니다.
        /// </summary>
        private void OnDisable()
        {
            _requestVersion++;
        }

        /// <summary>
        /// Addressables 아이콘 Sprite 요청을 Image에 바인딩합니다.
        /// 캐시에 있으면 즉시 적용하고, 없으면 비동기 로드 후 현재 요청일 때만 적용합니다.
        /// </summary>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        public void Bind(AddressableIconSpriteRequest request)
        {
            int version = ++_requestVersion;
            Image image = ResolveTargetImage();
            if (image == null)
            {
                return;
            }

            if (!request.IsValid)
            {
                ApplySprite(version, null);
                return;
            }

            if (AddressableIconSpriteService.TryGetCachedSprite(request, out Sprite cachedSprite))
            {
                ApplySprite(version, cachedSprite);
                return;
            }

            if (showFallbackWhileLoading)
            {
                image.sprite = fallbackSprite;
            }

            _ = BindAsync(version, request);
        }

        /// <summary>
        /// 현재 아이콘 바인딩 요청을 해제하고 Image를 비웁니다.
        /// </summary>
        public void Clear()
        {
            int version = ++_requestVersion;
            ApplySprite(version, null);
        }

        /// <summary>
        /// 필요한 아틀라스를 비동기로 로드하고, 요청이 여전히 유효하면 Sprite를 적용합니다.
        /// </summary>
        /// <param name="version">요청 버전입니다.</param>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        private async Task BindAsync(int version, AddressableIconSpriteRequest request)
        {
            try
            {
                Sprite sprite = await AddressableIconSpriteService.LoadSpriteAsync(request);
                if (this == null)
                {
                    return;
                }

                ApplySprite(version, sprite);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"UI 아이콘 비동기 바인딩 중 오류가 발생했습니다. sprite={request.SpriteName}, error={ex.Message}");
            }
        }

        /// <summary>
        /// 요청 버전과 생존 상태를 확인한 뒤 Image에 Sprite를 적용합니다.
        /// </summary>
        /// <param name="version">요청 버전입니다.</param>
        /// <param name="sprite">적용할 Sprite입니다.</param>
        private void ApplySprite(int version, Sprite sprite)
        {
            if (version != _requestVersion)
            {
                return;
            }

            Image image = ResolveTargetImage();
            if (image == null)
            {
                return;
            }

            image.sprite = sprite != null ? sprite : fallbackSprite;
            SpriteApplied?.Invoke(sprite);
        }

        /// <summary>
        /// 대상 Image를 반환합니다.
        /// Inspector에 지정되지 않았으면 현재 GameObject에서 자동 탐색합니다.
        /// </summary>
        /// <returns>Sprite를 적용할 Image입니다.</returns>
        private Image ResolveTargetImage()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            return targetImage;
        }
    }
}
