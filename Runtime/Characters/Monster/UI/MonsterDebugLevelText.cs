using System.Collections;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스폰된 몬스터의 레벨을 월드 UI로 표시하는 디버그 전용 컴포넌트입니다.
    /// </summary>
    public class MonsterDebugLevelText : MonoBehaviour
    {
        private Monster _monster;
        private TextMeshProUGUI _textLevel;
        private CanvasGroup _canvasGroup;
        private Vector3 _offset;
        private bool _isStartFade;

        /// <summary>
        /// 몬스터 레벨 디버그 텍스트를 초기화합니다.
        /// </summary>
        /// <param name="monster">레벨을 표시할 몬스터입니다.</param>
        /// <param name="settings">몬스터 디버그 표시 설정입니다.</param>
        public void Initialize(Monster monster, GGemCoMonsterSettings settings)
        {
            _monster = monster;
            _textLevel = GetComponent<TextMeshProUGUI>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_monster == null || settings == null || _textLevel == null)
            {
                GcLogger.LogError("몬스터 레벨 디버그 텍스트 초기화 정보가 없습니다.");
                return;
            }

            _offset = settings.monsterSpawnLevelTextOffset;
            _textLevel.fontSize = settings.monsterSpawnLevelTextFontSize;
            _textLevel.color = settings.monsterSpawnLevelTextColor;
            _textLevel.alignment = TextAlignmentOptions.Center;
            _textLevel.raycastTarget = false;
            _textLevel.text = $"Lv. {_monster.CurrentLevel}";
            _isStartFade = false;
        }

        private void Update()
        {
            if (_monster == null)
            {
                return;
            }

            transform.position = _monster.transform.position + new Vector3(0f, _monster.GetHeightByScale(), 0f) + _offset;
        }

        /// <summary>
        /// 맵 컬링 복귀 시 레벨 텍스트를 즉시 또는 페이드로 표시합니다.
        /// </summary>
        public void StartFadeIn()
        {
            if (_isStartFade) return;
            gameObject.SetActive(true);
            if (!CanStartFadeCoroutine())
            {
                ApplyImmediateFadeState(true);
                return;
            }

            _isStartFade = true;
            StartCoroutine(FadeIn(ConfigCommon.CharacterFadeSec));
        }

        /// <summary>
        /// 맵 컬링 이탈 시 레벨 텍스트를 즉시 또는 페이드로 숨깁니다.
        /// </summary>
        public void StartFadeOut()
        {
            if (_isStartFade) return;
            if (!gameObject.activeSelf) return;
            if (!CanStartFadeCoroutine())
            {
                ApplyImmediateFadeState(false);
                return;
            }

            _isStartFade = true;
            StartCoroutine(FadeOut(ConfigCommon.CharacterFadeSec));
        }

        /// <summary>
        /// 레벨 텍스트 Fade In 코루틴입니다.
        /// </summary>
        /// <param name="duration">페이드 시간입니다.</param>
        /// <returns>페이드 처리 코루틴입니다.</returns>
        private IEnumerator FadeIn(float duration)
        {
            yield return FadeEffect(duration, true);
        }

        /// <summary>
        /// 레벨 텍스트 Fade Out 코루틴입니다.
        /// </summary>
        /// <param name="duration">페이드 시간입니다.</param>
        /// <returns>페이드 처리 코루틴입니다.</returns>
        private IEnumerator FadeOut(float duration)
        {
            yield return FadeEffect(duration, false);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// CanvasGroup alpha 값을 보간하여 레벨 텍스트 표시 상태를 전환합니다.
        /// </summary>
        /// <param name="duration">페이드 시간입니다.</param>
        /// <param name="fadeIn">표시 방향이면 true, 숨김 방향이면 false입니다.</param>
        /// <returns>페이드 처리 코루틴입니다.</returns>
        private IEnumerator FadeEffect(float duration, bool fadeIn)
        {
            float elapsedTime = 0f;
            float startAlpha = fadeIn ? 0f : 1f;
            float endAlpha = fadeIn ? 1f : 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                yield return null;
            }

            _canvasGroup.alpha = endAlpha;
            _isStartFade = false;
        }

        /// <summary>
        /// 페이드 코루틴을 시작할 수 있는 활성 상태인지 확인합니다.
        /// </summary>
        /// <returns>코루틴 시작이 가능하면 true입니다.</returns>
        private bool CanStartFadeCoroutine()
        {
            return isActiveAndEnabled && gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 코루틴 시작이 불가능한 상황에서 즉시 알파/활성 상태를 적용합니다.
        /// </summary>
        /// <param name="visible">표시 여부입니다.</param>
        private void ApplyImmediateFadeState(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
            }

            if (!visible)
            {
                gameObject.SetActive(false);
            }

            _isStartFade = false;
        }
    }
}
