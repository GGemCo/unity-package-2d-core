using UnityEngine;

namespace GGemCo2DCore
{
    public class TagNameItem : DefaultTagName
    {
        private Item item;
        private StruckTableItem struckTableItem;

        /// <summary>
        /// 기존 int 수량 기반 호출부와 호환되도록 이름 태그를 초기화합니다.
        /// </summary>
        /// <param name="itemObject">이름 태그가 추적할 드랍 아이템 오브젝트입니다.</param>
        /// <param name="itemCount">표시할 아이템 수량입니다.</param>
        public void Initialize(GameObject itemObject, int itemCount)
        {
            Initialize(itemObject, (long)itemCount);
        }

        /// <summary>
        /// 드랍 아이템과 표시 수량을 사용해 이름 태그를 초기화합니다.
        /// </summary>
        /// <param name="itemObject">이름 태그가 추적할 드랍 아이템 오브젝트입니다.</param>
        /// <param name="itemCount">표시할 아이템 수량입니다.</param>
        public void Initialize(GameObject itemObject, long itemCount)
        {
            if (itemObject == null || TableLoaderManager.Instance == null) return;
            item = itemObject.GetComponent<Item>();
            struckTableItem = TableLoaderManager.Instance.GetItemData(item.GetItemUid());
            textName.text = $"{ItemDisplayNameUtility.GetDisplayName(struckTableItem)} ({itemCount})";
            ApplyTextEffect();
        }

        protected override void ApplyTextEffect()
        {
            // 텍스트 색상 및 효과 설정
            switch (struckTableItem.Type)
            {
                case ItemConstants.Type.Equip:
                case ItemConstants.Type.None:
                case ItemConstants.Type.Consumable:
                default:
                    textName.color = color;
                    textName.fontSize = fontSize;
                    break;
            }
        }
        private void LateUpdate()
        {
            // 아이템 위 월드 좌표 설정
            Vector3 npcNameWorldPosition = item.gameObject.transform.position + diffTextPosition;
            gameObject.transform.position = npcNameWorldPosition;
        }
    }
}
