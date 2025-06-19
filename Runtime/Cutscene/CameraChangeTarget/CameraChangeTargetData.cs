using System;
using UnityEngine;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CameraChangeTargetData
    {
        [Header("캐릭터 타입")]
        public CharacterConstants.Type characterType;
        [Header("캐릭터 고유번호")] 
        public int characterUid;
    }
}