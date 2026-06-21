using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 패키지가 캐릭터 표시 상태를 갱신할 수 있도록 콜백을 관리합니다.
    /// </summary>
    public static class CharacterPresentationRefreshRegistry
    {
        private static readonly List<Action<CharacterBase>> Handlers = new List<Action<CharacterBase>>();

        /// <summary>
        /// 캐릭터 표시 갱신 콜백을 등록합니다.
        /// </summary>
        /// <param name="handler">등록할 콜백입니다.</param>
        public static void Register(Action<CharacterBase> handler)
        {
            if (handler == null || Handlers.Contains(handler))
            {
                return;
            }

            Handlers.Add(handler);
        }

        /// <summary>
        /// 캐릭터 표시 갱신 콜백 등록을 해제합니다.
        /// </summary>
        /// <param name="handler">등록 해제할 콜백입니다.</param>
        public static void Unregister(Action<CharacterBase> handler)
        {
            if (handler == null)
            {
                return;
            }

            Handlers.Remove(handler);
        }

        /// <summary>
        /// 등록된 외부 패키지에 캐릭터 표시 갱신을 요청합니다.
        /// </summary>
        /// <param name="character">표시를 갱신할 캐릭터입니다.</param>
        public static void Refresh(CharacterBase character)
        {
            for (int i = 0; i < Handlers.Count; i++)
            {
                try
                {
                    Handlers[i]?.Invoke(character);
                }
                catch (Exception exception)
                {
                    GcLogger.LogException(exception);
                }
            }
        }
    }
}
