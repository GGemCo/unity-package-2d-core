using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 화면 중앙에 Overlay 텍스트를 출력하고,
    /// 페이드 인/아웃 및 알파 값을 타임라인 기반으로 제어하는 컨트롤러입니다.
    /// </summary>
    public sealed class OverlayTextController : CutsceneDefaultController, ICutsceneController
    {
        private CutsceneOverlayPresenter _presenter;
        private OverlayTextData _data;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;
        private string _resolvedText = string.Empty;

        /// <summary>
        /// Overlay 텍스트 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        public OverlayTextController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// Overlay Presenter를 준비합니다.
        /// 없을 경우 생성하여 이후 텍스트 출력에 사용합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>비동기 준비 처리를 위한 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.OverlayText)
            {
                yield break;
            }

            _presenter = CutsceneManager.GetOrCreateOverlayPresenter();
            yield return null;
        }

        /// <summary>
        /// Overlay 텍스트를 설정하고 화면에 표시합니다.
        /// duration에 따라 즉시 표시 또는 페이드 기반 표시를 시작합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.OverlayText)
            {
                return;
            }

            _presenter ??= CutsceneManager.GetOrCreateOverlayPresenter();
            if (_presenter == null)
            {
                return;
            }

            _data = evt.overlayText ?? new OverlayTextData();

            // 표시할 텍스트 결정 (런타임 override 가능)
            _resolvedText = ResolveDisplayText(_data);

            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            // 텍스트 설정 및 표시
            _presenter.ConfigureOverlayText(_data, _resolvedText);
            _presenter.SetOverlayTextVisible(true);

            if (_duration <= 0f)
            {
                // 즉시 표시
                _presenter.SetOverlayTextAlpha(_data.maxAlpha);
                return;
            }

            // 초기 알파 설정 (fadeIn 여부에 따라)
            _presenter.SetOverlayTextAlpha(_data.fadeIn ? 0f : _data.maxAlpha);
        }

        /// <summary>
        /// 시간 경과에 따라 알파 값을 계산하여 Overlay 텍스트에 적용합니다.
        /// </summary>
        public void Update()
        {
            if (!_isPlaying || _presenter == null || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float alpha = EvaluateAlpha();
            _presenter.SetOverlayTextAlpha(alpha);

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// Overlay 텍스트 표시를 중지하고 설정에 따라 상태를 유지하거나 숨깁니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;

            if (_presenter == null || _data == null)
            {
                return;
            }

            if (_data.fadeOut)
            {
                // 완전히 사라지도록 처리
                _presenter.SetOverlayTextAlpha(0f);
                _presenter.SetOverlayTextVisible(false);
            }
            else
            {
                // 마지막 상태 유지
                _presenter.SetOverlayTextAlpha(_data.maxAlpha);
            }
        }

        /// <summary>
        /// 컷신 종료 시 텍스트를 숨기고 상태를 초기화합니다.
        /// </summary>
        public void End()
        {
            _isPlaying = false;
            _resolvedText = string.Empty;

            if (_presenter != null)
            {
                _presenter.SetOverlayTextVisible(false);
            }
        }

        /// <summary>
        /// 표시할 텍스트를 결정합니다.
        /// 런타임 override가 설정된 경우 해당 값을 우선 사용합니다.
        /// </summary>
        /// <param name="data">Overlay 텍스트 설정 데이터입니다.</param>
        /// <returns>최종 표시할 문자열입니다.</returns>
        private string ResolveDisplayText(OverlayTextData data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            switch (data.sourceMode)
            {
                case OverlayTextSourceMode.RuntimeOverride:
                    if (!string.IsNullOrWhiteSpace(data.runtimeTextKey) &&
                        CutsceneManager != null &&
                        CutsceneManager.TryGetOverlayTextOverride(data.runtimeTextKey, out string overrideText))
                    {
                        return overrideText ?? string.Empty;
                    }

                    if (!string.IsNullOrWhiteSpace(data.runtimeTextKey))
                    {
                        GcLogger.Log($"OverlayText runtime override not found. key={data.runtimeTextKey}");
                    }

                    return data.text ?? string.Empty;

                case OverlayTextSourceMode.Fixed:
                default:
                    return data.text ?? string.Empty;
            }
        }

        /// <summary>
        /// 현재 시간 진행도에 따라 알파 값을 계산합니다.
        /// fadeIn, fadeOut 옵션 조합에 따라 다양한 페이드 패턴을 지원합니다.
        /// </summary>
        /// <returns>계산된 알파 값입니다.</returns>
        private float EvaluateAlpha()
        {
            if (_duration <= 0f)
            {
                return _data.maxAlpha;
            }

            float normalized = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(normalized, _data.easing));

            // fadeIn + fadeOut (양쪽 페이드)
            if (_data.fadeIn && _data.fadeOut)
            {
                if (normalized < 0.2f)
                {
                    float t = Mathf.Clamp01(normalized / 0.2f);
                    return Mathf.Lerp(0f, _data.maxAlpha, Mathf.Clamp01(Easing.Apply(t, _data.easing)));
                }

                if (normalized > 0.8f)
                {
                    float t = Mathf.Clamp01((normalized - 0.8f) / 0.2f);
                    return Mathf.Lerp(_data.maxAlpha, 0f, Mathf.Clamp01(Easing.Apply(t, _data.easing)));
                }

                return _data.maxAlpha;
            }

            // fadeIn만
            if (_data.fadeIn)
            {
                return Mathf.Lerp(0f, _data.maxAlpha, eased);
            }

            // fadeOut만
            if (_data.fadeOut)
            {
                return Mathf.Lerp(_data.maxAlpha, 0f, eased);
            }

            // 고정 알파
            return _data.maxAlpha;
        }
    }
}