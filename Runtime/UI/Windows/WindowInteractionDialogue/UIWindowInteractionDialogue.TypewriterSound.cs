namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowInteractionDialogue"/>의 말풍선 타자 효과음 수명주기를 담당하는 partial 스크립트입니다.
    /// </summary>
    public partial class UIWindowInteractionDialogue
    {
        private bool _useTypewriterSound;
        private int _typewriterSoundUid;
        private float _typewriterSoundPitchMultiplier =
            GGemCoDialogueBalloonSettings.DefaultTypewriterSoundPitchMultiplier;
        private SoundPlaybackHandle _typewriterSoundHandle;

        /// <summary>
        /// 프로젝트 전역 말풍선 설정에서 타자 효과음 옵션을 가져오고 이전 재생을 정리합니다.
        /// 새 메시지를 바인딩할 때마다 호출하여 설정 변경과 사운드 수명주기를 안전하게 반영합니다.
        /// </summary>
        private void ApplyProjectTypewriterSoundDefaults()
        {
            StopTypewriterSound();
            DialogueBalloonSettingsRuntimeResolver.ResolveTypewriterSoundDefaults(
                out _useTypewriterSound,
                out _typewriterSoundUid,
                out _typewriterSoundPitchMultiplier);
        }

        /// <summary>
        /// 말풍선 모드에서 현재 페이지의 타자 효과가 진행 중이면 효과음을 루프 재생합니다.
        /// 이미 재생 중인 핸들이 있으면 중복 재생하지 않습니다.
        /// </summary>
        private void TryStartTypewriterSound()
        {
            if (dialogueVisualMode != DialogueVisualMode.SpeechBubble ||
                !_useTypewriterSound ||
                _typewriterSoundUid <= 0 ||
                !_messagePlayer.IsTypewriterMode ||
                _messagePlayer.IsCurrentPageFullyRevealed ||
                (_typewriterSoundHandle != null && !_typewriterSoundHandle.IsStopped))
            {
                return;
            }

            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            SoundManager soundManager = sceneGame != null ? sceneGame.soundManager : null;
            if (soundManager == null)
            {
                return;
            }

            _typewriterSoundHandle = soundManager.PlayLoopingSfxByUidWithPitchMultiplier(
                _typewriterSoundUid,
                _typewriterSoundPitchMultiplier);
        }

        /// <summary>
        /// 자연스러운 타자 출력으로 현재 페이지가 이번 프레임에 완료되었으면 효과음을 정지합니다.
        /// </summary>
        /// <param name="wasCurrentPageFullyRevealed">갱신 직전 현재 페이지가 모두 표시되었는지 여부입니다.</param>
        private void StopTypewriterSoundWhenRevealCompleted(bool wasCurrentPageFullyRevealed)
        {
            if (dialogueVisualMode != DialogueVisualMode.SpeechBubble ||
                (!wasCurrentPageFullyRevealed && _messagePlayer.IsCurrentPageFullyRevealed))
            {
                StopTypewriterSound();
            }
        }

        /// <summary>
        /// 클릭 또는 터치로 페이지 상태가 바뀐 결과에 맞춰 타자 효과음을 동기화합니다.
        /// 현재 페이지를 즉시 공개하면 정지하고, 다음 페이지로 이동하면 새 페이지 기준으로 다시 시작합니다.
        /// </summary>
        /// <param name="result">인터랙션 대화 페이지 진행 결과입니다.</param>
        private void SynchronizeTypewriterSoundAfterAdvance(InteractionDialogueAdvanceResult result)
        {
            switch (result)
            {
                case InteractionDialogueAdvanceResult.RevealedCurrentPage:
                    StopTypewriterSound();
                    break;

                case InteractionDialogueAdvanceResult.MovedToNextPage:
                    StopTypewriterSound();
                    TryStartTypewriterSound();
                    break;
            }
        }

        /// <summary>
        /// 현재 타자 효과음 재생을 정지하고 핸들 참조를 해제합니다.
        /// 대화 종료나 페이지 전환 뒤 반복 사운드가 남지 않도록 보장합니다.
        /// </summary>
        private void StopTypewriterSound()
        {
            if (_typewriterSoundHandle == null)
            {
                return;
            }

            _typewriterSoundHandle.Stop();
            _typewriterSoundHandle = null;
        }

        /// <summary>
        /// 윈도우 GameObject가 비활성화될 때 남아 있는 타자 효과음을 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            StopTypewriterSound();
        }
    }
}
