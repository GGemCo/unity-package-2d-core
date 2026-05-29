using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 json 파일 로드
    /// </summary>
    public class SaveDataLoaderBase : MonoBehaviour
    {
        private int _maxSlotCount;
        private string _saveDirectory;
        protected SaveFileController saveFileController;

        private float _loadProgress;

        /// <summary>
        /// 마지막 저장 데이터 로드와 복구 처리 결과입니다.
        /// </summary>
        public SaveDataLoadResult LastLoadResult { get; private set; }

        protected virtual void Awake()
        {
            if (!AddressableLoaderSettings.Instance) return;
            _maxSlotCount = AddressableLoaderSettings.Instance.saveSettings.saveDataMaxSlotCount;
            _saveDirectory = AddressableLoaderSettings.Instance.saveSettings.SaveDataFolderName;
            saveFileController = new SaveFileController(_saveDirectory, _maxSlotCount);
        }

        protected virtual string GetSaveFilePath(int slotIndex)
        {
            return saveFileController.GetSaveFilePath(slotIndex);
        }

        /// <summary>
        /// 로더가 사용하는 백업 저장 파일 경로를 반환합니다.
        /// 기본 구현은 Core 표준 백업 파일 경로를 사용하며,
        /// 파생 로더는 전용 파일명 정책에 맞게 override 할 수 있습니다.
        /// </summary>
        /// <param name="slotIndex">로드할 저장 슬롯 번호입니다.</param>
        /// <returns>백업 저장 파일 경로입니다.</returns>
        protected virtual string GetBackupFilePath(int slotIndex)
        {
            return saveFileController.GetBackupFilePath(slotIndex);
        }

        /// <summary>
        /// 현재 로더가 읽는 저장 데이터의 논리 식별자를 반환합니다.
        /// </summary>
        /// <param name="slotIndex">로드할 저장 슬롯 번호입니다.</param>
        /// <returns>암호화 AAD 구성에 사용할 논리 저장 식별자입니다.</returns>
        protected virtual SaveDataIdentity GetSaveDataIdentity(int slotIndex)
        {
            return SaveDataIdentity.Core(slotIndex);
        }

        /// <summary>
        /// JSON 파일을 읽고, 실패 시 백업 복구 또는 신규 데이터 생성을 위한 상태를 기록합니다.
        /// </summary>
        /// <param name="onProgressUpdate">로드 진행률 갱신 콜백입니다.</param>
        public IEnumerator LoadData(Action<float> onProgressUpdate)
        {
            int slotIndex = PlayerPrefsManager.LoadSaveDataSlotIndex();
            string primaryPath = null;
            string backupPath = null;
            if (saveFileController != null && saveFileController.IsValidSlot(slotIndex))
            {
                // 유효 슬롯에서만 파일 경로를 계산해, 잘못된 슬롯(예: 0)일 때
                // 불필요한 슬롯 디렉터리 생성 부작용을 방지합니다.
                primaryPath = GetSaveFilePath(slotIndex);
                backupPath = GetBackupFilePath(slotIndex);
            }

            _loadProgress = 0.2f;
            onProgressUpdate?.Invoke(_loadProgress);
            yield return null;

            LastLoadResult = SaveDataRecoveryService.LoadWithRecovery(
                saveFileController,
                slotIndex,
                GetSaveDataIdentity(slotIndex),
                primaryPath,
                backupPath);

            _loadProgress = 0.6f;
            onProgressUpdate?.Invoke(_loadProgress);
            yield return null;

            if (LastLoadResult.HasJson)
            {
                try
                {
                    OnLoaded(LastLoadResult.Json);
                }
                catch (Exception ex)
                {
                    GcLogger.LogError($"[SaveDataLoader] Failed to deserialize {ex}");
                    LastLoadResult = new SaveDataLoadResult(
                        SaveDataLoadStatus.NewDataRequired,
                        userMessageKey: SaveDataMessageKeys.CannotLoadSaveData,
                        shouldShowUserMessage: true);
                    OnLoadFailed(LastLoadResult);
                }
            }
            else
            {
                OnLoadFailed(LastLoadResult);
            }

            _loadProgress = 1f;
            onProgressUpdate?.Invoke(_loadProgress);
            RequestUserMessageIfNeeded();
        }

        /// <summary>
        /// 저장 데이터 JSON을 실제 런타임 데이터로 변환합니다.
        /// </summary>
        /// <param name="json">역직렬화에 사용할 평문 JSON입니다.</param>
        protected virtual void OnLoaded(string json)
        {
        }

        /// <summary>
        /// 저장 데이터 로드 실패 또는 신규 데이터 생성 필요 상태를 하위 클래스에 전달합니다.
        /// </summary>
        /// <param name="result">로드와 복구 처리 결과입니다.</param>
        protected virtual void OnLoadFailed(SaveDataLoadResult result)
        {
        }

        /// <summary>
        /// 로드 결과에 사용자 안내 메시지가 있으면 이벤트 허브로 전달합니다.
        /// </summary>
        private void RequestUserMessageIfNeeded()
        {
            if (LastLoadResult == null || !LastLoadResult.ShouldShowUserMessage)
            {
                return;
            }

            SaveDataLoadNotificationCenter.RequestMessage(LastLoadResult.UserMessageKey);
        }
    }
}
