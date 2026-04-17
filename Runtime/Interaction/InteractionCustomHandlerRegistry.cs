using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임별 커스텀 인터렉션 핸들러 레지스트리입니다.
    /// customTypeKey 기준으로 상위 런타임이 핸들러를 등록합니다.
    /// </summary>
    public static class InteractionCustomHandlerRegistry
    {
        private static readonly Dictionary<string, IInteractionCustomHandler> Handlers =
            new(StringComparer.Ordinal);

        public static void Register(string customTypeKey, IInteractionCustomHandler handler)
        {
            if (string.IsNullOrWhiteSpace(customTypeKey))
            {
                throw new ArgumentException("Custom interaction key is required.", nameof(customTypeKey));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Handlers[customTypeKey] = handler;
        }

        public static void Unregister(string customTypeKey, IInteractionCustomHandler handler = null)
        {
            if (string.IsNullOrWhiteSpace(customTypeKey))
            {
                return;
            }

            if (handler == null)
            {
                Handlers.Remove(customTypeKey);
                return;
            }

            if (Handlers.TryGetValue(customTypeKey, out var current) && ReferenceEquals(current, handler))
            {
                Handlers.Remove(customTypeKey);
            }
        }

        public static bool TryGetHandler(string customTypeKey, out IInteractionCustomHandler handler)
        {
            handler = null;

            if (string.IsNullOrWhiteSpace(customTypeKey))
            {
                return false;
            }

            return Handlers.TryGetValue(customTypeKey, out handler) && handler != null;
        }

        public static bool TryGetDisplayName(string customTypeKey, int value, out string displayName)
        {
            displayName = string.Empty;

            if (!TryGetHandler(customTypeKey, out var handler))
            {
                return false;
            }

            return handler.TryGetDisplayName(value, out displayName) &&
                   string.IsNullOrWhiteSpace(displayName) == false;
        }

        public static bool TryExecute(string customTypeKey, SceneGame sceneGame, CharacterBase npc, int value)
        {
            if (!TryGetHandler(customTypeKey, out var handler))
            {
                return false;
            }

            return handler.TryExecute(sceneGame, npc, value);
        }
    }
}
