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
        
        /// <summary>
        /// 테이블 데이터 1행이 로드된 직후 호출된다.
        /// </summary>
        /// <param name="data">로드된 어펙트 데이터.</param>
        /// <remarks>
        /// 로컬라이징 시스템이 존재하면 UID 기반으로 이름을 치환한다.
        /// 기존 방식과의 호환을 위해 로컬라이징이 없을 경우 Memo를 이름으로 사용한다.
        /// </remarks>
        protected override void OnLoadedData(StruckTableMap data)
        {
            if (data == null) return;

            // 기존 방식과의 호환: 로컬라이징 키가 비어있으면 uid 문자열을 사용한다.
            if (LocalizationManager.Instance != null)
            {
                data.Name = LocalizationManager.Instance.GetMapNameByKey($"{data.Uid}");
            }
            else
            {
                data.Name = $"{data.Name}";
            }
            
            if (AddressableLoaderSettings.Instance && AddressableLoaderSettings.Instance.mapSettings && 
                AddressableLoaderSettings.Instance.mapSettings.EnableMapUid)
            {
                data.Name += $" ({data.Uid})";
            }
        }
        
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