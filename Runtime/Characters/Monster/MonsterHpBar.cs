using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    public class MonsterHpBar : MonoBehaviour
    {
        [Tooltip("몬스터 머리 위 기준에서 Y축 높이 값")]
        public float diffY;
        public TextMeshProUGUI textMonsterName;
        
        private Monster monster;
        private Slider hpSlider;
        private bool isStartFade;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            hpSlider = GetComponent<Slider>();
            canvasGroup = GetComponent<CanvasGroup>();
            hpSlider.value = 1f;
            isStartFade = false;
        }
        public void Initialize(Monster pmonster)
        {
            monster = pmonster;
            if (monster == null)
            {
                GcLogger.LogError("몬스터 오브젝트가 없습니다.");
                return;
            }
            var info = TableLoaderManager.Instance.TableMonster.GetDataByUid(monster.uid);
            if (info == null)
            {
                GcLogger.LogError("몬스터 테이블에 정보가 없습니다. uid:"+monster.uid);
                return;
            }
            if (textMonsterName == null) return;
            textMonsterName.text = info.Name;
        }

        private void Update()
        {
            if (monster == null) return;
            gameObject.transform.position = monster.transform.position + new Vector3(0, monster.GetHeight() + diffY, 0);
        }

        public void SetValue(long value)
        {
            if (hpSlider == null) return;
            hpSlider.value = (float)value / monster.TotalHp.Value;

            if (textMonsterName == null) return;
            if (hpSlider.value < hpSlider.maxValue * 0.5f)
                textMonsterName.color = Color.black;
            else
                textMonsterName.color = Color.white;
        }
        /// <summary>
        /// fade in 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeIn()
        {
            if (isStartFade) return;
            isStartFade = true;
            gameObject.SetActive(true);
            StartCoroutine(FadeIn(ConfigCommon.CharacterFadeSec));
        }

        /// <summary>
        /// fade out 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeOut()
        {
            if (isStartFade) return;
            isStartFade = true;
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
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                yield return null;
            }

            SetIsStartFade(false);
        }
        private void SetIsStartFade(bool value)
        {
            isStartFade = value;
        }
    }
}