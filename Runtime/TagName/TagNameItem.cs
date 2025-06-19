using UnityEngine;

namespace GGemCo.Scripts
{
    public class TagNameItem : DefaultTagName
    {
        private Item item;
        private StruckTableItem struckTableItem;

        public void Initialize(GameObject itemObject, int itemCount)
        {
            if (itemObject == null || TableLoaderManager.Instance == null) return;
            item = itemObject.GetComponent<Item>();
            struckTableItem = TableLoaderManager.Instance.TableItem.GetDataByUid(item.itemUid);
            textName.text = $"{struckTableItem.Name} ({itemCount})";
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