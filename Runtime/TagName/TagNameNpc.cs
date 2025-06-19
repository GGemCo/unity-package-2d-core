using UnityEngine;

namespace GGemCo.Scripts
{
    public class TagNameNpc : DefaultTagName
    {
        private Npc npc;
        private StruckTableNpc struckTableNpc;

        public void Initialize(GameObject itemObject)
        {
            if (itemObject == null || TableLoaderManager.Instance == null) return;
            npc = itemObject.GetComponent<Npc>();
            struckTableNpc = TableLoaderManager.Instance.TableNpc.GetDataByUid(npc.uid);
            string nameFunction = "";
            if (struckTableNpc.InteractionUid > 0)
            {
                var info = TableLoaderManager.Instance.TableInteraction.GetDataByUid(struckTableNpc.InteractionUid);
                if (info != null && info.Type1 != InteractionConstants.Type.None)
                {
                    nameFunction = $" - {InteractionConstants.GetTypeName(info.Type1)}";
                }
            }

            textName.text = $"[ {struckTableNpc.Name}{nameFunction} ]";
            ApplyTextEffect();
        }

        private void LateUpdate()
        {
            if (npc == null || npc.gameObject == null) return;
            // 아이템 위 월드 좌표 설정
            Vector3 npcNameWorldPosition = npc.gameObject.transform.position + new Vector3(0, npc.GetHeightByScale(), 0) + diffTextPosition;
            gameObject.transform.position = npcNameWorldPosition;
        }
    }
}