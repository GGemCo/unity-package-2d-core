using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 드롭 아이템의 시각 표현을 스프라이트 또는 VFX로 바인딩하고, 정렬 및 해제 상태를 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropItemVisualHost : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private VfxBehaviourBase _activeVfx;

        /// <summary>
        /// 현재 게임 오브젝트에 연결된 <see cref="SpriteRenderer"/>를 캐시합니다.
        /// </summary>
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 아이템 설정과 시각 테이블 정보를 기반으로 드롭 아이템의 스프라이트 또는 VFX를 바인딩합니다.
        /// </summary>
        /// <param name="itemUid">시각 표현을 조회할 아이템의 고유 식별자입니다.</param>
        /// <param name="defaultSpriteFileName">VFX를 사용하지 않거나 대체 표시가 필요할 때 사용할 기본 스프라이트 파일명입니다.</param>
        public void Bind(int itemUid, string defaultSpriteFileName)
        {
            ReleaseVisual();

            var itemSettings = AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.itemSettings
                : null;
            var tableItemVisual = TableLoaderManager.Instance != null
                ? TableLoaderManager.Instance.TableItemVisual?.TryGetByItemUid(itemUid)
                : null;

            var visualType = ResolveVisualType(itemSettings, tableItemVisual);
            var useSpriteFallback = itemSettings == null || itemSettings.useSpriteFallbackWhenVfxMissing;
            var hideSpriteWhenUsingVfx = itemSettings != null && itemSettings.hideSpriteRendererWhenUsingVfx;
            var visualScale = tableItemVisual != null && tableItemVisual.Scale > 0f
                ? tableItemVisual.Scale
                : itemSettings != null
                    ? itemSettings.defaultVisualScale
                    : 1f;
            var offsetY = tableItemVisual != null
                ? tableItemVisual.OffsetY
                : itemSettings != null
                    ? itemSettings.defaultVisualOffsetY
                    : 0f;

            switch (visualType)
            {
                case ItemConstants.DropVisualType.Vfx:
                    if (!TryBindVfx(tableItemVisual, visualScale, offsetY))
                    {
                        if (useSpriteFallback)
                            BindSprite(defaultSpriteFileName);
                    }
                    else if (hideSpriteWhenUsingVfx)
                    {
                        SetSpriteVisible(false);
                    }

                    break;

                case ItemConstants.DropVisualType.Sprite:
                default:
                    BindSprite(defaultSpriteFileName);
                    break;
            }
        }

        /// <summary>
        /// 스프라이트 렌더러와 활성 VFX에 동일한 정렬 레이어 및 정렬 순서를 적용합니다.
        /// </summary>
        /// <param name="sortingLayerName">적용할 Unity 정렬 레이어 이름입니다.</param>
        /// <param name="sortingOrder">정렬 레이어 내에서 사용할 렌더링 순서입니다.</param>
        public void ApplySorting(string sortingLayerName, int sortingOrder)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingLayerName = sortingLayerName;
                _spriteRenderer.sortingOrder = sortingOrder;
            }

            if (_activeVfx != null)
            {
                _activeVfx.SetSortingLayer(ConfigSortingLayer.ConvertKeys(sortingLayerName));
                _activeVfx.SetSortingOrder(sortingOrder);
            }
        }

        /// <summary>
        /// 현재 활성화된 VFX를 강제로 제거하고 스프라이트 표시 상태를 초기화합니다.
        /// </summary>
        public void ReleaseVisual()
        {
            if (_activeVfx != null)
            {
                // 아이템이 풀로 반환되어 다른 드랍에 재사용되기 전에 기존 VFX의 위치 추적 연결을 끊습니다.
                // 해제 연출이 남아 있더라도 재사용된 Item Transform을 따라 이동하지 않도록 보장합니다.
                TransformPositionFollower positionFollower =
                    _activeVfx.GetComponent<TransformPositionFollower>();
                positionFollower?.Clear();

                _activeVfx.DestroyForce();
                _activeVfx = null;
            }

            if (_spriteRenderer != null)
                _spriteRenderer.sprite = null;
            SetSpriteVisible(true);
        }

        /// <summary>
        /// 아이템 시각 테이블 정보를 사용하여 드롭 아이템에 연결될 VFX를 생성하고 위치 추적을 설정합니다.
        /// </summary>
        /// <param name="tableItemVisual">VFX 식별자, 크기, 오프셋 정보를 포함한 아이템 시각 테이블 행입니다.</param>
        /// <param name="visualScale">생성할 VFX에 적용할 크기 배율입니다.</param>
        /// <param name="offsetY">드롭 아이템 위치를 기준으로 적용할 Y축 오프셋입니다.</param>
        /// <returns>VFX 생성 및 위치 추적 바인딩에 성공하면 <c>true</c>, 필요한 정보가 없거나 생성에 실패하면 <c>false</c>입니다.</returns>
        private bool TryBindVfx(StruckTableItemVisual tableItemVisual, float visualScale, float offsetY)
        {
            if (tableItemVisual == null || tableItemVisual.VfxUid <= 0 || SceneGame.Instance == null ||
                SceneGame.Instance.VfxManager == null)
                return false;

            Vector3 worldOffset = new Vector3(0f, offsetY, 0f);
            _activeVfx = SceneGame.Instance.VfxManager.CreateVfx(new VfxSpawnRequest
            {
                VfxUid = tableItemVisual.VfxUid,
                WorldPosition = transform.position + worldOffset,
                ScaleOverride = visualScale,
                LifecycleTypeOverride = VfxConstants.LifecycleType.ManualRelease
            });

            if (_activeVfx == null)
                return false;

            var positionFollower = _activeVfx.GetComponent<TransformPositionFollower>();
            if (positionFollower == null)
                positionFollower = _activeVfx.gameObject.AddComponent<TransformPositionFollower>();
            positionFollower.Bind(transform, worldOffset);

            return true;
        }

        /// <summary>
        /// 기본 스프라이트 파일명을 사용하여 드롭 아이템 스프라이트를 로드하고 표시합니다.
        /// </summary>
        /// <param name="defaultSpriteFileName">로드할 드롭 아이템 스프라이트의 파일명입니다.</param>
        private void BindSprite(string defaultSpriteFileName)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = null;
            SetSpriteVisible(true);

            if (_spriteRenderer == null)
                return;

            _spriteRenderer.sprite = AddressableLoaderItem.Instance.GetImageDropByName(defaultSpriteFileName);
        }

        /// <summary>
        /// 아이템 시각 테이블과 전역 아이템 설정을 기준으로 사용할 드롭 시각 표현 방식을 결정합니다.
        /// </summary>
        /// <param name="itemSettings">드롭 아이템 시각 표현에 사용할 전역 아이템 설정입니다.</param>
        /// <param name="tableItemVisual">아이템별 시각 표현 설정이 담긴 테이블 행입니다.</param>
        /// <returns>테이블 또는 기본 설정에서 결정된 드롭 시각 표현 방식입니다.</returns>
        private static ItemConstants.DropVisualType ResolveVisualType(GGemCoItemSettings itemSettings,
            StruckTableItemVisual tableItemVisual)
        {
            if (tableItemVisual != null)
                return tableItemVisual.VisualType;

            if (itemSettings != null)
            {
                if (itemSettings.useSpriteFallbackWhenTableMissing)
                    return ItemConstants.DropVisualType.Sprite;

                return itemSettings.defaultDropVisualType;
            }

            return ItemConstants.DropVisualType.Sprite;
        }

        /// <summary>
        /// 스프라이트 렌더러의 표시 여부를 변경합니다.
        /// </summary>
        /// <param name="isVisible">스프라이트 렌더러를 표시하려면 <c>true</c>, 숨기려면 <c>false</c>입니다.</param>
        private void SetSpriteVisible(bool isVisible)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = isVisible;
        }
    }
}
