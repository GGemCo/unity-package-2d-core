using UnityEngine;

namespace GGemCo.Scripts
{
    public interface IMap
    {
        MapConstants.Type Type { get; set; }
        MapConstants.SubType SubType { get; set; }
        int Uid { get; set; } // 현재 맵 uid
        float FadeDuration { get; set; }  // 페이드 인 지속 시간
        Vector3 PlaySpawnPosition { get; set; }
        
        void CreateMap();
    }
}