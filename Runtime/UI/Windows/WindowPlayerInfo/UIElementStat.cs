using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// PlayerInfo 창에서 사용하는 스탯 라인 UI 엘리먼트
    /// - 스탯 값(총합), 투자 포인트, +/- 버튼을 한 줄로 묶는다.
    /// </summary>
    public class UIElementStat : MonoBehaviour
    {
        [Tooltip("표시할 스탯 총합 텍스트")]
        public TextMeshProUGUI textValue;

        [Tooltip("투자 포인트 텍스트")]
        public TextMeshProUGUI textInvested;

        [Tooltip("포인트 투자 버튼")]
        public Button buttonPlus;

        [Tooltip("포인트 회수 버튼")]
        public Button buttonMinus;

        private string _label;

        private CharacterConstants.IndexPlayerInfo _indexPlayerInfo;
        private UIWindowPlayerInfo _uiWindowPlayerInfo;
        private Player _boundPlayer;

        public void Initialize(UIWindowPlayerInfo uiWindowPlayerInfo, Player player, CharacterConstants.IndexPlayerInfo indexPlayerInfo)
        {
            _uiWindowPlayerInfo = uiWindowPlayerInfo;
            _indexPlayerInfo = indexPlayerInfo;
            _boundPlayer = player;

            InitializeStatPoint();
        }

        /// <summary>
        /// 라벨은 변경될 일이 거의 없으므로, 바인딩 시 1회만 세팅합니다.
        /// </summary>
        public void SetLabel(string label)
        {
            _label = label;
        }

        public string GetLabel() => _label;
        /// <summary>
        /// 스탯 포인트 투자 대상만 +/- 버튼과 투자 텍스트를 활성화합니다.
        /// </summary>
        private void InitializeStatPoint()
        {
            buttonPlus?.gameObject.SetActive(false);
            buttonMinus?.gameObject.SetActive(false);
            textInvested?.gameObject.SetActive(false);
            if (!CharacterConstants.IsStatPointTarget(_indexPlayerInfo)) return;
            
            buttonPlus?.gameObject.SetActive(true);
            buttonMinus?.gameObject.SetActive(true);
            textInvested?.gameObject.SetActive(true);

            if (buttonPlus != null) buttonPlus.onClick.AddListener(OnClickPlus);
            if (buttonMinus != null) buttonMinus.onClick.AddListener(OnClickMinus);
        }

        /// <summary>
        /// UIWindowPlayerInfo가 Player를 바인딩한 이후, UIElementStat에도 Player 참조를 주입합니다.
        /// (Awake에서 라인을 먼저 생성하는 구조이므로 반드시 필요)
        /// </summary>
        public void BindPlayer(Player player)
        {
            _boundPlayer = player;
        }

        private void OnDestroy()
        {
            if (buttonPlus != null) buttonPlus.onClick.RemoveAllListeners();
            if (buttonMinus != null) buttonMinus.onClick.RemoveAllListeners();
        }
        
        private void OnClickPlus()
        {
            _uiWindowPlayerInfo?.TryChangeDraft(_indexPlayerInfo, +1);
        }

        private void OnClickMinus()
        {
            _uiWindowPlayerInfo?.TryChangeDraft(_indexPlayerInfo, -1);
        }
    }
}