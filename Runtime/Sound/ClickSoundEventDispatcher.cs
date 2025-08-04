using System;

namespace GGemCo2DCore
{
    public static class ClickSoundEventDispatcher
    {
        public static event Action<IClickSoundEventTrigger> OnClickDispatched;

        public static void Dispatch(IClickSoundEventTrigger trigger)
        {
            OnClickDispatched?.Invoke(trigger);
        }
    }
}