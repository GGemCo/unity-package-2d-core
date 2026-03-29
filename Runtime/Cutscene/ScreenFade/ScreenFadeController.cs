using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 화면 전체 페이드 효과를 제어하는 컨트롤러입니다.
    /// 페이드 색상, 시작/종료 알파값, easing 및 최종 상태 유지 여부를 처리합니다.
    /// </summary>
    public sealed class ScreenFadeController : CutsceneDefaultController, ICutsceneController
    {
        private ScreenFadePresenter _presenter;
        private ScreenFadeData _data;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;

        /// <summary>
        /// 화면 페이드 연출 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public ScreenFadeController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 화면 페이드 이벤트 실행 전 Presenter를 준비합니다.
        /// 이벤트 타입이 일치하지 않으면 즉시 종료합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>준비 과정을 비동기적으로 진행하기 위한 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.ScreenFade)
            {
                yield break;
            }

            _presenter = CutsceneManager.GetOrCreateScreenFadePresenter(evt.screenFade);
            yield return null;
        }

        /// <summary>
        /// 화면 페이드 이벤트를 시작하고 렌더 설정 및 초기 알파 상태를 적용합니다.
        /// duration이 0 이하이면 목표 알파를 즉시 적용합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.ScreenFade)
            {
                return;
            }

            _presenter = CutsceneManager.GetOrCreateScreenFadePresenter(evt.screenFade);
            if (_presenter == null)
            {
                return;
            }

            _data = evt.screenFade ?? new ScreenFadeData();
            _presenter.ApplyRenderSettings(_data, SceneGame.Instance);

            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            if (_duration <= 0f)
            {
                _presenter.SetFade(_data.color, _data.toAlpha, _data.toAlpha > 0f);
                return;
            }

            _presenter.SetFade(_data.color, _data.fromAlpha, true);
        }

        /// <summary>
        /// 시간 경과에 따라 페이드 알파를 보간하여 적용하고 완료 시 자동으로 종료합니다.
        /// </summary>
        public void Update()
        {
            if (!_isPlaying || _presenter == null || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float alpha = Mathf.Lerp(_data.fromAlpha, _data.toAlpha, eased);

            _presenter.SetFade(_data.color, alpha, true);

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 현재 페이드 진행을 중지하고 설정에 따라 최종 상태를 유지하거나 해제합니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;

            if (_presenter == null || _data == null)
            {
                return;
            }

            if (_data.holdFinalState)
            {
                _presenter.SetFade(_data.color, _data.toAlpha, _data.toAlpha > 0f);
            }
            else
            {
                _presenter.SetFade(_data.color, 0f, false);
            }
        }

        /// <summary>
        /// 컷신 종료 시 페이드 진행 상태를 종료합니다.
        /// </summary>
        public void End()
        {
            _isPlaying = false;
        }
    }
}