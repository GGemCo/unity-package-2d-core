using System.Collections.Generic;
using TMPro;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인터랙션 대화 페이지 진행 요청 결과입니다.
    /// </summary>
    public enum InteractionDialogueAdvanceResult
    {
        /// <summary>
        /// 처리할 변경이 없습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 현재 페이지 전체를 즉시 노출했습니다.
        /// </summary>
        RevealedCurrentPage = 1,

        /// <summary>
        /// 다음 페이지로 이동했습니다.
        /// </summary>
        MovedToNextPage = 2,
    }

    /// <summary>
    /// 인터랙션 대사 페이지 분할, 타자 효과, 페이지 이동을 전담합니다.
    /// </summary>
    public sealed class InteractionDialogueMessagePlayer
    {
        private readonly List<string> _pages = new();
        private readonly DialogueTextRevealPlayer _revealPlayer = new();
        private GGemCoNpcInteractionSettings _settings;
        private int _currentPageIndex;

        /// <summary>
        /// 현재 표시 중인 메시지 페이지가 존재하는지 여부입니다.
        /// </summary>
        public bool HasMessage => _pages.Count > 0;

        /// <summary>
        /// 현재 페이지가 모두 표시되었는지 여부입니다.
        /// </summary>
        public bool IsCurrentPageFullyRevealed => _revealPlayer.IsFullyRevealed;

        /// <summary>
        /// 현재 설정이 타자 효과 모드인지 여부입니다.
        /// </summary>
        public bool IsTypewriterMode =>
            _settings != null && _settings.reveal.revealPolicy == InteractionDialogueRevealPolicy.Typewriter;

        /// <summary>
        /// 전체 메시지 시퀀스가 끝까지 표시되었는지 여부입니다.
        /// </summary>
        public bool IsSequenceCompleted =>
            !HasMessage || (_currentPageIndex >= _pages.Count - 1 && _revealPlayer.IsFullyRevealed);

        /// <summary>
        /// 현재 표시 상태를 초기화하고 새 메시지를 페이지 단위로 바인딩합니다.
        /// </summary>
        /// <param name="target">텍스트 출력 대상입니다.</param>
        /// <param name="message">원본 메시지입니다.</param>
        /// <param name="settings">인터랙션 대화 설정입니다.</param>
        public void Configure(TextMeshProUGUI target, string message, GGemCoNpcInteractionSettings settings)
        {
            _settings = settings != null ? settings : GGemCoNpcInteractionSettings.CreateRuntimeDefault();
            _pages.Clear();
            _currentPageIndex = 0;
            _revealPlayer.Clear(target);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _pages.AddRange(DialogueTextFormatter.SplitMessage(message, _settings.GetSafeMaxLinesPerPage()));
            ApplyCurrentPage(target);
        }

        /// <summary>
        /// 현재 메시지 표시 상태를 완전히 초기화합니다.
        /// </summary>
        /// <param name="target">초기화할 텍스트 출력 대상입니다.</param>
        public void Clear(TextMeshProUGUI target)
        {
            _pages.Clear();
            _currentPageIndex = 0;
            _revealPlayer.Clear(target);
        }

        /// <summary>
        /// 타자 효과가 진행 중이면 deltaTime 만큼 글자 노출을 갱신합니다.
        /// </summary>
        /// <param name="target">텍스트 출력 대상입니다.</param>
        /// <param name="deltaTime">누적할 시간값입니다.</param>
        public void Tick(TextMeshProUGUI target, float deltaTime)
        {
            if (target == null || !HasMessage)
            {
                return;
            }

            _revealPlayer.Tick(target, deltaTime);
        }

        /// <summary>
        /// 클릭 또는 터치 입력에 따라 현재 페이지를 공개하거나 다음 페이지로 이동합니다.
        /// </summary>
        /// <param name="target">텍스트 출력 대상입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        public InteractionDialogueAdvanceResult Advance(TextMeshProUGUI target)
        {
            if (target == null || !HasMessage)
            {
                return InteractionDialogueAdvanceResult.None;
            }

            if (!_revealPlayer.IsFullyRevealed)
            {
                return AdvanceWhileRevealing(target);
            }

            if (!HasNextPage())
            {
                return InteractionDialogueAdvanceResult.None;
            }

            _currentPageIndex++;
            ApplyCurrentPage(target);
            return InteractionDialogueAdvanceResult.MovedToNextPage;
        }

        /// <summary>
        /// 현재 페이지를 즉시 모두 노출합니다.
        /// </summary>
        /// <param name="target">텍스트 출력 대상입니다.</param>
        public void RevealCurrentPage(TextMeshProUGUI target)
        {
            if (target == null)
            {
                return;
            }

            _revealPlayer.RevealAll(target);
        }

        /// <summary>
        /// 아직 타자 효과가 끝나지 않았을 때 클릭 정책에 맞춰 페이지를 처리합니다.
        /// </summary>
        /// <param name="target">텍스트 출력 대상입니다.</param>
        /// <returns>처리 결과입니다.</returns>
        private InteractionDialogueAdvanceResult AdvanceWhileRevealing(TextMeshProUGUI target)
        {
            bool canMoveNextPage = HasNextPage();
            if (_settings != null &&
                _settings.reveal.typewriterClickPolicy == InteractionDialogueTypewriterClickPolicy.SkipCurrentPageAndAdvance &&
                canMoveNextPage)
            {
                _currentPageIndex++;
                ApplyCurrentPage(target);
                return InteractionDialogueAdvanceResult.MovedToNextPage;
            }

            RevealCurrentPage(target);
            return InteractionDialogueAdvanceResult.RevealedCurrentPage;
        }

        /// <summary>
        /// 현재 페이지 텍스트를 출력 대상에 반영하고 표시 상태를 초기화합니다.
        /// </summary>
        /// <param name="target">텍스트 출력 대상입니다.</param>
        private void ApplyCurrentPage(TextMeshProUGUI target)
        {
            if (target == null)
            {
                return;
            }

            string pageText = GetCurrentPageText();
            _revealPlayer.Configure(target, pageText, IsTypewriterMode, _settings.GetSafeCharactersPerSecond());
        }

        /// <summary>
        /// 현재 페이지 텍스트를 반환합니다.
        /// </summary>
        /// <returns>현재 페이지 문자열입니다.</returns>
        private string GetCurrentPageText()
        {
            if (!HasMessage || _currentPageIndex < 0 || _currentPageIndex >= _pages.Count)
            {
                return string.Empty;
            }

            return _pages[_currentPageIndex];
        }

        /// <summary>
        /// 다음 페이지가 남아 있는지 여부를 반환합니다.
        /// </summary>
        /// <returns>다음 페이지가 있으면 true입니다.</returns>
        private bool HasNextPage()
        {
            return HasMessage && _currentPageIndex < _pages.Count - 1;
        }
    }
}
