using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class CharacterRegenData
    {
        public int Uid;
        public int MapUid;
        public float x, y, z;
        public bool IsFlip;
        public bool DefaultVisible;
        public int MoveStep;
        public int MoveSpeed;

        public CharacterRegenData(int uid, Vector3 position, bool flip, int mapUid, bool defaultVisible, int moveStep = 0, int moveSpeed = 0)
        {
            Uid = uid;
            MapUid = mapUid;
            x = position.x;
            y = position.y;
            z = position.z;
            IsFlip = flip;
            DefaultVisible = defaultVisible;
            MoveStep = moveStep;
            MoveSpeed = moveSpeed;
        }
    }
    
    [System.Serializable]
    public class CharacterRegenDataList
    {
        public List<CharacterRegenData> CharacterRegenDatas = new List<CharacterRegenData>();
    }
}