using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    public class UIElementSaveDataSlot : MonoBehaviour
    {
        [Header("기본오브젝트")]
        [Tooltip("썸네일 이미지")]
        public Image imageThumbnail;
        [Tooltip("선택 표시 아이콘")]
        public Image iconCheck;
        [Tooltip("레벨")]
        public TextMeshProUGUI textLevel;
        [Tooltip("저장된 시간")]
        public TextMeshProUGUI textSaveDate;
        
        private UIWindowLoadSaveData uiWindowLoadSaveData;
        private int slotIndex;
        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SetElement);
        }
        public void Initialize(SlotMetaInfo slotMetaInfo, bool isCheck, UIWindowLoadSaveData puiWindowLoadSaveData)
        {
            uiWindowLoadSaveData = puiWindowLoadSaveData;
            slotIndex = slotMetaInfo.SlotIndex;
            iconCheck?.gameObject.SetActive(isCheck);
            if (imageThumbnail != null)
            {
                string thumbnailPath = slotMetaInfo.ThumbnailFilePath;
                if (File.Exists(thumbnailPath))
                {
                    byte[] fileData = File.ReadAllBytes(thumbnailPath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(fileData);
                    imageThumbnail.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            if (textLevel != null) textLevel.text = $"Lv.{slotMetaInfo.Level}";
            if (textSaveDate != null) textSaveDate.text = slotMetaInfo.SaveTime;
            
            gameObject.SetActive(slotMetaInfo.Exists);
        }
        /// <summary>
        /// 현재 element 를 선택하기
        /// </summary>
        private void SetElement()
        {
            uiWindowLoadSaveData?.SetSelectElement(slotIndex);
        }
        /// <summary>
        /// 체크 아이콘 on/off
        /// </summary>
        /// <param name="isCheck"></param>
        public void SetIconCheck(bool isCheck)
        {
            iconCheck?.gameObject.SetActive(isCheck);
        }

        public void ClearInfo()
        {
            imageThumbnail.sprite = null;
            textLevel.text = "빈 슬롯";
            textSaveDate.text = "";
            SetIconCheck(false);
        }
    }
}