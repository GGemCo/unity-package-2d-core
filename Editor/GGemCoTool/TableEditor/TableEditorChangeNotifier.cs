using System;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// TableEditor에서 테이블 저장이 완료된 사실을 다른 Editor 도구에 전달합니다.
    /// </summary>
    public static class TableEditorChangeNotifier
    {
        /// <summary>
        /// 테이블 파일 저장, AssetDatabase 재임포트 및 테이블 캐시 해제가 완료된 후 발생합니다.
        /// 이벤트 인수는 저장된 테이블의 고유 키입니다.
        /// </summary>
        public static event Action<string> TableSaved;

        /// <summary>
        /// 지정한 테이블의 저장 완료 이벤트를 발행합니다.
        /// </summary>
        /// <param name="tableKey">저장이 완료된 테이블의 고유 키입니다.</param>
        internal static void NotifyTableSaved(string tableKey)
        {
            if (string.IsNullOrWhiteSpace(tableKey))
            {
                return;
            }

            Action<string> handlers = TableSaved;
            if (handlers == null)
            {
                return;
            }

            Delegate[] invocationList = handlers.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<string>)invocationList[i]).Invoke(tableKey);
                }
                catch (Exception exception)
                {
                    // 한 Editor 도구의 갱신 실패가 테이블 저장 성공 자체를 실패로 바꾸지 않도록 구독자별로 격리합니다.
                    Debug.LogException(exception);
                }
            }
        }
    }
}
