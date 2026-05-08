using System.IO;
using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Save.FileName, menuName = ConfigScriptableObject.Save.MenuName, order = ConfigScriptableObject.Save.Ordering)]
    public class GGemCoSaveSettings: ScriptableObject
    {
        [Header("세이브 데이터 기본 설정")]
        [Tooltip("세이브 데이터를 사용할지 여부를 설정합니다.")]
        public bool useSaveData;

        [Tooltip("세이브 데이터를 저장할 폴더 이름입니다.")]
        public string saveDataFolderName;

        [Tooltip("저장 슬롯의 최대 개수입니다. UI 디자인을 고려하여 적절히 설정하세요.")]
        public int saveDataMaxSlotCount;

        [Tooltip("세이브 데이터 썸네일을 저장할 폴더 이름입니다.")]
        [SerializeField] private string saveDataThumbnailFolderName;

        [Tooltip("저장될 썸네일의 가로 크기(px)입니다. 0으로 설정하면 썸네일을 생성하지 않습니다.")]
        public int saveDataThumbnailWidth;

        [Header("세이브 타이밍 설정")]
        [Tooltip("자동 저장 요청 시 대기 시간(초). " +
                 "대기 중 새로운 요청이 들어오면 기존 요청은 취소되고 다시 대기합니다.")]
        public float saveDataDelay;

        [Tooltip("강제 저장이 수행되는 최소 간격(초)입니다.")]
        public float saveDataForceSaveInterval;

        [Header("세이브 데이터 암호화 설정")]
        [Tooltip("세이브 데이터 파일 암호화 적용 방식을 설정합니다.")]
        public SaveDataEncryptionMode saveDataEncryptionMode = SaveDataEncryptionMode.OptionalMigration;

        [Tooltip("Android Keystore에서 사용할 저장 데이터 암호화 키 별칭입니다.")]
        public string saveDataEncryptionKeyAlias = SaveDataCryptoService.DefaultKeyAlias;
        
        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            useSaveData = false;
            saveDataFolderName = "SaveData";
            saveDataMaxSlotCount = 3;
            saveDataThumbnailFolderName = "SaveThumbnails";
            saveDataDelay = 1f;
            saveDataForceSaveInterval = 30f;
            saveDataEncryptionMode = SaveDataEncryptionMode.OptionalMigration;
            saveDataEncryptionKeyAlias = SaveDataCryptoService.DefaultKeyAlias;
        }
        
        public string SaveDataFolderName => Path.Combine(Application.persistentDataPath, saveDataFolderName);
        public string SaveDataThumnailFolderName => Path.Combine(Application.persistentDataPath, saveDataThumbnailFolderName);
        public bool UseSaveData => useSaveData;

        /// <summary>
        /// 저장 데이터 암호화 적용 방식입니다.
        /// </summary>
        public SaveDataEncryptionMode SaveDataEncryptionMode => saveDataEncryptionMode;

        /// <summary>
        /// 플랫폼 보안 저장소에서 사용할 저장 데이터 암호화 키 별칭입니다.
        /// </summary>
        public string SaveDataEncryptionKeyAlias
        {
            get
            {
                string alias = saveDataEncryptionKeyAlias == null ? string.Empty : saveDataEncryptionKeyAlias.Trim();
                return string.IsNullOrEmpty(alias) ? SaveDataCryptoService.DefaultKeyAlias : alias;
            }
        }
    }
}
