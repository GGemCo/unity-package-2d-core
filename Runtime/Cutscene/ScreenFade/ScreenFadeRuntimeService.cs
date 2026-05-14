using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 화면 전체 페이드 연출을 런타임에서 공용으로 실행하는 서비스입니다.
    /// Cutscene과 Skill 같은 서로 다른 시스템이 같은 Presenter를 공유하되, 소유자 우선순위와 출처 기준으로 충돌을 줄입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFadeRuntimeService : MonoBehaviour
    {
        private SceneGame _sceneGame;
        private ScreenFadePresenter _presenter;
        private ScreenFadeRequest _currentRequest;
        private ScreenFadeData _currentData;
        private float _elapsedSeconds;
        private bool _isPlaying;
        private bool _hasCurrentRequest;

        /// <summary>
        /// 현재 페이드가 시간 보간 중인지 여부입니다.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 현재 서비스가 보유한 요청 소유자입니다.
        /// </summary>
        public ScreenFadeOwner CurrentOwner => _hasCurrentRequest ? _currentRequest.owner : ScreenFadeOwner.None;

        /// <summary>
        /// SceneGame 기준으로 화면 페이드 서비스를 반환하거나 새로 생성합니다.
        /// </summary>
        /// <param name="sceneGame">서비스를 배치할 게임 씬 루트입니다.</param>
        /// <returns>사용 가능한 화면 페이드 서비스입니다. 씬 참조가 없으면 <see langword="null"/>을 반환합니다.</returns>
        public static ScreenFadeRuntimeService GetOrCreate(SceneGame sceneGame)
        {
            if (sceneGame == null)
            {
                sceneGame = SceneGame.Instance;
            }

            if (sceneGame == null)
            {
                GcLogger.LogError("Screen Fade Runtime Service를 만들기 위한 SceneGame 참조가 없습니다.");
                return null;
            }

            var service = sceneGame.GetComponentInChildren<ScreenFadeRuntimeService>(includeInactive: true);
            if (service == null)
            {
                var go = new GameObject(nameof(ScreenFadeRuntimeService));
                go.transform.SetParent(sceneGame.transform, false);
                service = go.AddComponent<ScreenFadeRuntimeService>();
            }

            service.Initialize(sceneGame);
            return service;
        }

        /// <summary>
        /// 서비스가 사용할 씬 참조와 Presenter를 준비합니다.
        /// </summary>
        /// <param name="sceneGame">현재 게임 씬 참조입니다.</param>
        public void Initialize(SceneGame sceneGame)
        {
            if (sceneGame == null)
                return;

            _sceneGame = sceneGame;
            EnsurePresenter(new ScreenFadeData(), resetPresentation: !_hasCurrentRequest);
        }

        /// <summary>
        /// 새 화면 페이드 요청을 재생합니다.
        /// 현재 요청과의 교체 정책을 확인한 뒤, 시작 알파와 렌더 설정을 즉시 적용합니다.
        /// </summary>
        /// <param name="request">재생할 화면 페이드 요청입니다.</param>
        /// <returns>요청을 수락하고 재생을 시작했으면 <see langword="true"/>입니다.</returns>
        public bool Play(ScreenFadeRequest request)
        {
            if (!CanAccept(request))
                return false;

            if (_sceneGame == null)
            {
                _sceneGame = SceneGame.Instance;
            }

            if (_sceneGame == null)
            {
                GcLogger.LogError("Screen Fade를 실행할 SceneGame 참조가 없습니다.");
                return false;
            }

            _currentRequest = NormalizeRequest(request);
            _currentData = _currentRequest.ToData();
            _hasCurrentRequest = true;
            _elapsedSeconds = 0f;

            EnsurePresenter(_currentData, resetPresentation: true);
            if (_presenter == null)
                return false;

            _presenter.ApplyRenderSettings(_currentData, _sceneGame);

            if (_currentRequest.durationSeconds <= 0f)
            {
                _isPlaying = false;
                _presenter.SetFade(_currentData.color, _currentData.toAlpha, _currentData.toAlpha > 0f);
                return true;
            }

            _isPlaying = true;
            _presenter.SetFade(_currentData.color, _currentData.fromAlpha, true);
            return true;
        }

        /// <summary>
        /// 현재 재생 중인 페이드를 특정 소유자와 출처가 일치할 때만 중지합니다.
        /// </summary>
        /// <param name="owner">중지할 요청 소유자입니다.</param>
        /// <param name="source">중지할 요청 출처입니다. null이면 소유자만 검사합니다.</param>
        /// <param name="forceClear">true이면 holdFinalState와 무관하게 알파 0으로 초기화합니다.</param>
        /// <returns>조건이 일치해 정리했으면 <see langword="true"/>입니다.</returns>
        public bool StopIfOwnedBy(ScreenFadeOwner owner, UnityEngine.Object source, bool forceClear)
        {
            if (!_hasCurrentRequest)
                return false;
            if (_currentRequest.owner != owner)
                return false;
            if (source != null && _currentRequest.source != source)
                return false;

            StopInternal(forceClear);
            return true;
        }

        /// <summary>
        /// 현재 페이드 상태를 강제로 초기화합니다.
        /// </summary>
        public void ResetPresentation()
        {
            _isPlaying = false;
            _hasCurrentRequest = false;
            _currentData = null;
            _presenter?.ResetPresentation();
        }

        /// <summary>
        /// 프레임마다 진행 중인 페이드의 알파를 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (!_isPlaying || _presenter == null || _currentData == null)
                return;

            _elapsedSeconds += _currentData.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float duration = Mathf.Max(0.0001f, _currentRequest.durationSeconds);
            float t = Mathf.Clamp01(_elapsedSeconds / duration);
            float eased = Mathf.Clamp01(Easing.Apply(t, _currentData.easing));
            float alpha = Mathf.Lerp(_currentData.fromAlpha, _currentData.toAlpha, eased);

            _presenter.SetFade(_currentData.color, alpha, true);

            if (_elapsedSeconds >= duration)
            {
                StopInternal(forceClear: false);
            }
        }

        /// <summary>
        /// 요청을 수락할 수 있는지 교체 정책과 소유자 우선순위를 기준으로 판단합니다.
        /// </summary>
        /// <param name="request">확인할 요청입니다.</param>
        /// <returns>요청을 수락할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool CanAccept(ScreenFadeRequest request)
        {
            if (!_hasCurrentRequest)
                return true;

            int currentPriority = GetOwnerPriority(_currentRequest.owner);
            int nextPriority = GetOwnerPriority(request.owner);

            switch (request.replaceMode)
            {
                case ScreenFadeReplaceMode.IgnoreIfPlaying:
                    return !_isPlaying;

                case ScreenFadeReplaceMode.IgnoreIfOwnerPriorityIsGreaterOrEqual:
                    return nextPriority > currentPriority;

                case ScreenFadeReplaceMode.ReplaceCurrent:
                default:
                    return nextPriority >= currentPriority;
            }
        }

        /// <summary>
        /// 요청 데이터의 범위와 기본값을 보정합니다.
        /// </summary>
        /// <param name="request">원본 요청입니다.</param>
        /// <returns>안전한 값으로 보정된 요청입니다.</returns>
        private static ScreenFadeRequest NormalizeRequest(ScreenFadeRequest request)
        {
            request.fromAlpha = Mathf.Clamp01(request.fromAlpha);
            request.toAlpha = Mathf.Clamp01(request.toAlpha);
            request.durationSeconds = Mathf.Max(0f, request.durationSeconds);
            request.planeDistance = Mathf.Max(0.01f, request.planeDistance);
            if (string.IsNullOrWhiteSpace(request.sortingLayerName))
            {
                request.sortingLayerName = nameof(ConfigSortingLayer.Keys.UI);
            }

            return request;
        }

        /// <summary>
        /// 현재 요청을 완료 또는 강제 정리합니다.
        /// </summary>
        /// <param name="forceClear">true이면 마지막 상태 유지 설정과 무관하게 화면 페이드를 숨깁니다.</param>
        private void StopInternal(bool forceClear)
        {
            _isPlaying = false;

            if (_presenter == null || _currentData == null)
                return;

            if (!forceClear && _currentData.holdFinalState)
            {
                _presenter.SetFade(_currentData.color, _currentData.toAlpha, _currentData.toAlpha > 0f);
            }
            else
            {
                _presenter.SetFade(_currentData.color, 0f, false);
                _hasCurrentRequest = false;
                _currentData = null;
            }
        }

        /// <summary>
        /// 페이드 표시용 Presenter가 존재하도록 보장합니다.
        /// </summary>
        /// <param name="data">Presenter 초기화에 사용할 렌더 설정입니다.</param>
        private void EnsurePresenter(ScreenFadeData data, bool resetPresentation)
        {
            if (_sceneGame == null)
                return;

            bool created = false;
            if (_presenter == null)
            {
                var root = new GameObject("ScreenFadePresenter", typeof(RectTransform), typeof(Canvas));
                root.transform.SetParent(_sceneGame.transform, false);

                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                _presenter = root.AddComponent<ScreenFadePresenter>();
                created = true;
            }

            _presenter.ApplyRenderSettings(data, _sceneGame);
            if (created || resetPresentation)
            {
                _presenter.ResetPresentation();
            }
        }

        /// <summary>
        /// 소유자별 화면 페이드 우선순위를 반환합니다.
        /// </summary>
        /// <param name="owner">확인할 소유자입니다.</param>
        /// <returns>우선순위 값입니다. 값이 클수록 우선합니다.</returns>
        private static int GetOwnerPriority(ScreenFadeOwner owner)
        {
            return (int)owner;
        }
    }
}
