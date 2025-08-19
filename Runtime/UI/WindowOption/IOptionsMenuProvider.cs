using UnityEngine;

namespace GGemCo2DCore
{
    public interface IOptionsMenuProvider
    {
        string SectionId { get; }         // 예: "controls"
        string DisplayName { get; }       // 예: "키 설정"
        int Order { get; }                // 정렬용
        UnityEngine.GameObject BuildSection(Transform parent, UIWindowOption puiWindowOption);
        void OnOpen();
        void OnClose();
    }
}