using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Runtime buyability rules for shop items.
    /// </summary>
    public sealed class ShopAvailabilityService
    {
        public static ShopAvailabilityService Instance { get; } = new ShopAvailabilityService();

        private readonly Dictionary<int, AvailabilityRule> _itemRules = new Dictionary<int, AvailabilityRule>();
        private readonly Dictionary<(int shopUid, int itemUid), AvailabilityRule> _shopItemRules = new Dictionary<(int shopUid, int itemUid), AvailabilityRule>();
        private readonly Dictionary<(int shopUid, int slotIndex), AvailabilityRule> _slotRules = new Dictionary<(int shopUid, int slotIndex), AvailabilityRule>();
        private readonly Dictionary<(int shopUid, int slotIndex, int itemUid), AvailabilityRule> _productRules = new Dictionary<(int shopUid, int slotIndex, int itemUid), AvailabilityRule>();

        public event Action Changed;

        public void SetItemBuyable(int itemUid, bool isBuyable, string disabledReason = null)
        {
            if (itemUid <= 0) return;
            SetRule(_itemRules, itemUid, isBuyable, disabledReason);
        }

        public void SetShopItemBuyable(int shopUid, int itemUid, bool isBuyable, string disabledReason = null)
        {
            if (shopUid <= 0 || itemUid <= 0) return;
            SetRule(_shopItemRules, (shopUid, itemUid), isBuyable, disabledReason);
        }

        public void SetSlotBuyable(int shopUid, int slotIndex, bool isBuyable, string disabledReason = null)
        {
            if (shopUid <= 0 || slotIndex < 0) return;
            SetRule(_slotRules, (shopUid, slotIndex), isBuyable, disabledReason);
        }

        public void SetProductBuyable(int shopUid, int slotIndex, int itemUid, bool isBuyable, string disabledReason = null)
        {
            if (shopUid <= 0 || slotIndex < 0 || itemUid <= 0) return;
            SetRule(_productRules, (shopUid, slotIndex, itemUid), isBuyable, disabledReason);
        }

        public void ClearItemRule(int itemUid)
        {
            if (_itemRules.Remove(itemUid))
            {
                Changed?.Invoke();
            }
        }

        public void ClearShopItemRule(int shopUid, int itemUid)
        {
            if (_shopItemRules.Remove((shopUid, itemUid)))
            {
                Changed?.Invoke();
            }
        }

        public void ClearSlotRule(int shopUid, int slotIndex)
        {
            if (_slotRules.Remove((shopUid, slotIndex)))
            {
                Changed?.Invoke();
            }
        }

        public void ClearProductRule(int shopUid, int slotIndex, int itemUid)
        {
            if (_productRules.Remove((shopUid, slotIndex, itemUid)))
            {
                Changed?.Invoke();
            }
        }

        public void ClearAllRules()
        {
            _itemRules.Clear();
            _shopItemRules.Clear();
            _slotRules.Clear();
            _productRules.Clear();
            Changed?.Invoke();
        }

        public bool CanBuy(ShopDisplayItem item, out string disabledReason)
        {
            disabledReason = null;
            if (item == null) return false;

            if (TryGetRule(_productRules, (item.ShopUid, item.SlotIndex, item.ItemUid), out var rule) ||
                TryGetRule(_shopItemRules, (item.ShopUid, item.ItemUid), out rule) ||
                TryGetRule(_slotRules, (item.ShopUid, item.SlotIndex), out rule) ||
                TryGetRule(_itemRules, item.ItemUid, out rule))
            {
                disabledReason = rule.DisabledReason;
                return rule.IsBuyable;
            }

            return true;
        }

        private void SetRule<TKey>(Dictionary<TKey, AvailabilityRule> rules, TKey key, bool isBuyable, string disabledReason)
        {
            rules[key] = new AvailabilityRule(isBuyable, disabledReason);
            Changed?.Invoke();
        }

        private static bool TryGetRule<TKey>(Dictionary<TKey, AvailabilityRule> rules, TKey key, out AvailabilityRule rule)
        {
            return rules.TryGetValue(key, out rule);
        }

        private readonly struct AvailabilityRule
        {
            public readonly bool IsBuyable;
            public readonly string DisabledReason;

            public AvailabilityRule(bool isBuyable, string disabledReason)
            {
                IsBuyable = isBuyable;
                DisabledReason = disabledReason;
            }
        }
    }
}
