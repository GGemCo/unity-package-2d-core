using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class VfxTableCollection
    {
        private readonly Dictionary<int, StruckTableVfx> _datas;

        public VfxTableCollection(Dictionary<int, StruckTableVfx> datas)
        {
            _datas = datas ?? new Dictionary<int, StruckTableVfx>();
        }

        public Dictionary<int, StruckTableVfx> GetDatas() => _datas;

        public IReadOnlyDictionary<int, StruckTableVfx> GetAll() => _datas;

        public StruckTableVfx GetDataByUid(int uid)
            => _datas.TryGetValue(uid, out var row) ? row : null;

        public bool TryGetDataByUid(int uid, out StruckTableVfx row)
            => _datas.TryGetValue(uid, out row);
    }
}
