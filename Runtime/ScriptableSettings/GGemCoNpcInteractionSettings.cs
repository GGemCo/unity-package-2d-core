using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// NPC 인터랙션 대화 넘기기 입력 정책입니다.
    /// </summary>
    public enum InteractionDialogueAdvanceInputPolicy
    {
        /// <summary>
        /// 화면 클릭 또는 터치로 다음 페이지를 진행합니다.
        /// </summary>
        PointerClickOrTap = 0,
    }

    /// <summary>
    /// NPC 인터랙션 대사 표시 정책입니다.
    /// </summary>
    public enum InteractionDialogueRevealPolicy
    {
        /// <summary>
        /// 대사 페이지를 한 번에 모두 표시합니다.
        /// </summary>
        Instant = 0,

        /// <summary>
        /// 대사 페이지를 한 글자씩 표시합니다.
        /// </summary>
        Typewriter = 1,
    }

    /// <summary>
    /// 타자 효과 도중 클릭했을 때의 처리 정책입니다.
    /// </summary>
    public enum InteractionDialogueTypewriterClickPolicy
    {
        /// <summary>
        /// 현재 페이지 타자 효과를 중단하고 즉시 다음 페이지로 이동합니다.
        /// 마지막 페이지에서는 현재 페이지 전체를 노출합니다.
        /// </summary>
        SkipCurrentPageAndAdvance = 0,

        /// <summary>
        /// 첫 클릭은 현재 페이지 전체를 노출하고,
        /// 다음 클릭에서 다음 페이지로 이동합니다.
        /// </summary>
        RevealCurrentPageThenWaitNextClick = 1,
    }

    /// <summary>
    /// 대화 페이지 표시 정책 묶음입니다.
    /// </summary>
    [System.Serializable]
    public struct InteractionDialoguePageSettings
    {
        [Tooltip("한 번에 표시할 최대 줄 수입니다.")]
        public int maxLinesPerPage;

        [Tooltip("페이지를 넘길 입력 정책입니다.")]
        public InteractionDialogueAdvanceInputPolicy advanceInputPolicy;
    }

    /// <summary>
    /// 인터랙션 대화 시작 시 UI 표시 정책입니다.
    /// </summary>
    [System.Serializable]
    public struct InteractionDialogueUiSettings
    {
        [Tooltip("대화 시작 시 InteractionDialogue를 제외한 다른 UI를 닫을지 여부입니다.")]
        public bool hideOtherUiOnStart;
    }

    /// <summary>
    /// 인터랙션 대사 표시 연출 정책입니다.
    /// </summary>
    [System.Serializable]
    public struct InteractionDialogueRevealSettings
    {
        [Tooltip("대사 표시 연출 방식입니다.")]
        public InteractionDialogueRevealPolicy revealPolicy;

        [Tooltip("타자 효과일 때 초당 표시할 글자 수입니다.")]
        public float charactersPerSecond;

        [Tooltip("타자 효과 도중 클릭했을 때의 처리 정책입니다.")]
        public InteractionDialogueTypewriterClickPolicy typewriterClickPolicy;

        [Tooltip("대사 표시 연출에 TimeScale 영향을 받지 않는 시간을 사용할지 여부입니다.")]
        public bool useUnscaledTime;
    }

    /// <summary>
    /// NPC 인터랙션 대화 공통 설정입니다.
    /// </summary>
    [CreateAssetMenu(fileName = ConfigScriptableObject.NpcInteraction.FileName, menuName = ConfigScriptableObject.NpcInteraction.MenuName, order = ConfigScriptableObject.NpcInteraction.Ordering)]
    public class GGemCoNpcInteractionSettings : ScriptableObject
    {
        [Header("실행")]
        [Tooltip("선택지가 하나일 때 자동 시작 여부")]
        public bool autoStartWhenOneChoice;

        [Header("대화 페이지")]
        [Tooltip("대화 페이지 표시 정책입니다.")]
        public InteractionDialoguePageSettings page;

        [Header("UI")]
        [Tooltip("인터랙션 UI 표시 정책입니다.")]
        public InteractionDialogueUiSettings ui;

        [Header("대사 연출")]
        [Tooltip("대사 연출 정책입니다.")]
        public InteractionDialogueRevealSettings reveal;

        /// <summary>
        /// 런타임에서 Addressables 설정이 준비되지 않았을 때 사용할 기본 설정 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>기본값이 적용된 런타임용 설정 인스턴스입니다.</returns>
        public static GGemCoNpcInteractionSettings CreateRuntimeDefault()
        {
            GGemCoNpcInteractionSettings instance = CreateInstance<GGemCoNpcInteractionSettings>();
            instance.hideFlags = HideFlags.DontSave;
            instance.ApplyDefaultValues();
            return instance;
        }

        /// <summary>
        /// 페이지당 최대 줄 수를 1 이상으로 보정해서 반환합니다.
        /// </summary>
        /// <returns>보정된 페이지당 최대 줄 수입니다.</returns>
        public int GetSafeMaxLinesPerPage()
        {
            return Mathf.Max(1, page.maxLinesPerPage);
        }

        /// <summary>
        /// 타자 효과 초당 글자 수를 1 이상으로 보정해서 반환합니다.
        /// </summary>
        /// <returns>보정된 초당 글자 수입니다.</returns>
        public float GetSafeCharactersPerSecond()
        {
            return Mathf.Max(1f, reveal.charactersPerSecond);
        }

        /// <summary>
        /// 에셋이 처음 생성될 때 기본값을 적용합니다.
        /// </summary>
        private void Reset()
        {
            ApplyDefaultValues();
        }

        /// <summary>
        /// Inspector 값이 수정될 때 안전한 범위로 보정합니다.
        /// </summary>
        private void OnValidate()
        {
            page.maxLinesPerPage = Mathf.Max(1, page.maxLinesPerPage);
            reveal.charactersPerSecond = Mathf.Max(1f, reveal.charactersPerSecond);
        }

        /// <summary>
        /// NPC 인터랙션 설정의 기본값을 적용합니다.
        /// </summary>
        private void ApplyDefaultValues()
        {
            autoStartWhenOneChoice = true;

            page.maxLinesPerPage = 3;
            page.advanceInputPolicy = InteractionDialogueAdvanceInputPolicy.PointerClickOrTap;

            ui.hideOtherUiOnStart = true;

            reveal.revealPolicy = InteractionDialogueRevealPolicy.Instant;
            reveal.charactersPerSecond = 30f;
            reveal.typewriterClickPolicy = InteractionDialogueTypewriterClickPolicy.SkipCurrentPageAndAdvance;
            reveal.useUnscaledTime = true;
        }
    }
}
