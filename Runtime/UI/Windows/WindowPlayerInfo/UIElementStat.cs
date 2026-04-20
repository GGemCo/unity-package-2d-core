using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스탯 포인트 초안(draft) 변경 요청을 처리하는 핸들러를 정의합니다.
    /// </summary>
    public interface IStatPointDraftChangeHandler
    {
        /// <summary>
        /// 지정한 스탯의 임시 투자 포인트를 변경합니다.
        /// </summary>
        /// <param name="statType">변경할 대상 스탯 종류입니다.</param>
        /// <param name="delta">변경할 포인트 값입니다. 양수는 투자, 음수는 회수를 의미합니다.</param>
        /// <returns>변경이 성공하면 <see langword="true"/>, 조건 불충족 등으로 실패하면 <see langword="false"/>를 반환합니다.</returns>
        bool TryChangeDraft(CharacterConstants.IndexPlayerInfo statType, int delta);
    }

    /// <summary>
    /// 스탯 한 줄 UI를 렌더링하기 위한 표시용 데이터입니다.
    /// 현재 값, 미리보기 값, 투자 정보, 버튼 활성 상태를 함께 전달합니다.
    /// </summary>
    public readonly struct UIElementStatRenderData
    {
        /// <summary>
        /// 화면에 표시할 스탯 이름입니다.
        /// </summary>
        public readonly string Label;

        /// <summary>
        /// 현재 적용된 실제 스탯 값입니다.
        /// </summary>
        public readonly long CurrentValue;

        /// <summary>
        /// 미리보기 값 표시 여부를 나타냅니다.
        /// </summary>
        public readonly bool HasPreview;

        /// <summary>
        /// 임시 투자 반영 후의 미리보기 스탯 값입니다.
        /// </summary>
        public readonly long PreviewValue;

        /// <summary>
        /// 해당 스탯이 포인트 투자 대상인지 여부를 나타냅니다.
        /// </summary>
        public readonly bool IsStatPointTarget;

        /// <summary>
        /// 현재 초안에 누적 투자된 포인트입니다.
        /// </summary>
        public readonly int DraftInvested;

        /// <summary>
        /// 직전 상태 대비 투자 변화량입니다.
        /// </summary>
        public readonly int InvestedDelta;

        /// <summary>
        /// 증가 버튼 사용 가능 여부를 나타냅니다.
        /// </summary>
        public readonly bool CanIncrease;

        /// <summary>
        /// 감소 버튼 사용 가능 여부를 나타냅니다.
        /// </summary>
        public readonly bool CanDecrease;

        /// <summary>
        /// 스탯 UI 렌더링에 필요한 표시 데이터를 생성합니다.
        /// </summary>
        /// <param name="label">화면에 표시할 스탯 이름입니다.</param>
        /// <param name="currentValue">현재 적용된 실제 스탯 값입니다.</param>
        /// <param name="hasPreview">미리보기 값을 함께 표시할지 여부입니다.</param>
        /// <param name="previewValue">임시 투자 반영 후의 예상 스탯 값입니다.</param>
        /// <param name="isStatPointTarget">포인트 투자 대상 스탯인지 여부입니다.</param>
        /// <param name="draftInvested">현재 초안 기준 누적 투자 포인트입니다.</param>
        /// <param name="investedDelta">직전 상태 대비 투자 변화량입니다.</param>
        /// <param name="canIncrease">증가 버튼 활성 가능 여부입니다.</param>
        /// <param name="canDecrease">감소 버튼 활성 가능 여부입니다.</param>
        public UIElementStatRenderData(
            string label,
            long currentValue,
            bool hasPreview,
            long previewValue,
            bool isStatPointTarget,
            int draftInvested,
            int investedDelta,
            bool canIncrease,
            bool canDecrease)
        {
            Label = label;
            CurrentValue = currentValue;
            HasPreview = hasPreview;
            PreviewValue = previewValue;
            IsStatPointTarget = isStatPointTarget;
            DraftInvested = draftInvested;
            InvestedDelta = investedDelta;
            CanIncrease = canIncrease;
            CanDecrease = canDecrease;
        }
    }

    /// <summary>
    /// PlayerInfo 창에서 스탯 한 줄을 표시하는 UI 컴포넌트입니다.
    /// 스탯 이름, 현재 값, 투자 포인트, 증가/감소 버튼을 함께 관리합니다.
    /// </summary>
    public class UIElementStat : MonoBehaviour
    {
        [Tooltip("표시할 스탯 이름")]
        public TextMeshProUGUI textName;

        [Tooltip("표시할 스탯 총합 텍스트")]
        public TextMeshProUGUI textValue;

        [Tooltip("투자 포인트 텍스트")]
        public TextMeshProUGUI textInvested;

        [Tooltip("포인트 투자 버튼")]
        public Button buttonPlus;

        [Tooltip("포인트 회수 버튼")]
        public Button buttonMinus;

        private CharacterConstants.IndexPlayerInfo _indexPlayerInfo;
        private IStatPointDraftChangeHandler _draftChangeHandler;

        /// <summary>
        /// 스탯 항목의 대상 정보와 초안 변경 핸들러를 초기화합니다.
        /// </summary>
        /// <param name="draftChangeHandler">증가/감소 버튼 입력을 처리할 초안 변경 핸들러입니다.</param>
        /// <param name="indexPlayerInfo">이 UI가 표현할 스탯 식별자입니다.</param>
        public void Initialize(IStatPointDraftChangeHandler draftChangeHandler, CharacterConstants.IndexPlayerInfo indexPlayerInfo)
        {
            _draftChangeHandler = draftChangeHandler;
            _indexPlayerInfo = indexPlayerInfo;

            InitializeStatPoint();
        }

        /// <summary>
        /// 현재 스탯이 포인트 투자 대상인지 확인하고 관련 UI를 초기화합니다.
        /// 투자 대상인 경우에만 투자 텍스트와 +/- 버튼을 활성화하고 클릭 이벤트를 연결합니다.
        /// </summary>
        private void InitializeStatPoint()
        {
            bool isStatPointTarget = CharacterConstants.IsStatPointTarget(_indexPlayerInfo);

            buttonPlus?.gameObject.SetActive(isStatPointTarget);
            buttonMinus?.gameObject.SetActive(isStatPointTarget);
            textInvested?.gameObject.SetActive(isStatPointTarget);

            if (!isStatPointTarget) return;

            if (buttonPlus != null) buttonPlus.onClick.AddListener(OnClickPlus);
            if (buttonMinus != null) buttonMinus.onClick.AddListener(OnClickMinus);
        }

        /// <summary>
        /// 등록된 버튼 클릭 이벤트를 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (buttonPlus != null) buttonPlus.onClick.RemoveListener(OnClickPlus);
            if (buttonMinus != null) buttonMinus.onClick.RemoveListener(OnClickMinus);
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

        /// <summary>
        /// 전달받은 렌더링 데이터를 기준으로 스탯 라인 UI를 갱신합니다.
        /// 미리보기 값, 투자 포인트 텍스트, 버튼 활성 상태를 함께 반영합니다.
        /// </summary>
        /// <param name="renderData">현재 UI에 반영할 스탯 표시 데이터입니다.</param>
        public void Render(in UIElementStatRenderData renderData)
        {
            if (textName != null)
            {
                textName.text = renderData.Label;
            }

            if (textValue != null)
            {
                textValue.text = renderData.HasPreview
                    ? $"{renderData.CurrentValue} → {renderData.PreviewValue}"
                    : renderData.CurrentValue.ToString();
            }

            if (!renderData.IsStatPointTarget)
            {
                if (textInvested != null) textInvested.text = string.Empty;
                if (buttonPlus != null) buttonPlus.interactable = false;
                if (buttonMinus != null) buttonMinus.interactable = false;
                return;
            }

            if (textInvested != null)
            {
                textInvested.text = renderData.InvestedDelta != 0
                    ? $"(+{renderData.DraftInvested}, Δ{renderData.InvestedDelta:+#;-#;0})"
                    : $"(+{renderData.DraftInvested})";
            }

            if (buttonPlus != null) buttonPlus.interactable = renderData.CanIncrease;
            if (buttonMinus != null) buttonMinus.interactable = renderData.CanDecrease;
        }
    }
}