using TMPro;
using UnityEngine;

namespace GGemCo.Scripts.UI
{
    /// <summary>
    /// 말풍선
    /// </summary>
    public class UIDialogueBalloon : MonoBehaviour
    {
        public TextMeshProUGUI textMessage;
        private CharacterBase target;
        private Vector3 diffTextPosition;

        public void Initialize(CharacterBase characterBase, DialogueBalloonData data)
        {
            target = characterBase;
            SetMessage(data.message);
            SetFontSize(data.fontSize);
        }
        private void SetFontSize(float size)
        {
            if (textMessage == null) return;
            if (size <= 0) return;
            textMessage.fontSize = size;
        }
        private void SetMessage(string message)
        {
            if (textMessage == null) return;
            textMessage.text = message;
        }
        private void LateUpdate()
        {
            if (target == null) return;
            // 아이템 위 월드 좌표 설정
            Vector3 npcNameWorldPosition = target.gameObject.transform.position + new Vector3(0, target.GetHeightByScale(), 0) + diffTextPosition;
            gameObject.transform.position = npcNameWorldPosition;
        }
    }
}