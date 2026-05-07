using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 위에 대사 말풍선을 표시하고 필요 시 타자 효과를 진행하는 UI 컴포넌트입니다.
    /// </summary>
    public class UIDialogueBalloon : MonoBehaviour
    {
        public TextMeshProUGUI textMessage;
        private readonly DialogueTextRevealPlayer _revealPlayer = new();
        private CharacterBase target;
        private Vector3 diffTextPosition;

        /// <summary>
        /// 말풍선 대상 캐릭터와 표시할 대사 데이터를 초기화합니다.
        /// </summary>
        /// <param name="characterBase">말풍선을 따라갈 대상 캐릭터입니다.</param>
        /// <param name="data">말풍선 메시지와 표시 옵션입니다.</param>
        public void Initialize(CharacterBase characterBase, DialogueBalloonData data)
        {
            target = characterBase;
            DialogueBalloonData safeData = data ?? new DialogueBalloonData();
            SetFontSize(safeData.fontSize);
            SetMessage(safeData);
        }

        /// <summary>
        /// 말풍선 텍스트의 폰트 크기를 적용합니다.
        /// </summary>
        /// <param name="size">적용할 폰트 크기입니다. 0 이하이면 현재 값을 유지합니다.</param>
        private void SetFontSize(float size)
        {
            if (textMessage == null) return;
            if (size <= 0) return;
            textMessage.fontSize = size;
        }

        /// <summary>
        /// 말풍선 메시지를 적용하고 타자 효과 상태를 초기화합니다.
        /// </summary>
        /// <param name="data">말풍선 메시지와 타자 효과 설정입니다.</param>
        private void SetMessage(DialogueBalloonData data)
        {
            if (textMessage == null) return;
            _revealPlayer.Configure(
                textMessage,
                data.message,
                data.useTypewriter,
                data.GetSafeTypewriterCharactersPerSecond());
        }

        /// <summary>
        /// 풀 반환이나 비활성화 시 남아 있는 메시지 노출 상태를 초기화합니다.
        /// </summary>
        private void OnDisable()
        {
            target = null;
            _revealPlayer.Clear(textMessage);
        }

        /// <summary>
        /// 매 프레임 타자 효과와 대상 캐릭터 추적 위치를 갱신합니다.
        /// </summary>
        private void LateUpdate()
        {
            _revealPlayer.Tick(textMessage, Time.deltaTime);

            if (target == null) return;
            // 아이템 위 월드 좌표 설정
            Vector3 npcNameWorldPosition = target.gameObject.transform.position + new Vector3(0, target.GetHeightByScale(), 0) + diffTextPosition;
            gameObject.transform.position = npcNameWorldPosition;
        }
    }
}
