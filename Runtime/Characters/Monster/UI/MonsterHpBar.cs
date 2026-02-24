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
        
        protected Monster Monster;
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
            Monster = monster;
            if (Monster == null)
            {
                GcLogger.LogError("몬스터 오브젝트가 없습니다.");
                return;
            }
            var info = TableLoaderManager.Instance.GetMonsterData(Monster.uid);
            if (info == null)
            {
                GcLogger.LogError("몬스터 테이블에 정보가 없습니다. uid:"+Monster.uid);
                return;
            }
            if (textMonsterName == null) return;
            textMonsterName.text = info.Name;
        }

        private void Start()
        {
            _monsterHeight = Monster.GetHeightByScale();
        }

        private void Update()
        {
            if (Monster == null) return;
            gameObject.transform.position = Monster.transform.position + new Vector3(diffX, _monsterHeight + diffY, 0);
        }

        public virtual void SetValue(long value)
        {
            if (_hpSlider == null) return;
            _hpSlider.value = (float)value / Monster.TotalHp.Value;

            if (textMonsterName == null) return;
            textMonsterName.color = _hpSlider.value < _hpSlider.maxValue * 0.5f ? Color.black : Color.white;
        }
        /// <summary>
        /// fade in 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeIn()
        {
            if (_isStartFade) return;
            _isStartFade = true;
            gameObject.SetActive(true);
            StartCoroutine(FadeIn(ConfigCommon.CharacterFadeSec));
        }

        /// <summary>
        /// fade out 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeOut()
        {
            if (_isStartFade) return;
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
    }
}