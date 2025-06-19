using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 세이브 데이터 불러오기 Window
    /// </summary>
    public class UIWindowLoadSaveData : MonoBehaviour
    {
        [Header("기본오브젝트")]
        [Tooltip("세이브 데이터를 보여줄 슬롯 Prefab")] [SerializeField] private GameObject elementSaveDataSlot;
        [Tooltip("슬롯 프리팹이 들어갈 Panel")] [SerializeField] private GameObject containerElementSaveDataSlot;
        [Tooltip("닫기 버튼")] [SerializeField] private Button buttonClose;
        [Tooltip("불러오기 버튼")] [SerializeField] private Button buttonLoad;
        [Tooltip("삭제하기 버튼")] [SerializeField] private Button buttonDelete;
        [Tooltip("팝업 매니저")] [SerializeField] private PopupManager popupManager;

        // 현재 선택된 slot index
        private int currentCheckSlotIndex;
        // UIElementSaveDataSlot 배열
        private List<UIElementSaveDataSlot> uiElementSaveDataSlots;
        private AddressableSettingsLoader addressableSettingsLoader;
        private SlotMetaDatController slotMetaDatController;
        
        // 델리게이트 선언
        public delegate void DelegateOnUpdateSlotData();
        // 델리게이트 이벤트 정의
        public event DelegateOnUpdateSlotData OnUpdateSlotData;
        
        private void Awake()
        {
            uiElementSaveDataSlots = new List<UIElementSaveDataSlot>();
            currentCheckSlotIndex = 0;
            InitButtons();
        }

        private void InitButtons()
        {
            buttonClose?.onClick.RemoveAllListeners();
            buttonLoad?.onClick.RemoveAllListeners();
            buttonDelete?.onClick.RemoveAllListeners();
            buttonClose?.onClick.AddListener(OnClickClose);
            buttonLoad?.onClick.AddListener(OnClickLoad);
            buttonDelete?.onClick.AddListener(OnClickDelete);
        }
        private void Start()
        {
            gameObject.SetActive(false);
        }
        /// <summary>
        /// Settings 에서 최대 슬롯 개수를 가져와 UIElementSaveDataSlot 을 만든다.
        /// </summary>
        public void InitializeSaveDataSlots(GGemCoSaveSettings saveSettings, SlotMetaDatController pslotMetaDatController)
        {
            if (elementSaveDataSlot == null || containerElementSaveDataSlot == null) return;
            int maxSlotCount = saveSettings.saveDataMaxSlotCount;
            slotMetaDatController = pslotMetaDatController;   
            List<SlotMetaInfo> slotMetaInfos = slotMetaDatController.GetMetaDataSlots();
            
            for (int i = 0; i < maxSlotCount; i++)
            {
                GameObject slot = Instantiate(elementSaveDataSlot, containerElementSaveDataSlot.transform);
                if (slot == null) continue;
                UIElementSaveDataSlot uiElementSaveDataSlot = slot.GetComponent<UIElementSaveDataSlot>();
                if (uiElementSaveDataSlot == null) continue;
                uiElementSaveDataSlots.Add(uiElementSaveDataSlot);
                SlotMetaInfo slotMetaInfo = slotMetaInfos[i];
                if (slotMetaInfo != null)
                {
                    bool isCheck = slotMetaInfo.SlotIndex == PlayerPrefsManager.LoadSaveDataSlotIndex();
                    if (isCheck)
                    {
                        currentCheckSlotIndex = slotMetaInfo.SlotIndex;
                    }
                    uiElementSaveDataSlot.Initialize(slotMetaInfo, isCheck, this);
                }
            }
        }
        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }

        private void OnClickClose()
        {
            Show(false);
        }
        /// <summary>
        /// 불러오기을 클릭하면 PlayerPrefs 에 선택한 슬롯 index 를 저장하고, 로딩씬으로 넘어간다. 
        /// </summary>
        private void OnClickLoad()
        {
            if (currentCheckSlotIndex <= 0)
            {
                GcLogger.LogError("선택된 슬롯이 없습니다.");
                popupManager.ShowPopupError("선택된 슬롯이 없습니다.");
                return;
            }
            // PlayerPrefs 에 선택한 슬롯 index 를 저장
            PlayerPrefsManager.SaveSaveDataSlotIndex(currentCheckSlotIndex);
            // 로딩씬으로 넘어간다. 
            SceneManager.ChangeScene(ConfigDefine.SceneNameLoading);
        }

        /// <summary>
        /// 삭제하기를 클릭하면 확인 팝업창이 뜨고, 확인을 클릭하면 선택된 슬롯을 삭제한다.
        /// </summary>
        private void OnClickDelete()
        {
            if (currentCheckSlotIndex <= 0)
            {
                GcLogger.LogError("선택된 슬롯이 없습니다.");
                popupManager.ShowPopupError("선택된 슬롯이 없습니다.");
                return;
            }

            PopupMetadata popupMetadata = new PopupMetadata
            {
                PopupType = PopupManager.Type.Default,
                MessageColor = Color.red,
                Title = "슬롯 삭제",
                Message = "삭제한 데이터는 복구할 수 없습니다.\n정말로 삭제하시겠습니까?",
                OnConfirm = DeleteElement,
                ShowCancelButton = true
            };
            popupManager.ShowPopup(popupMetadata);
        }
        /// <summary>
        /// 선택된 슬롯을 삭제한다.
        /// </summary>
        private void DeleteElement()
        {
            if (currentCheckSlotIndex <= 0)
            {
                GcLogger.LogError("선택된 슬롯이 없습니다.");
                popupManager.ShowPopupError("선택된 슬롯이 없습니다.");
                return;
            }

            string filePath = slotMetaDatController.GetFilePath(currentCheckSlotIndex);
            string thumbnailPath = slotMetaDatController.GetThumbnailFilePath(currentCheckSlotIndex);
            
            // 삭제 후 json 파일과 썸네일 파일을 삭제한다.
            if (File.Exists(filePath)) File.Delete(filePath);
            if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath);
            
            // 삭제한 element 는 비활성화 처리를 한다.
            slotMetaDatController.DeleteSlot(currentCheckSlotIndex);
            UIElementSaveDataSlot uiElementSaveDataSlot = GetCurrentUIElementSaveDataSlot();
            if (uiElementSaveDataSlot == null) return;
            uiElementSaveDataSlot.ClearInfo();
            uiElementSaveDataSlot.gameObject.SetActive(false);
            
            // 현재 선턱된 슬롯 index 와 PlayerPrefs 에 저장되어있는 값을 0 으로 초기화 시킨다.
            currentCheckSlotIndex = 0;
            PlayerPrefsManager.SaveSaveDataSlotIndex(0);
            // 인트로 씬의 새로운 게임 버튼을 활성화 시킨다.
            OnUpdateSlotData?.Invoke();
        }
        /// <summary>
        /// 현재 선택된 UIElementSaveDataSlot 가져오기
        /// </summary>
        /// <returns></returns>
        private UIElementSaveDataSlot GetCurrentUIElementSaveDataSlot()
        {
            return uiElementSaveDataSlots[currentCheckSlotIndex - 1];
        }
        /// <summary>
        /// element 클릭하면 현재 선택된 currentCheckSlotIndex 값을 업데이트 해준다.
        /// </summary>
        /// <param name="slotIndex"></param>
        public void SetSelectElement(int slotIndex)
        {
            if (uiElementSaveDataSlots.Count <= 0) return;
            if (slotIndex <= 0) return;
            // 체크 아이콘 모두 off
            foreach (var slot in uiElementSaveDataSlots)
            {
                slot.SetIconCheck(false);
            }
            // 선택된 currentCheckSlotIndex 값을 업데이트 해준다.
            currentCheckSlotIndex = slotIndex;
            // icon check 이미지를 활성화 시켜준다.
            uiElementSaveDataSlots[slotIndex-1]?.SetIconCheck(true);
        }
    }
}