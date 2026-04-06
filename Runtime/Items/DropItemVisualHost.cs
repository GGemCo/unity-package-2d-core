using UnityEngine;

namespace GGemCo2DCore
{
    [DisallowMultipleComponent]
    public sealed class DropItemVisualHost : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private VfxBehaviourBase _activeVfx;
        private ItemConstants.DropVisualType _activeVisualType;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

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
                : itemSettings != null ? itemSettings.defaultVisualScale : 1f;
            var offsetY = tableItemVisual != null
                ? tableItemVisual.OffsetY
                : itemSettings != null ? itemSettings.defaultVisualOffsetY : 0f;

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

        public void ReleaseVisual()
        {
            if (_activeVfx != null)
            {
                _activeVfx.DestroyForce();
                _activeVfx = null;
            }

            _activeVisualType = ItemConstants.DropVisualType.Sprite;
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = null;
            SetSpriteVisible(true);
        }

        private bool TryBindVfx(StruckTableItemVisual tableItemVisual, float visualScale, float offsetY)
        {
            if (tableItemVisual == null || tableItemVisual.VfxUid <= 0 || SceneGame.Instance == null || SceneGame.Instance.VfxManager == null)
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

            _activeVisualType = ItemConstants.DropVisualType.Vfx;
            return true;
        }

        private void BindSprite(string defaultSpriteFileName)
        {
            _activeVisualType = ItemConstants.DropVisualType.Sprite;
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = null;
            SetSpriteVisible(true);

            if (_spriteRenderer == null)
                return;

            _spriteRenderer.sprite = AddressableLoaderItem.Instance.GetImageDropByName(defaultSpriteFileName);
        }

        private static ItemConstants.DropVisualType ResolveVisualType(GGemCoItemSettings itemSettings, StruckTableItemVisual tableItemVisual)
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

        private void SetSpriteVisible(bool isVisible)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = isVisible;
        }
    }
}
