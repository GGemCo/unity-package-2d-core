using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 아이템 아이콘 제공자(Core).
    /// </summary>
    public sealed class QuickSlotItemContentProvider : IQuickSlotContentProvider
    {
        public int Priority => 0;

        private TableItem _tableItem;

        public bool CanProvide(QuickSlotContentKind kind) => kind == QuickSlotContentKind.Item;

        public Sprite GetIconSprite(QuickSlotContentKind kind, int uid, int level, long instanceId)
        {
            if (uid <= 0) return null;
            if (AddressableLoaderItem.Instance == null) return null;
            if (TableLoaderManager.Instance == null) return null;

            _tableItem ??= TableLoaderManager.Instance.TableItem;
            var info = _tableItem?.GetDataByUid(uid);
            if (info == null) return null;

            var path = info.ImageItemPath;
            if (string.IsNullOrEmpty(path)) return null;

            return AddressableLoaderItem.Instance.GetImageIconItemByName(path);
        }
    }
}
