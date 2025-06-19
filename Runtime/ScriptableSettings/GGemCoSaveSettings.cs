using System.IO;
using UnityEngine;

namespace GGemCo.Scripts
{
    [CreateAssetMenu(fileName = "GGemCoSaveSettings", menuName = "GGemCo/Settings/GGemCoSaveSettings", order = 4)]
    public class GGemCoSaveSettings: ScriptableObject
    {
        [Header("세이브 데이터 설정")] 
        [Header("세이브 데이터 사용여부")] [SerializeField] private bool useSaveData;
        [Header("세이브 데이터 자장할 폴더 이름 입니다.")] [SerializeField] private string saveDataFolderName;
        [Header("저장 슬롯 최대 개수. UI 를 고려하여 개수를 정해주세요.")] public int saveDataMaxSlotCount;
        [Header("세이브 데이터 썸네일을 자장할 폴더 이름 입니다.")] [SerializeField] private string saveDataThumbnailFolderName;
        [Header("썸네일의 width 크기(pixel). 0 이면 생성하지 않습니다.")] public int saveDataThumbnailWidth;

        [Header("저장 대기 시간")]
        [Tooltip("saveDataDelay 시간 후 저장 처리. 저장 처리되기전에 재요청이 오면 기존 요청은 취소되고 saveDataDelay 시간 동안 기다린다.(초)")]
        public float saveDataDelay;
        [Header("강제로 저장할 시간(초)")] public float saveDataForceSaveInterval;
        
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
        }
        
        public string SaveDataFolderName => Path.Combine(Application.persistentDataPath, saveDataFolderName);
        public string SaveDataThumnailFolderName => Path.Combine(Application.persistentDataPath, saveDataThumbnailFolderName);
        public bool UseSaveData => useSaveData;
    }
}