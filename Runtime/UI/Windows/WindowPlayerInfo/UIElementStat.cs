using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// PlayerInfo 창에서 사용하는 스탯 라인 UI 엘리먼트입니다.
    /// 값 계산은 외부에서 전달받고, UIElementStat는 표시/입력 처리만 담당합니다.
    /// </summary>
    public class UIElementStat : MonoBehaviour
    {
        [Tooltip("표시할 스탯 이름")]
        [SerializeField] private TextMeshProUGUI textName;
        [Tooltip("스탯 설명")]
        [SerializeField] private TextMeshProUGUI textDescription;

        [Tooltip("표시할 스탯 총합 텍스트")]
        [SerializeField] private TextMeshProUGUI textValue;

        [Tooltip("투자 포인트 텍스트")]
        [SerializeField] private TextMeshProUGUI textInvested;

        [Tooltip("포인트 투자 버튼")]
        [SerializeField] private Button buttonPlus;

        [Tooltip("포인트 회수 버튼")]
        [SerializeField] private Button buttonMinus;

        [Header("Formatting")]
        [Tooltip("값/증가량/투자 포인트 텍스트 표현 규칙")]
        [SerializeField] private UIElementStatFormatterAsset formatterAsset;

        private CharacterConstants.IndexPlayerInfo _indexPlayerInfo;
        private IStatPointDraftChangeHandler _draftChangeHandler;
        private EntityPlayerInfo _entityPlayerInfo;
        private string _label;

        /// <summary>
        /// 스탯 항목의 대상 정보와 초안 변경 핸들러를 초기화합니다.
        /// </summary>
        /// <param name="draftChangeHandler">증가/감소 버튼 입력을 처리할 초안 변경 핸들러입니다.</param>
        /// <param name="indexPlayerInfo">이 UI가 표현할 스탯 식별자입니다.</param>
        /// <param name="entityPlayerInfo">이 UI가 표현할 스탯 정보.</param>
        public void Initialize(IStatPointDraftChangeHandler draftChangeHandler, CharacterConstants.IndexPlayerInfo indexPlayerInfo, EntityPlayerInfo entityPlayerInfo)
        {
            _draftChangeHandler = draftChangeHandler;
            _indexPlayerInfo = indexPlayerInfo;
            _entityPlayerInfo = entityPlayerInfo;

            SetDescription();
            SetupStaticUi();
            RegisterListeners();
        }

        public void SetLabel(string label)
        {
            _label = label;
            if (textName != null)
                textName.text = label;
        }

        public string GetLabel() => _label;

        /// <summary>
        /// 포맷터 에셋을 런타임에 교체하고 즉시 다시 그릴 수 있도록 확장 포인트를 제공합니다.
        /// </summary>
        public void SetFormatter(UIElementStatFormatterAsset formatter)
        {
            formatterAsset = formatter;
        }

        public void Render(in UIElementStatRenderData data)
        {
            if (textName != null)
                textName.text = data.Label;

            if (textValue != null)
                textValue.text = FormatValue(data);

            if (textInvested != null)
                textInvested.text = FormatInvested(data);

            ApplyStatPointUiState(data);
        }

        private void SetupStaticUi()
        {
            bool isTarget = CharacterConstants.IsStatPointTarget(_indexPlayerInfo);

            if (buttonPlus != null)
                buttonPlus.gameObject.SetActive(isTarget);

            if (buttonMinus != null)
                buttonMinus.gameObject.SetActive(isTarget);

            if (textInvested != null)
                textInvested.gameObject.SetActive(isTarget);
        }

        private void RegisterListeners()
        {
            if (buttonPlus != null)
                buttonPlus.onClick.AddListener(OnClickPlus);

            if (buttonMinus != null)
                buttonMinus.onClick.AddListener(OnClickMinus);
        }

        private void OnDestroy()
        {
            if (buttonPlus != null)
                buttonPlus.onClick.RemoveListener(OnClickPlus);

            if (buttonMinus != null)
                buttonMinus.onClick.RemoveListener(OnClickMinus);
        }

        private void ApplyStatPointUiState(in UIElementStatRenderData data)
        {
            if (buttonPlus != null)
                buttonPlus.interactable = data.IsStatPointTarget && data.CanIncrease;

            if (buttonMinus != null)
                buttonMinus.interactable = data.IsStatPointTarget && data.CanDecrease;
        }

        private string FormatValue(in UIElementStatRenderData data)
        {
            if (formatterAsset != null)
                return formatterAsset.FormatValue(data);

            return data.IsChanged
                ? $"{data.CurrentValue} → {data.PreviewValue}"
                : data.CurrentValue.ToString();
        }

        private string FormatInvested(in UIElementStatRenderData data)
        {
            if (formatterAsset != null)
                return formatterAsset.FormatInvested(data);

            if (!data.IsStatPointTarget)
                return string.Empty;

            return data.InvestedDelta != 0
                ? $"(+{data.DraftInvested}, Δ{FormatSigned(data.InvestedDelta)})"
                : $"(+{data.DraftInvested})";
        }

        private static string FormatSigned(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }

        /// <summary>
        /// 증가 버튼 클릭 시 현재 스탯의 임시 투자 포인트를 1 증가시키도록 요청합니다.
        /// </summary>
        private void OnClickPlus()
        {
            _draftChangeHandler?.TryChangeDraft(_indexPlayerInfo, +1);
        }

        /// <summary>
        /// 감소 버튼 클릭 시 현재 스탯의 임시 투자 포인트를 1 회수하도록 요청합니다.
        /// </summary>
        private void OnClickMinus()
        {
            _draftChangeHandler?.TryChangeDraft(_indexPlayerInfo, -1);
        }

        private void SetDescription()
        {
            if (!textDescription || _entityPlayerInfo == null) return;
            textDescription.text = LocalizationManager.Instance.GetUIWindowPlayerInfoByKey(_entityPlayerInfo.localizationKeyDescription);
        }
    }
}
