using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class MonsterUISuperArmor : MonoBehaviour
    {
        [Tooltip("몬스터 머리 위 기준에서 Y축 높이 값")]
        public float diffY;
        [Tooltip("슈퍼 아머 아이콘 프리팹")]
        public GameObject prefabShield;
        
        private Monster _monster;
        private CanvasGroup _canvasGroup;
        private float _monsterHeight;
        private bool _isStartFade;
        private List<GameObject> _shieldIcons;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
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
        }
        private void Start()
        {
            _monsterHeight = _monster.GetHeightByScale();

            InitializeSuperArmorIcon();
        }

        private void InitializeSuperArmorIcon()
        {
            if (!_monster)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다.");
                return;
            }
            if (!prefabShield)
            {
                GcLogger.LogError($"{nameof(prefabShield)}가 없습니다.");
                return;
            }
            int superArmor = _monster.CurrentSuperArmor.Value;
            if (superArmor <= 0) return;
            _shieldIcons = new List<GameObject>(superArmor);
            for (int i = 0; i < superArmor; i++)
            {
                var shield = Instantiate(prefabShield, transform);
                _shieldIcons.Add(shield);
            }
        }

        private void Update()
        {
            if (_monster == null) return;
            gameObject.transform.position = _monster.transform.position + new Vector3(0, _monsterHeight + diffY, 0);
        }
        public void SetValue(int value)
        {
            if (value <= 0)
            {
                foreach (var shieldIcon in _shieldIcons)
                {
                    shieldIcon.SetActive(false);
                }
                return;
            }
            int index = 0;
            foreach (var shieldIcon in _shieldIcons)
            {
                shieldIcon.SetActive(index < value);
                index++;
            }
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