using System;

namespace GGemCo2DCore
{
    public interface ISettingsChangeNotifier
    {
        event Action Changed;
        void RaiseChanged(); // 필요 시 수동 호출
    }
}