using UnityEngine;

namespace GGemCo.Scripts
{
    public class SystemMessage
    {
        public SystemMessageManager.MessageType Type;
        public float Duration;
        public float FadeInTime;
        public float FadeOutTime;
        public Color TextColor;
        public int FontSize;

        public SystemMessage(SystemMessageManager.MessageType type, float duration, float fadeInTime, float fadeOutTime, 
            Color color, int fontSize)
        {
            Type = type;
            Duration = duration;
            FadeInTime = fadeInTime;
            FadeOutTime = fadeOutTime;
            TextColor = color;
            FontSize = fontSize;
        }
    }
    public class DefaultSystemMessage 
    {
    }
}
