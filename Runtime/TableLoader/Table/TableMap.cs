using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 테이블 Structure
    /// </summary>
    public class StruckTableMap : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public MapConstants.Type Type;
        public MapConstants.SubType Subtype;
        public string FolderName;
        public Vector2 PlayerSpawnPosition;
        public int PlayerDeadSpawnUid;
        public int BgmUid;
    }
    /// <summary>
    /// 맵 테이블
    /// </summary>
    public class TableMap : DefaultTable<StruckTableMap>
    {
        public override string Key => ConfigAddressableTable.Map;
        protected override StruckTableMap BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableMap
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data["Name"],
                Type = EnumHelper.ConvertEnum<MapConstants.Type>(data["Type"]),
                Subtype = EnumHelper.ConvertEnum<MapConstants.SubType>(data["Subtype"]),
                FolderName = data["FolderName"],
                PlayerSpawnPosition = ConvertPlayerSpawnPosition(data["PlayerSpawnPosition"]),
                PlayerDeadSpawnUid = MathHelper.ParseInt(data["PlayerDeadSpawnUid"]),
                BgmUid = MathHelper.ParseInt(data["BgmUid"]),
            };
        }

        private Vector2 ConvertPlayerSpawnPosition(string position)
        {
            Vector2 playerSpawnPosition = new Vector2(0, 0);
            if (position != "")
            {
                var result2 = position.Split(",");
                playerSpawnPosition.x = float.Parse(result2[0]);
                playerSpawnPosition.y = float.Parse(result2[1]);
            }
            return playerSpawnPosition;
        }
    }
}