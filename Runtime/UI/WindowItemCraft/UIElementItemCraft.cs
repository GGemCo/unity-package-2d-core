using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GGemCo2DCore
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
        
        private UIWindowItemCraft _uiWindowItemCraft;
        private UIWindowItemInfo _uiWindowItemInfo;
        private StruckTableItemCraft _struckTableItemCraft;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="puiWindowItemCraft"></param>
        /// <param name="pslotIndex"></param>
        /// <param name="pstruckTableItemCraft"></param>
        public void Initialize(UIWindowItemCraft puiWindowItemCraft, int pslotIndex, StruckTableItemCraft pstruckTableItemCraft)
        {
            _struckTableItemCraft = pstruckTableItemCraft;

            _uiWindowItemCraft = puiWindowItemCraft;
            _uiWindowItemInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(
                    UIWindowConstants.WindowUid.ItemInfo);
            
            UpdateInfos(pstruckTableItemCraft);
        }

        /// <summary>
        /// slotIndex 로 아이템 정보를 가져온다.
        /// SaveDataIcon 정보에 따라 버튼 visible 업데이트
        /// </summary>
        public void UpdateInfos(StruckTableItemCraft pstruckTableItemCraft)
        {
            _struckTableItemCraft = pstruckTableItemCraft;
            if (_struckTableItemCraft == null)
            {
                GcLogger.LogError($"제작 테이블에 없는 아이템 입니다. struckTableItemCraft is null");
                return;
            }

            var info = TableLoaderManager.Instance.TableItem.GetDataByUid(_struckTableItemCraft.ResultItemUid);
            if (info == null)
            {
                GcLogger.LogError("item 테이블에 정보가 없습니다. item Uid:" + _struckTableItemCraft.ResultItemUid);
                return;
            }
            if (textName != null) textName.text = info.Name;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            _uiWindowItemInfo.SetItemUid(_struckTableItemCraft.ResultItemUid, gameObject,
                UIWindowItemInfo.PositionType.None, _uiWindowItemCraft.containerIcon.cellSize, new Vector2(0, 1f),
                new Vector2(
                    transform.position.x + _uiWindowItemCraft.containerIcon.cellSize.x / 2f,
                    transform.position.y + _uiWindowItemCraft.containerIcon.cellSize.y / 2f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _uiWindowItemInfo.Show(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _uiWindowItemCraft.textCraftResult.gameObject.SetActive(false);
            _uiWindowItemCraft.SetInfo(_struckTableItemCraft);
        }
        
        public Vector3 GetIconPosition() => iconPosition;
    }
}