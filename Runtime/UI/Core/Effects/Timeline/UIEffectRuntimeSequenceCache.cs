using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// ui_effect UID로 <see cref="UIEffectRuntimeSequence"/>를 로드하고 캐시하는 런타임 서비스입니다.
    /// </summary>
    public static class UIEffectRuntimeSequenceCache
    {
        private static readonly Dictionary<int, AsyncOperationHandle<UIEffectRuntimeSequence>> HandlesByUid = new();

        /// <summary>
        /// 이미 로드된 UI 효과 런타임 시퀀스를 조회합니다.
        /// </summary>
        /// <param name="uid">ui_effect 데이터 테이블 UID입니다.</param>
        /// <param name="sequence">로드된 런타임 시퀀스입니다.</param>
        /// <returns>시퀀스가 성공적으로 로드되어 있으면 <see langword="true"/>입니다.</returns>
        public static bool TryGetLoaded(int uid, out UIEffectRuntimeSequence sequence)
        {
            sequence = null;
            if (uid <= 0)
            {
                return false;
            }

            if (!HandlesByUid.TryGetValue(uid, out AsyncOperationHandle<UIEffectRuntimeSequence> handle))
            {
                return false;
            }

            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                return false;
            }

            sequence = handle.Result;
            return true;
        }

        /// <summary>
        /// ui_effect UID에 해당하는 런타임 시퀀스를 비동기로 로드합니다.
        /// </summary>
        /// <param name="uid">ui_effect 데이터 테이블 UID입니다.</param>
        /// <param name="onLoaded">로드 성공 시 호출할 콜백입니다.</param>
        /// <returns>Addressables 로드가 끝날 때까지 대기하는 코루틴입니다.</returns>
        public static IEnumerator LoadAsync(int uid, Action<UIEffectRuntimeSequence> onLoaded)
        {
            if (uid <= 0)
            {
                onLoaded?.Invoke(null);
                yield break;
            }

            if (!IsEnabledUIEffect(uid))
            {
                onLoaded?.Invoke(null);
                yield break;
            }

            if (TryGetLoaded(uid, out UIEffectRuntimeSequence loadedSequence))
            {
                onLoaded?.Invoke(loadedSequence);
                yield break;
            }

            if (!HandlesByUid.TryGetValue(uid, out AsyncOperationHandle<UIEffectRuntimeSequence> handle) || !handle.IsValid())
            {
                string key = ConfigAddressableKey.GetUIEffectRuntimeSequenceKey(uid);
                handle = Addressables.LoadAssetAsync<UIEffectRuntimeSequence>(key);
                HandlesByUid[uid] = handle;
            }

            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                onLoaded?.Invoke(handle.Result);
                yield break;
            }

            onLoaded?.Invoke(null);
        }

        /// <summary>
        /// 캐시된 모든 UI 효과 런타임 시퀀스 핸들을 해제합니다.
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (AsyncOperationHandle<UIEffectRuntimeSequence> handle in HandlesByUid.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            HandlesByUid.Clear();
        }

        /// <summary>
        /// ui_effect 테이블에 UID가 존재하고 활성화되어 있는지 확인합니다.
        /// </summary>
        /// <param name="uid">확인할 ui_effect UID입니다.</param>
        /// <returns>테이블 행이 없거나 비활성화되어 있으면 <see langword="false"/>입니다.</returns>
        private static bool IsEnabledUIEffect(int uid)
        {
            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            if (tableLoaderManager == null)
            {
                return true;
            }

            return tableLoaderManager.TryGetUIEffectData(uid, out StruckTableUIEffect row, false) && row != null && row.Enabled;
        }
    }
}
