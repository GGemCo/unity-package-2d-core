using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables 종속성 다운로드를 공통으로 관리하는 워밍 서비스입니다.
    /// </summary>
    /// <remarks>
    /// 실제 에셋 객체를 메모리에 올리지 않고, 지정한 키/라벨의 번들 종속성만 미리 준비합니다.
    /// </remarks>
    public sealed class AddressableDependencyWarmupService : MonoBehaviour
    {
        /// <summary>
        /// 전역 워밍 서비스 인스턴스입니다.
        /// </summary>
        public static AddressableDependencyWarmupService Instance { get; private set; }

        private readonly Dictionary<string, WarmupRecord> _records = new Dictionary<string, WarmupRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _groupProgress = new Dictionary<string, float>(StringComparer.Ordinal);

        /// <summary>
        /// 서비스 인스턴스를 가져오거나 없으면 새 게임 오브젝트에 생성합니다.
        /// </summary>
        /// <returns>사용 가능한 워밍 서비스 인스턴스입니다.</returns>
        public static AddressableDependencyWarmupService GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            AddressableDependencyWarmupService found = CompatObjectFind.FindFirst<AddressableDependencyWarmupService>();
            if (found != null)
                return found;

            return new GameObject(nameof(AddressableDependencyWarmupService)).AddComponent<AddressableDependencyWarmupService>();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            ReleaseAll();
        }

        /// <summary>
        /// 지정한 키 또는 라벨의 Addressables 종속성을 다운로드합니다.
        /// </summary>
        /// <param name="keyOrLabel">다운로드할 Addressables 키 또는 라벨입니다.</param>
        /// <returns>다운로드가 완료되면 true, 실패하면 false를 반환합니다.</returns>
        public Task<bool> WarmupAsync(string keyOrLabel)
        {
            if (string.IsNullOrWhiteSpace(keyOrLabel))
                return Task.FromResult(false);

            if (_records.TryGetValue(keyOrLabel, out WarmupRecord record))
            {
                if (record.State == WarmupState.Succeeded)
                    return Task.FromResult(true);

                if (record.State == WarmupState.Running && record.Task != null)
                    return record.Task;
            }

            record = new WarmupRecord();
            _records[keyOrLabel] = record;
            record.Task = ExecuteWarmupAsync(keyOrLabel, record);
            return record.Task;
        }

        /// <summary>
        /// 여러 키 또는 라벨을 순차적으로 워밍하고 그룹 진행률을 기록합니다.
        /// </summary>
        /// <param name="keysOrLabels">다운로드할 Addressables 키 또는 라벨 목록입니다.</param>
        /// <param name="groupId">진행률 조회에 사용할 그룹 식별자입니다.</param>
        /// <returns>모든 항목이 성공하면 true를 반환합니다.</returns>
        public async Task<bool> WarmupManyAsync(IReadOnlyList<string> keysOrLabels, string groupId)
        {
            if (keysOrLabels == null || keysOrLabels.Count == 0)
            {
                SetGroupProgress(groupId, 1f);
                return true;
            }

            bool allSucceeded = true;
            for (int i = 0; i < keysOrLabels.Count; i++)
            {
                string key = keysOrLabels[i];
                Task<bool> task = WarmupAsync(key);

                while (!task.IsCompleted)
                {
                    float itemProgress = GetProgress(key);
                    SetGroupProgress(groupId, ((float)i + itemProgress) / keysOrLabels.Count);
                    await Task.Yield();
                }

                if (task.IsCanceled || task.IsFaulted || !task.Result)
                    allSucceeded = false;

                SetGroupProgress(groupId, (float)(i + 1) / keysOrLabels.Count);
            }

            SetGroupProgress(groupId, 1f);
            return allSucceeded;
        }

        /// <summary>
        /// 지정한 키 또는 라벨의 워밍 완료 여부를 확인합니다.
        /// </summary>
        /// <param name="keyOrLabel">확인할 Addressables 키 또는 라벨입니다.</param>
        /// <returns>성공적으로 준비된 상태이면 true를 반환합니다.</returns>
        public bool IsPrepared(string keyOrLabel)
        {
            return !string.IsNullOrWhiteSpace(keyOrLabel)
                   && _records.TryGetValue(keyOrLabel, out WarmupRecord record)
                   && record.State == WarmupState.Succeeded;
        }

        /// <summary>
        /// 지정한 키 또는 라벨의 현재 워밍 진행률을 반환합니다.
        /// </summary>
        /// <param name="keyOrLabel">진행률을 확인할 Addressables 키 또는 라벨입니다.</param>
        /// <returns>0~1 범위의 진행률입니다.</returns>
        public float GetProgress(string keyOrLabel)
        {
            if (string.IsNullOrWhiteSpace(keyOrLabel))
                return 0f;

            if (!_records.TryGetValue(keyOrLabel, out WarmupRecord record))
                return 0f;

            if (record.State == WarmupState.Succeeded)
                return 1f;

            if (record.Handle.IsValid())
                return Mathf.Clamp01(record.Handle.PercentComplete);

            return 0f;
        }

        /// <summary>
        /// 지정한 그룹의 현재 워밍 진행률을 반환합니다.
        /// </summary>
        /// <param name="groupId">진행률을 확인할 그룹 식별자입니다.</param>
        /// <returns>0~1 범위의 진행률입니다.</returns>
        public float GetGroupProgress(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return 0f;

            return _groupProgress.TryGetValue(groupId, out float value)
                ? Mathf.Clamp01(value)
                : 0f;
        }

        /// <summary>
        /// 워밍 작업 핸들을 모두 해제합니다.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (WarmupRecord record in _records.Values)
            {
                if (record.Handle.IsValid())
                    Addressables.Release(record.Handle);
            }

            _records.Clear();
            _groupProgress.Clear();
        }

        /// <summary>
        /// 단일 종속성 다운로드를 수행하고 상태를 기록합니다.
        /// </summary>
        /// <param name="keyOrLabel">다운로드할 Addressables 키 또는 라벨입니다.</param>
        /// <param name="record">진행 상태를 저장할 레코드입니다.</param>
        /// <returns>다운로드 성공 여부입니다.</returns>
        private static async Task<bool> ExecuteWarmupAsync(string keyOrLabel, WarmupRecord record)
        {
            record.State = WarmupState.Running;

            bool hasLocation = await HasAnyLocationAsync(keyOrLabel);
            if (!hasLocation)
            {
                record.State = WarmupState.Failed;
                record.Error = $"Addressables Location 미존재: key={keyOrLabel}";
                GcLogger.LogWarning($"[AddressableWarmup] 키/라벨에 해당하는 Location이 없어 워밍업을 건너뜁니다. key={keyOrLabel}");
                return false;
            }

            try
            {
                record.Handle = Addressables.DownloadDependenciesAsync(keyOrLabel, false);
            }
            catch (InvalidKeyException ex)
            {
                record.State = WarmupState.Failed;
                record.Error = ex.Message;
                GcLogger.LogWarning($"[AddressableWarmup] InvalidKeyException으로 워밍업을 건너뜁니다. key={keyOrLabel}, error={record.Error}");
                return false;
            }
            catch (Exception ex)
            {
                record.State = WarmupState.Failed;
                record.Error = ex.Message;
                GcLogger.LogWarning($"[AddressableWarmup] 예외로 워밍업을 건너뜁니다. key={keyOrLabel}, error={record.Error}");
                return false;
            }

            while (!record.Handle.IsDone)
                await Task.Yield();

            if (record.Handle.Status == AsyncOperationStatus.Succeeded)
            {
                record.State = WarmupState.Succeeded;
                return true;
            }

            record.State = WarmupState.Failed;
            record.Error = record.Handle.OperationException != null
                ? record.Handle.OperationException.Message
                : "원인을 알 수 없는 Addressables 종속성 다운로드 실패";

            GcLogger.LogWarning($"[AddressableWarmup] 종속성 다운로드에 실패했습니다. key={keyOrLabel}, error={record.Error}");
            return false;
        }

        /// <summary>
        /// 지정한 키/라벨로 조회되는 Addressables Location 존재 여부를 확인합니다.
        /// </summary>
        /// <param name="keyOrLabel">검증할 Addressables 키 또는 라벨입니다.</param>
        /// <returns>조회 가능한 Location이 하나 이상이면 true를 반환합니다.</returns>
        private static async Task<bool> HasAnyLocationAsync(string keyOrLabel)
        {
            AsyncOperationHandle<IList<IResourceLocation>> locationHandle = default;

            try
            {
                locationHandle = Addressables.LoadResourceLocationsAsync(keyOrLabel);
                await locationHandle.Task;

                if (locationHandle.Status != AsyncOperationStatus.Succeeded)
                    return false;

                return locationHandle.Result != null && locationHandle.Result.Count > 0;
            }
            catch (InvalidKeyException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (locationHandle.IsValid())
                    Addressables.Release(locationHandle);
            }
        }

        private void SetGroupProgress(string groupId, float progress)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return;

            _groupProgress[groupId] = Mathf.Clamp01(progress);
        }

        private enum WarmupState
        {
            None,
            Running,
            Succeeded,
            Failed
        }

        private sealed class WarmupRecord
        {
            public AsyncOperationHandle Handle;
            public WarmupState State = WarmupState.None;
            public Task<bool> Task;
            public string Error;
        }
    }
}
