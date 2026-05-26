using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 로드 결과에 따른 사용자 안내 요청을 전달하는 이벤트 허브입니다.
    /// </summary>
    public static class SaveDataLoadNotificationCenter
    {
        /// <summary>
        /// 사용자 안내 메시지를 표시해야 할 때 발생합니다.
        /// </summary>
        public static event Action<string> MessageRequested;

        /// <summary>
        /// 사용자 안내 메시지 표시를 요청합니다.
        /// </summary>
        /// <param name="messageKey">로컬라이즈 또는 시스템 메시지에서 사용할 메시지 키입니다.</param>
        public static void RequestMessage(string messageKey)
        {
            if (string.IsNullOrEmpty(messageKey))
            {
                return;
            }

            MessageRequested?.Invoke(messageKey);
        }
    }
}
