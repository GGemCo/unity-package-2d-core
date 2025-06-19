using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 맵 테이블 Structure
    /// </summary>
    public class StruckTableEffect
    {
        public int Uid;
        public string Name;
        public string PrefabPath;
        public GameObject Prefab;
        public int Width;
        public int Height;
        public Vector2 ColliderSize;
        public bool NeedRotation;
        public string Color;
    }
    /// <summary>
    /// 맵 테이블
    /// </summary>
    public class TableEffect : DefaultTable
    {
        public StruckTableEffect GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableEffect
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                PrefabPath = data["PrefabPath"],
                Prefab = LoadPrefab(data["PrefabPath"]),
                Width = int.Parse(data["Width"]),
                Height = int.Parse(data["Height"]),
                ColliderSize = ConvertVector2(data["ColliderSize"]),
                NeedRotation = ConvertBoolean(data["NeedRotation"]),
                Color = data["Color"],
            };
        }
        public GameObject GetPrefab(int uid) {
            var info = GetDataByUid(uid);
            if (info == null) return null;
            return info.Prefab;
        }
    }
}