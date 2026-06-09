using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class NpcHpBar : MonoBehaviour
    {
        [Tooltip("몬스터 머리 위 기준에서 Y축 높이 값")]
        public float diffY;
        public TextMeshProUGUI textNpcName;
        
        private Npc _npc;
        private Slider _hpSlider;
        private bool _isStartFade;
        private CanvasGroup _canvasGroup;
        private float _npcHeight;

        private void Awake()
        {
            _hpSlider = GetComponent<Slider>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _hpSlider.value = 1f;
            _isStartFade = false;
        }
        public void Initialize(Npc npc)
        {
            _npc = npc;
            if (_npc == null)
            {
                GcLogger.LogError("몬스터 오브젝트가 없습니다.");
                return;
            }
            var info = TableLoaderManager.Instance.GetNpcData(_npc.uid);
            if (info == null)
            {
                GcLogger.LogError("몬스터 테이블에 정보가 없습니다. uid:"+_npc.uid);
                return;
            }
            if (textNpcName == null) return;
            textNpcName.text = info.Name;
        }

        private void Start()
        {
            _npcHeight = _npc.GetHeightByScale();
        }

        private void Update()
        {
            if (_npc == null) return;
            gameObject.transform.position = _npc.transform.position + new Vector3(0, _npcHeight + diffY, 0);
        }

        public void SetValue(long value)
        {
            if (_hpSlider == null) return;
            _hpSlider.value = (float)value / _npc.MaxHp.Value;

            if (textNpcName == null) return;
            textNpcName.color = _hpSlider.value < _hpSlider.maxValue * 0.5f ? Color.black : Color.white;
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
        /// 코루틴 시작이 불가능한 상황에서 즉시 시각 상태를 적용합니다.
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

            SetIsStartFade(false);
        }
    }
}
