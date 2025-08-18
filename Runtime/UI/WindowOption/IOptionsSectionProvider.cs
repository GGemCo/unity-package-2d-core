using UnityEngine;

namespace GGemCo2DCore
{
    public interface IOptionsSectionProvider
    {
        string SectionId { get; }         // 예: "controls"
        string DisplayName { get; }       // 예: "키 설정"
        int Order { get; }                // 정렬용
        UnityEngine.GameObject BuildSection(Transform parent, UIWindowOption uiWindowOption);
        void OnOpen();
        void OnClose();
    }
}