using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 스킬 윈도우 - 스킬 리스트 element
    /// </summary>
    public class UIElementItemCraft : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler 
    {
        [Header("기본 속성")]
        [Tooltip("아이콘 위치")]
        public Vector3 iconPosition;
        [Tooltip("아이템 이름")]
        public TextMeshProUGUI textName;
        
        private UIWindowItemCraft uiWindowItemCraft;
        private UIWindowItemInfo uiWindowItemInfo;
        private StruckTableItemCraft struckTableItemCraft;
        private TableItemCraft tableItemCraft;
        private int slotIndex;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="puiWindowItemCraft"></param>
        /// <param name="pslotIndex"></param>
        /// <param name="pstruckTableItemCraft"></param>
        public void Initialize(UIWindowItemCraft puiWindowItemCraft, int pslotIndex, StruckTableItemCraft pstruckTableItemCraft)
        {
            slotIndex = pslotIndex;
            struckTableItemCraft = pstruckTableItemCraft;

            uiWindowItemCraft = puiWindowItemCraft;
            uiWindowItemInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(
                    UIWindowManager.WindowUid.ItemInfo);
            tableItemCraft = TableLoaderManager.Instance.TableItemCraft;
            
            UpdateInfos(pstruckTableItemCraft);
        }

        /// <summary>
        /// slotIndex 로 아이템 정보를 가져온다.
        /// SaveDataIcon 정보에 따라 버튼 visible 업데이트
        /// </summary>
        public void UpdateInfos(StruckTableItemCraft pstruckTableItemCraft)
        {
            struckTableItemCraft = pstruckTableItemCraft;
            if (struckTableItemCraft == null)
            {
                GcLogger.LogError($"제작 테이블에 없는 아이템 입니다. struckTableItemCraft is null");
                return;
            }

            var info = TableLoaderManager.Instance.TableItem.GetDataByUid(struckTableItemCraft.ResultItemUid);
            if (info == null)
            {
                GcLogger.LogError("item 테이블에 정보가 없습니다. item Uid:" + struckTableItemCraft.ResultItemUid);
                return;
            }
            if (textName != null) textName.text = info.Name;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            uiWindowItemInfo.SetItemUid(struckTableItemCraft.ResultItemUid, gameObject,
                UIWindowItemInfo.PositionType.None, uiWindowItemCraft.containerIcon.cellSize, new Vector2(0, 1f),
                new Vector2(
                    transform.position.x + uiWindowItemCraft.containerIcon.cellSize.x / 2f,
                    transform.position.y + uiWindowItemCraft.containerIcon.cellSize.y / 2f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            uiWindowItemInfo.Show(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            uiWindowItemCraft.textCraftResult.gameObject.SetActive(false);
            uiWindowItemCraft.SetInfo(struckTableItemCraft.Uid);
        }
        
        public Vector3 GetIconPosition() => iconPosition;
    }
}