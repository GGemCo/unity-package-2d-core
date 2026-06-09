using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class MonsterHpBar : MonoBehaviour
    {
        [Tooltip("몬스터 머리 위 기준에서 X축 값. flip Slider가 있으면 x 좌표를 이동시켜 주어야 한다.")]
        public float diffX;
        [Tooltip("몬스터 머리 위 기준에서 Y축 높이 값")]
        public float diffY;
        [Tooltip("몬스터 이름. 사용안할 경우 비워 둠")]
        public TextMeshProUGUI textMonsterName;

        private Monster _monster;
        private Slider _hpSlider;
        private bool _isStartFade;
        private CanvasGroup _canvasGroup;
        private float _monsterHeight;

        protected virtual void Awake()
        {
            _hpSlider = GetComponent<Slider>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _hpSlider.value = 1f;
            _isStartFade = false;
        }
        public void Initialize(Monster monster)
        {
            _monster = monster;
            if (_monster == null)
            {
                GcLogger.LogError("몬스터 오브젝트가 없습니다.");
                return;
            }
            var info = TableLoaderManager.Instance.GetMonsterData(_monster.uid);
            if (info == null)
            {
                GcLogger.LogError("몬스터 테이블에 정보가 없습니다. uid:"+_monster.uid);
                return;
            }
            _monsterHeight = _monster.GetHeightByScale();

            SetName(info.Name);
        }

        private void Update()
        {
            if (_monster == null) return;
            gameObject.transform.position = _monster.transform.position + new Vector3(diffX, _monsterHeight + diffY, 0);
        }

        public virtual void SetValue(long value)
        {
            if (_hpSlider != null)
            {
                _hpSlider.value = (float)value / _monster.MaxHp.Value;    
            }

            if (textMonsterName != null)
            {
                textMonsterName.color = _hpSlider.value < _hpSlider.maxValue * 0.5f ? Color.black : Color.white;    
            }
        }
        /// <summary>
        /// fade in 효과 시작. 맵 컬링시 사용
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
        /// fade out 효과 시작. 맵 컬링시 사용
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
        private IEnumerator FadeIn(float duration)
        {
            yield return FadeEffect(duration, true);
        }

        private IEnumerator FadeOut(float duration)
        {
            yield return FadeEffect(duration, false);
            gameObject.SetActive(false);
        }

        private IEnumerator FadeEffect(float duration, bool fadeIn)
        {
            float elapsedTime = 0f;
            float startAlpha = fadeIn ? 0 : 1;
            float endAlpha = fadeIn ? 1 : 0;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                yield return null;
            }

            SetIsStartFade(false);
        }
        private void SetIsStartFade(bool value)
        {
            _isStartFade = value;
        }

        private void SetName(string monsterName)
        {
            if (textMonsterName == null) return;
            textMonsterName.text = monsterName;
        }

        /// <summary>
        /// 페이드 코루틴을 시작할 수 있는 활성 상태인지 확인합니다.
        /// 부모 컨테이너가 비활성화된 경우 activeInHierarchy가 거짓이므로 코루틴을 시작할 수 없습니다.
        /// </summary>
        /// <returns>코루틴 시작이 가능하면 <see langword="true"/>를 반환합니다.</returns>
        private bool CanStartFadeCoroutine()
        {
            return isActiveAndEnabled && gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 코루틴을 시작할 수 없는 상황에서 즉시 알파/활성 상태를 반영합니다.
        /// </summary>
        /// <param name="visible">표시 상태를 나타냅니다.</param>
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

            SetIsStartFade(false);
        }
    }
}
