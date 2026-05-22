using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// TextMeshPro 텍스트의 타자 효과 상태를 관리하는 공통 플레이어입니다.
    /// </summary>
    public sealed class DialogueTextRevealPlayer
    {
        private bool _useTypewriter;
        private float _charactersPerSecond;
        private float _visibleCharacterCount;
        private int _totalCharacterCount;

        /// <summary>
        /// 현재 텍스트가 모두 노출되었는지 여부입니다.
        /// </summary>
        public bool IsFullyRevealed { get; private set; } = true;

        /// <summary>
        /// 현재 노출 방식이 타자 효과 모드인지 여부입니다.
        /// </summary>
        public bool IsTypewriterMode => _useTypewriter;

        /// <summary>
        /// 출력 대상에 새 메시지를 설정하고 타자 효과 상태를 초기화합니다.
        /// </summary>
        /// <param name="target">텍스트를 출력할 대상 컴포넌트입니다.</param>
        /// <param name="message">출력할 메시지입니다.</param>
        /// <param name="useTypewriter">타자 효과를 사용할지 여부입니다.</param>
        /// <param name="charactersPerSecond">타자 효과일 때 초당 노출할 글자 수입니다.</param>
        public void Configure(TextMeshProUGUI target, string message, bool useTypewriter, float charactersPerSecond)
        {
            _useTypewriter = useTypewriter;
            _charactersPerSecond = GetSafeCharactersPerSecond(charactersPerSecond);
            _visibleCharacterCount = 0f;
            _totalCharacterCount = 0;
            IsFullyRevealed = true;

            if (target == null)
            {
                return;
            }

            target.text = message ?? string.Empty;
            target.ForceMeshUpdate();

            _totalCharacterCount = Mathf.Max(0, target.textInfo.characterCount);
            if (!_useTypewriter || _totalCharacterCount <= 0)
            {
                RevealAll(target);
                return;
            }

            IsFullyRevealed = false;
            target.maxVisibleCharacters = 0;
        }

        /// <summary>
        /// 타자 효과 진행 중일 때 경과 시간만큼 표시 글자 수를 갱신합니다.
        /// </summary>
        /// <param name="target">텍스트를 출력할 대상 컴포넌트입니다.</param>
        /// <param name="deltaTime">이번 프레임의 경과 시간입니다.</param>
        public void Tick(TextMeshProUGUI target, float deltaTime)
        {
            if (target == null || IsFullyRevealed || !_useTypewriter)
            {
                return;
            }

            _visibleCharacterCount += _charactersPerSecond * Mathf.Max(0f, deltaTime);
            int nextVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(_visibleCharacterCount), 0, _totalCharacterCount);
            target.maxVisibleCharacters = nextVisibleCharacters;

            if (nextVisibleCharacters >= _totalCharacterCount)
            {
                RevealAll(target);
            }
        }

        /// <summary>
        /// 현재 메시지의 모든 글자를 즉시 노출합니다.
        /// </summary>
        /// <param name="target">텍스트를 출력할 대상 컴포넌트입니다.</param>
        public void RevealAll(TextMeshProUGUI target)
        {
            _visibleCharacterCount = _totalCharacterCount;
            IsFullyRevealed = true;

            if (target == null)
            {
                return;
            }

            target.maxVisibleCharacters = int.MaxValue;
        }

        /// <summary>
        /// 출력 대상과 내부 타자 효과 상태를 초기화합니다.
        /// </summary>
        /// <param name="target">초기화할 텍스트 출력 대상 컴포넌트입니다.</param>
        public void Clear(TextMeshProUGUI target)
        {
            _useTypewriter = false;
            _charactersPerSecond = 1f;
            _visibleCharacterCount = 0f;
            _totalCharacterCount = 0;
            IsFullyRevealed = true;

            if (target == null)
            {
                return;
            }

            target.text = string.Empty;
            target.maxVisibleCharacters = int.MaxValue;
        }

        /// <summary>
        /// 초당 글자 수가 1 미만으로 내려가지 않도록 보정합니다.
        /// </summary>
        /// <param name="charactersPerSecond">검사할 초당 글자 수입니다.</param>
        /// <returns>1 이상으로 보정된 초당 글자 수입니다.</returns>
        private static float GetSafeCharactersPerSecond(float charactersPerSecond)
        {
            return Mathf.Max(1f, charactersPerSecond);
        }
    }
}
