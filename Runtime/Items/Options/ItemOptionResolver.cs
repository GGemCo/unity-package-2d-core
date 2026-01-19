using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 정의(Base) + 인스턴스(Roll) 옵션을 합쳐 최종 옵션 리스트를 산출한다.
    /// </summary>
    public sealed class ItemOptionResolver
    {
        private readonly TableLoaderManager _tables;

        public ItemOptionResolver(TableLoaderManager tables)
        {
            _tables = tables;
        }

        /// <summary>
        /// 지정한 아이템 UID의 고정 옵션을 조회한다.
        /// </summary>
        public List<ItemOptionEntry> ResolveBaseOptions(int itemUid)
        {
            var result = new List<ItemOptionEntry>(8);
            if (_tables == null) return result;

            var table = _tables.TableItemBaseOption;
            if (table == null) return result;

            var rows = table.GetByItemUid(itemUid);
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r == null || string.IsNullOrEmpty(r.TargetId)) continue;
                result.Add(new ItemOptionEntry(r.Kind, r.TargetId, r.Op, r.Value, r.Chance, r.Duration));
            }
            return result;
        }

        /// <summary>
        /// 인스턴스에 저장된 랜덤 옵션을 조회한다.
        /// </summary>
        public List<ItemOptionEntry> ResolveRolledOptions(ItemInstanceInfo instance)
        {
            var result = new List<ItemOptionEntry>(8);
            if (_tables == null || instance == null) return result;

            var defTable = _tables.TableItemAffixDef;
            if (defTable == null) return result;

            var list = instance.RolledAffixes;
            if (list == null) return result;

            for (int i = 0; i < list.Count; i++)
            {
                var roll = list[i];
                var def = defTable.GetByUid(roll.AffixUid);
                if (def == null || string.IsNullOrEmpty(def.TargetId)) continue;

                result.Add(new ItemOptionEntry(def.Kind, def.TargetId, def.Op, roll.RolledValue));
            }

            return result;
        }

        /// <summary>
        /// 최종 옵션(Base + Rolled) 리스트.
        /// </summary>
        public List<ItemOptionEntry> ResolveFinalOptions(ItemInstanceInfo instance)
        {
            var result = ResolveBaseOptions(instance != null ? instance.ItemUid : 0);
            if (instance == null) return result;

            var rolled = ResolveRolledOptions(instance);
            if (rolled.Count > 0)
                result.AddRange(rolled);
            return result;
        }
    }
}
